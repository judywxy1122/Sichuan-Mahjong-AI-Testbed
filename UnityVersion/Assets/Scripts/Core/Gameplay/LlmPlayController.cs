using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using SichuanMahjong.Core.Model;

namespace SichuanMahjong.Core.Gameplay
{
    /// <summary>
    /// LLM-driven controller for the human seat. The Java version shelled out
    /// to scripts/llm_play.py; this rewrite talks to the same Azure-style
    /// chat-completions endpoint natively over HTTP so the Unity project has
    /// no Python dependency. Environment variables are unchanged:
    /// GPT_API_SG_KEY, MAHJONG_LLM_MODEL, MAHJONG_LLM_ENDPOINT,
    /// MAHJONG_LLM_API_VERSION, MAHJONG_LLM_REASONING,
    /// MAHJONG_LLM_TIMEOUT_SECONDS.
    /// </summary>
    public class LlmPlayController : IPlayController
    {
        private const string LlmLogPath = "llm_play_log.txt";
        private static readonly Regex ActionPattern = new Regex("\"action\"\\s*:\\s*\"([^\"]*)\"");
        private static readonly Regex TilePattern = new Regex("\"tile\"\\s*:\\s*(?:\"([^\"]*)\"|null)");
        private static readonly Regex ReasonPattern = new Regex("\"reason\"\\s*:\\s*\"([^\"]*)\"");
        private static readonly Regex ContentPattern = new Regex("\"content\"\\s*:\\s*\"((?:[^\"\\\\]|\\\\.)*)\"");

        private static readonly HttpClient httpClient = new HttpClient
        {
            Timeout = Timeout.InfiniteTimeSpan
        };

        private readonly AutoPlayController fallbackController;
        private readonly object logLock = new object();

        public LlmPlayController(AutoPlayController fallbackController)
        {
            this.fallbackController = fallbackController;
        }

        public string GetName()
        {
            return "LLM Play";
        }

        public void MarkNewGame(Game game)
        {
            if (game == null)
            {
                AppendLlmLog("NEW GAME", "A new Mahjong hand started.");
                return;
            }
            AppendLlmLog("NEW GAME", "A new Mahjong hand started."
                + "\nlast_action=" + game.GetLastActionText()
                + "\nstatus=" + game.GetStatusText()
                + "\nturn_player=" + (game.GetTurnPlayer() == null ? "" : game.GetTurnPlayer().GetName()));
        }

        public PlayDecision Choose(Game game, Player player)
        {
            PlayerActionContext context = new PlayerActionContext(game, player);
            if (!context.IsActionNeeded())
            {
                return PlayDecision.None("No player action needed.", GetName());
            }

            string apiKey = Environment.GetEnvironmentVariable("GPT_API_SG_KEY");
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                return Fallback(game, player, "GPT_API_SG_KEY is not set.", null, null, null);
            }

            string requestJson = BuildRequestJson(game, player, context);
            AppendLlmLog("REQUEST", "legal_actions=[" + string.Join(", ", context.LegalActionLabels())
                + "]\nrequest_json=" + requestJson);
            try
            {
                long startedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                string response = CallLlm(apiKey, requestJson, context);
                long elapsedMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - startedAt;
                AppendLlmLog("RESPONSE", "elapsed_ms=" + elapsedMs + "\nraw_response=" + response.Trim());
                PlayDecision decision = ParseDecision(response, context);
                if (context.IsLegal(decision))
                {
                    AppendLlmLog("ACCEPTED", DescribeDecision(decision));
                    return decision;
                }
                return Fallback(game, player, "LLM returned an illegal action: "
                        + DescribeDecision(decision)
                        + "; legal actions: [" + string.Join(", ", context.LegalActionLabels()) + "].",
                    requestJson, response, decision);
            }
            catch (Exception e)
            {
                return Fallback(game, player, "LLM unavailable: " + RootMessage(e), requestJson, null, null);
            }
        }

        private static string RootMessage(Exception e)
        {
            Exception cur = e;
            while (cur.InnerException != null)
            {
                cur = cur.InnerException;
            }
            return cur.Message;
        }

        private PlayDecision Fallback(Game game, Player player, string reason,
                                      string requestJson, string response, PlayDecision rejectedDecision)
        {
            PlayDecision fallback = fallbackController.Choose(game, player);
            AppendLlmLog("FALLBACK", "reason=" + reason
                + "\nfallback_decision=" + DescribeDecision(fallback)
                + (rejectedDecision == null ? "" : "\nrejected_decision=" + DescribeDecision(rejectedDecision))
                + (response == null ? "" : "\nraw_response=" + response.Trim())
                + (requestJson == null ? "" : "\nrequest_json=" + requestJson));
            return new PlayDecision(fallback.GetAction(), fallback.GetTile(),
                reason + " Fallback: " + fallback.GetReason(), GetName() + " -> " + fallback.GetSource());
        }

        // ---- native HTTP bridge (replaces scripts/llm_play.py) ----

        private static string GetEnvOrDefault(string name, string fallbackValue)
        {
            string value = Environment.GetEnvironmentVariable(name);
            return string.IsNullOrWhiteSpace(value) ? fallbackValue : value.Trim();
        }

        private static string DefaultEndpoint()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return "https://aidp-i18ntt-sg.tiktok-row.net/api/modelhub/online/multimodal/crawl";
            }
            return "https://aidp-i18ntt-sg.byteintl.net/api/modelhub/online/multimodal/crawl";
        }

        private string CallLlm(string apiKey, string stateJson, PlayerActionContext context)
        {
            string model = GetEnvOrDefault("MAHJONG_LLM_MODEL", "gpt-5.5-2026-04-24");
            string apiVersion = GetEnvOrDefault("MAHJONG_LLM_API_VERSION", "2025-01-01-preview");
            string reasoningEffort = GetEnvOrDefault("MAHJONG_LLM_REASONING", "medium");
            string endpoint = GetEnvOrDefault("MAHJONG_LLM_ENDPOINT", DefaultEndpoint()).TrimEnd('/');
            double timeoutSeconds = double.Parse(
                GetEnvOrDefault("MAHJONG_LLM_TIMEOUT_SECONDS", "18"), CultureInfo.InvariantCulture);

            string url = endpoint + "/openai/deployments/" + Uri.EscapeDataString(model)
                         + "/chat/completions?api-version=" + Uri.EscapeDataString(apiVersion);

            string body = BuildChatCompletionBody(stateJson, context, reasoningEffort);

            using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, url))
            using (CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds)))
            {
                request.Headers.TryAddWithoutValidation("api-key", apiKey);
                request.Content = new StringContent(body, Encoding.UTF8, "application/json");
                HttpResponseMessage response =
                    httpClient.SendAsync(request, cts.Token).GetAwaiter().GetResult();
                string text = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                if (!response.IsSuccessStatusCode)
                {
                    throw new IOException("HTTP " + (int)response.StatusCode + ": " + Truncate(text, 500));
                }
                string content = ExtractMessageContent(text);
                if (content == null)
                {
                    throw new IOException("no message content in response: " + Truncate(text, 500));
                }
                return content;
            }
        }

        private static string Truncate(string text, int max)
        {
            if (text == null)
            {
                return "";
            }
            return text.Length <= max ? text : text.Substring(0, max) + "...";
        }

        /// <summary>Builds the same system/user messages the Python bridge sent.</summary>
        private string BuildChatCompletionBody(string stateJson, PlayerActionContext context, string reasoningEffort)
        {
            List<string> legalActions = context.LegalActionLabels();
            string system =
                "You are playing Sichuan Mahjong for the human seat. "
                + "Choose exactly one legal action from the provided legal_actions list. "
                + "Do not choose any action that is not literally allowed by legal_actions. "
                + "Return only JSON with fields: action, tile, reason. "
                + "Allowed action values: hu, chow, pung, kong, skip, discard. "
                + "If legal_actions contains discard:X, output action=discard and tile=X. "
                + "For discard, tile must match one of the legal discard tile codes exactly. "
                + "For all other actions, tile must be null. "
                + "If legal_actions does not contain any discard:X item, do not discard. "
                + "Prefer winning immediately. Prefer legal claims that improve the hand. "
                + "If a kong is optional, take it only when it does not obviously damage the hand.";

            StringBuilder user = new StringBuilder();
            user.Append("{\"task\":\"Choose the next action for the human player.\",\"legal_actions\":[");
            for (int i = 0; i < legalActions.Count; i++)
            {
                if (i > 0)
                {
                    user.Append(",");
                }
                user.Append("\"").Append(Escape(legalActions[i])).Append("\"");
            }
            user.Append("],\"legal_actions_text\":\"").Append(Escape(string.Join(", ", legalActions)));
            user.Append("\",\"state\":").Append(stateJson);
            user.Append(",\"output_examples\":[");
            user.Append("{\"action\":\"hu\",\"tile\":null,\"reason\":\"Winning is available.\"},");
            user.Append("{\"action\":\"discard\",\"tile\":\"C7\",\"reason\":\"Discard an isolated tile.\"},");
            user.Append("{\"action\":\"skip\",\"tile\":null,\"reason\":\"Claim is not worth taking.\"}]}");

            StringBuilder body = new StringBuilder();
            body.Append("{\"messages\":[");
            body.Append("{\"role\":\"system\",\"content\":\"").Append(Escape(system)).Append("\"},");
            body.Append("{\"role\":\"user\",\"content\":\"").Append(Escape(user.ToString())).Append("\"}");
            body.Append("],\"temperature\":1.0,\"top_p\":1.0,\"max_completion_tokens\":800,");
            body.Append("\"response_format\":{\"type\":\"json_object\"},");
            body.Append("\"reasoning_effort\":\"").Append(Escape(reasoningEffort)).Append("\"}");
            return body.ToString();
        }

        /// <summary>Pulls choices[0].message.content out of the response envelope.</summary>
        private static string ExtractMessageContent(string envelope)
        {
            Match match = ContentPattern.Match(envelope ?? "");
            if (!match.Success)
            {
                return null;
            }
            return JsonUnescape(match.Groups[1].Value);
        }

        private static string JsonUnescape(string value)
        {
            StringBuilder sb = new StringBuilder(value.Length);
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                if (c != '\\' || i + 1 >= value.Length)
                {
                    sb.Append(c);
                    continue;
                }
                char next = value[++i];
                switch (next)
                {
                    case '"': sb.Append('"'); break;
                    case '\\': sb.Append('\\'); break;
                    case '/': sb.Append('/'); break;
                    case 'b': sb.Append('\b'); break;
                    case 'f': sb.Append('\f'); break;
                    case 'n': sb.Append('\n'); break;
                    case 'r': sb.Append('\r'); break;
                    case 't': sb.Append('\t'); break;
                    case 'u':
                        if (i + 4 < value.Length
                            && int.TryParse(value.Substring(i + 1, 4), NumberStyles.HexNumber,
                                CultureInfo.InvariantCulture, out int code))
                        {
                            sb.Append((char)code);
                            i += 4;
                        }
                        break;
                    default: sb.Append(next); break;
                }
            }
            return sb.ToString();
        }

        // ---- request-state JSON (same shape as the Java version) ----

        private string BuildRequestJson(Game game, Player player, PlayerActionContext context)
        {
            StringBuilder json = new StringBuilder();
            json.Append("{");
            Field(json, "player", player.GetName()).Append(",");
            Field(json, "last_action", game.GetLastActionText()).Append(",");
            Field(json, "status", game.GetStatusText()).Append(",");
            Field(json, "last_played_tile", TileCodec.Code(game.GetLastPlayedTile())).Append(",");
            ArrayField(json, "legal_actions", context.LegalActionLabels()).Append(",");
            TileArrayField(json, "hand", player.GetHand().ToList()).Append(",");
            Field(json, "new_tile", TileCodec.Code(player.GetHand().GetNewTile())).Append(",");
            TileArrayField(json, "your_discards", player.GetTable().ToList()).Append(",");
            GroupArrayField(json, "melds", player.GetHand().GetPungKong());
            json.Append("}");
            return json.ToString();
        }

        private static StringBuilder Field(StringBuilder json, string name, string value)
        {
            return json.Append("\"").Append(name).Append("\":\"").Append(Escape(value)).Append("\"");
        }

        private static StringBuilder ArrayField(StringBuilder json, string name, List<string> values)
        {
            json.Append("\"").Append(name).Append("\":[");
            for (int i = 0; i < values.Count; i++)
            {
                if (i > 0)
                {
                    json.Append(",");
                }
                json.Append("\"").Append(Escape(values[i])).Append("\"");
            }
            return json.Append("]");
        }

        private static StringBuilder TileArrayField(StringBuilder json, string name, List<Tile> tiles)
        {
            json.Append("\"").Append(name).Append("\":[");
            for (int i = 0; i < tiles.Count; i++)
            {
                if (i > 0)
                {
                    json.Append(",");
                }
                json.Append("\"").Append(TileCodec.Code(tiles[i])).Append("\"");
            }
            return json.Append("]");
        }

        private static StringBuilder GroupArrayField(StringBuilder json, string name, List<Model.Group> groups)
        {
            json.Append("\"").Append(name).Append("\":[");
            for (int i = 0; i < groups.Count; i++)
            {
                if (i > 0)
                {
                    json.Append(",");
                }
                json.Append("{");
                Field(json, "type", groups[i].GetCategory().ToString()).Append(",");
                TileArrayField(json, "tiles", groups[i].ToList());
                json.Append("}");
            }
            return json.Append("]");
        }

        private static string Escape(string value)
        {
            if (value == null)
            {
                return "";
            }
            return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }

        // ---- decision parsing (same as the Java version) ----

        private PlayDecision ParseDecision(string response, PlayerActionContext context)
        {
            string action = Extract(ActionPattern, response);
            string tileCode = Extract(TilePattern, response);
            string reason = Extract(ReasonPattern, response);
            if (action == null)
            {
                return PlayDecision.None("LLM response had no action.", GetName());
            }
            action = action.Trim().ToLowerInvariant();
            if (action.StartsWith("discard:") && string.IsNullOrWhiteSpace(tileCode))
            {
                tileCode = action.Substring("discard:".Length);
                action = "discard";
            }
            action = NormalizeAction(action);
            if (action == "hu")
            {
                return PlayDecision.Of(PlayDecision.ActionType.HU, reason, GetName());
            }
            if (action == "chow")
            {
                return PlayDecision.Of(PlayDecision.ActionType.CHOW, reason, GetName());
            }
            if (action == "pung")
            {
                return PlayDecision.Of(PlayDecision.ActionType.PUNG, reason, GetName());
            }
            if (action == "kong")
            {
                return PlayDecision.Of(PlayDecision.ActionType.KONG, reason, GetName());
            }
            if (action == "skip")
            {
                return PlayDecision.Of(PlayDecision.ActionType.SKIP, reason, GetName());
            }
            if (action == "discard")
            {
                string normalizedTileCode = NormalizeTileCode(tileCode);
                Tile tile = context.ResolveDiscardTile(TileCodec.FromCode(normalizedTileCode));
                string discardReason = reason;
                if (tile == null)
                {
                    discardReason = AppendReason(reason, "raw tile=" + normalizedTileCode);
                }
                return PlayDecision.Discard(tile, discardReason, GetName());
            }
            return PlayDecision.None("Unknown LLM action: " + action, GetName());
        }

        private static string NormalizeAction(string action)
        {
            if (action == "win" || action == "hu牌" || action == "胡")
            {
                return "hu";
            }
            if (action == "chi" || action == "chou" || action == "eat" || action == "吃")
            {
                return "chow";
            }
            if (action == "pong" || action == "peng" || action == "碰")
            {
                return "pung";
            }
            if (action == "gang" || action == "杠")
            {
                return "kong";
            }
            if (action == "pass" || action == "fold" || action == "过")
            {
                return "skip";
            }
            return action;
        }

        private static string NormalizeTileCode(string tileCode)
        {
            return tileCode?.Trim().ToUpperInvariant();
        }

        private static string DescribeDecision(PlayDecision decision)
        {
            if (decision == null)
            {
                return "null";
            }
            StringBuilder builder = new StringBuilder(decision.GetAction().ToString().ToLowerInvariant());
            if (decision.GetTile() != null)
            {
                builder.Append(" ").Append(TileCodec.Code(decision.GetTile()));
            }
            if (!string.IsNullOrEmpty(decision.GetReason()))
            {
                builder.Append(" (").Append(decision.GetReason()).Append(")");
            }
            return builder.ToString();
        }

        private static string AppendReason(string reason, string detail)
        {
            if (string.IsNullOrEmpty(reason))
            {
                return detail;
            }
            return reason + "; " + detail;
        }

        private void AppendLlmLog(string section, string body)
        {
            string text = "\n=== " + DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fff", CultureInfo.InvariantCulture)
                          + " " + section + " ===\n" + (body ?? "") + "\n";
            lock (logLock)
            {
                try
                {
                    File.AppendAllText(LlmLogPath, text, Encoding.UTF8);
                }
                catch (IOException e)
                {
                    CoreEnv.Println("Could not write LLM play log: " + e.Message);
                }
            }
        }

        private static string Extract(Regex pattern, string text)
        {
            Match match = pattern.Match(text ?? "");
            return match.Success ? match.Groups[1].Value : null;
        }
    }
}
