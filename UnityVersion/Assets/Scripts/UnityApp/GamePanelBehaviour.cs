using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using SichuanMahjong.Core;
using SichuanMahjong.Core.Gameplay;
using SichuanMahjong.Core.Model;
using UnityEngine;
using UnityEngine.UI;

namespace SichuanMahjong.UnityApp
{
    /// <summary>
    /// Port of the Swing GamePanel: game loop, keyboard shortcuts, action
    /// buttons, Auto Play / LLM Play scheduling and the board rendering. UI
    /// is rebuilt from the game state whenever the state key changes, which
    /// replaces the Swing repaint loop.
    /// </summary>
    public class GamePanelBehaviour : MonoBehaviour
    {
        private enum PlayMode
        {
            None,
            Auto,
            Llm
        }

        private const float PlayDelaySeconds = 3f;
        private const string AutoPlayPendingPrefix = "Auto Play will act in 3s. ";
        private const string LlmPlayPendingPrefix = "LLM Play will act in 3s. ";

        private Game game;
        private Player player;
        private TileImageLoader imageLoader;
        private BeepPlayer beeper;
        private readonly AutoPlayController autoPlayController = new AutoPlayController();
        private LlmPlayController llmPlayController;

        private RectTransform gameRoot;
        private RectTransform boardLayer;
        private RectTransform dialogLayer;
        private Image backgroundImage;

        private Tile hoveredTile;
        private PlayMode playMode = PlayMode.None;
        private float playTimerDeadline = -1f;
        private string scheduledPlayKey = "";
        private bool llmDecisionInFlight;
        private string llmDecisionKey = "";
        private bool rewardCelebrationPlayed;
        private GameObject winningHandDialog;
        private string renderedStateKey;

        private readonly ConcurrentQueue<Action> mainThreadActions = new ConcurrentQueue<Action>();

        private static readonly Color32 HighlightYellow = new Color32(255, 225, 80, 255);
        private static readonly Color32 TurnBlue = new Color32(90, 190, 255, 255);
        private static readonly Color32 BannerYellow = new Color32(255, 245, 120, 255);

        public void Initialize(RectTransform root, TileImageLoader loader, BeepPlayer beepPlayer)
        {
            gameRoot = root;
            imageLoader = loader;
            beeper = beepPlayer;
            llmPlayController = new LlmPlayController(autoPlayController);

            game = new Game();
            player = game.GetPlayers()[0];
            llmPlayController.MarkNewGame(game);

            BuildStaticBackground();
            boardLayer = (RectTransform)UiFactory.CreateChild(gameRoot, "BoardLayer").transform;
            UiFactory.SetRect(boardLayer, 0, 0, Config.SCREEN_WIDTH, Config.SCREEN_HEIGHT);
            dialogLayer = (RectTransform)UiFactory.CreateChild(gameRoot, "DialogLayer").transform;
            UiFactory.SetRect(dialogLayer, 0, 0, Config.SCREEN_WIDTH, Config.SCREEN_HEIGHT);
        }

        private void Update()
        {
            if (game == null)
            {
                return;
            }

            while (mainThreadActions.TryDequeue(out Action action))
            {
                action();
            }

            if (game.IsOver())
            {
                // The Swing game thread stopped here, which also stopped the
                // keyboard shortcuts; only the buttons stay active.
                CancelPlayTimer();
            }
            else
            {
                HandleKeys();
            }
            UpdatePlayController();
            FirePlayTimerIfDue();
            RefreshUiIfChanged();
        }

        // ---- keyboard shortcuts (port of KeyHandler + GamePanel.update) ----

        private void HandleKeys()
        {
            if (Input.GetKeyDown(KeyCode.H) && player.GetStatus().Contains(PlayerStatusEnum.HU))
            {
                game.ProcessHu(player);
            }
            if (Input.GetKeyDown(KeyCode.C) && player.ContainsChow())
            {
                game.ProcessChou(player);
            }
            if (Input.GetKeyDown(KeyCode.P) && player.ContainsPung())
            {
                game.ProcessPung(player);
            }
            if (Input.GetKeyDown(KeyCode.K) && player.ContainsKong())
            {
                game.ProcessKong(player);
            }
            if (Input.GetKeyDown(KeyCode.S)
                && ((!player.IsPlaying() && player.ContainsResponseAction()) || player.ContainsChouPungKong()))
            {
                game.ProcessSkip(player);
            }
        }

        // ---- tile interaction (port of the mouse listeners) ----

        public void OnTileHovered(TileView view)
        {
            hoveredTile = view.GetTile();
        }

        public void OnTileUnhovered(TileView view)
        {
            if (ReferenceEquals(hoveredTile, view.GetTile()))
            {
                hoveredTile = null;
            }
        }

        public void OnTileClicked(Tile tile)
        {
            if (game.IsOver())
            {
                return;
            }
            if (player.IsPlaying() && !player.ContainsChouPungKong())
            {
                CancelPlayTimer();
                player.Plays(tile);
                hoveredTile = null;
                game.ProcessPlayed();
            }
            else if (!player.IsPlaying() && player.ContainsResponseAction())
            {
                game.ShowInvalidInput(BuildResponsePhaseReminder());
            }
            else
            {
                game.ShowInvalidInput("It is not your discard turn yet.");
            }
        }

        private string BuildResponsePhaseReminder()
        {
            List<string> actions = new List<string>();
            if (player.ContainsHu())
            {
                actions.Add("H Hu");
            }
            if (player.ContainsChow())
            {
                actions.Add("C Chow");
            }
            if (player.ContainsPung())
            {
                actions.Add("P Pung");
            }
            if (player.ContainsKong())
            {
                actions.Add("K Kong");
            }
            actions.Add("S Skip");

            Player actor = game.GetLastActionPlayer();
            Tile tile = game.GetLastPlayedTile();
            if (actor != null && tile != null)
            {
                return actor.GetName() + " played " + TileCodec.Display(tile)
                       + "; choose response: " + string.Join(" / ", actions) + ".";
            }
            return "Response phase: choose response: " + string.Join(" / ", actions) + ".";
        }

        // ---- action buttons ----

        private void ProcessActionButton(string action)
        {
            switch (action)
            {
                case "Hu":
                    if (player.GetStatus().Contains(PlayerStatusEnum.HU))
                    {
                        game.ProcessHu(player);
                    }
                    break;
                case "Chow":
                    if (player.ContainsChow())
                    {
                        game.ProcessChou(player);
                    }
                    break;
                case "Pung":
                    if (player.ContainsPung())
                    {
                        game.ProcessPung(player);
                    }
                    break;
                case "Kong":
                    if (player.ContainsKong())
                    {
                        game.ProcessKong(player);
                    }
                    break;
                case "Skip":
                    if ((!player.IsPlaying() && player.ContainsResponseAction()) || player.ContainsChouPungKong())
                    {
                        game.ProcessSkip(player);
                    }
                    break;
            }
        }

        private void ProcessEndGameButton(string action)
        {
            if (action == "Reward")
            {
                if (!player.Equals(game.GetWinner()))
                {
                    game.ShowInvalidInput("Reward is available only when you win.");
                    return;
                }
                RewardCelebration.OpenRandomReward();
            }
            else if (action == "New game")
            {
                StartNewGame();
            }
            else if (action == "Exit")
            {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
            }
        }

        private void StartNewGame()
        {
            SetPlayMode(PlayMode.None);
            game = new Game();
            player = game.GetPlayers()[0];
            llmPlayController.MarkNewGame(game);
            hoveredTile = null;
            rewardCelebrationPlayed = false;
            if (winningHandDialog != null)
            {
                Destroy(winningHandDialog);
                winningHandDialog = null;
            }
            renderedStateKey = null;
        }

        // ---- Auto Play / LLM Play scheduling (port of the Swing timers) ----

        private void ToggleAutoPlay()
        {
            if (playMode == PlayMode.Llm)
            {
                return;
            }
            if (playMode == PlayMode.Auto)
            {
                SetPlayMode(PlayMode.None);
                game.ShowStatus("Auto Play is off. " + StripPlayStatusPrefix(game.GetStatusText()));
            }
            else
            {
                SetPlayMode(PlayMode.Auto);
                game.ShowStatus("Auto Play is on. I will act for you when it is your move.");
            }
        }

        private void ToggleLlmPlay()
        {
            if (playMode == PlayMode.Auto)
            {
                return;
            }
            if (playMode == PlayMode.Llm)
            {
                SetPlayMode(PlayMode.None);
                game.ShowStatus("LLM Play is off. " + StripPlayStatusPrefix(game.GetStatusText()));
            }
            else
            {
                SetPlayMode(PlayMode.Llm);
                game.ShowStatus("LLM Play is on. I will decide for you using gpt-5.5 when it is your move.");
            }
        }

        private void SetPlayMode(PlayMode nextMode)
        {
            CancelPlayTimer();
            llmDecisionInFlight = false;
            llmDecisionKey = "";
            playMode = nextMode;
        }

        private void UpdatePlayController()
        {
            if (playMode == PlayMode.None || game.IsOver())
            {
                CancelPlayTimer();
                return;
            }
            if (!new PlayerActionContext(game, player).IsActionNeeded())
            {
                CancelPlayTimer();
                return;
            }
            if (llmDecisionInFlight)
            {
                return;
            }

            string actionKey = BuildPlayKey();
            if (playTimerDeadline >= 0f && actionKey == scheduledPlayKey)
            {
                return;
            }

            CancelPlayTimer();
            scheduledPlayKey = actionKey;
            game.ShowStatus(GetPendingPrefix() + StripPlayStatusPrefix(game.GetStatusText()));
            playTimerDeadline = Time.unscaledTime + PlayDelaySeconds;
        }

        private void FirePlayTimerIfDue()
        {
            if (playTimerDeadline < 0f || Time.unscaledTime < playTimerDeadline)
            {
                return;
            }
            string key = scheduledPlayKey;
            CancelPlayTimer();
            ExecuteControllerAction(key);
        }

        private string BuildPlayKey()
        {
            Tile newTile = player.GetHand().GetNewTile();
            Tile lastPlayedTile = game.GetLastPlayedTile();
            return game.GetActionVersion()
                   + "|" + playMode
                   + "|" + player.IsPlaying()
                   + "|" + player.ContainsHu()
                   + "|" + player.ContainsChow()
                   + "|" + player.ContainsPung()
                   + "|" + player.ContainsKong()
                   + "|" + player.GetHand().GetHashCode()
                   + "|" + (newTile == null ? "none" : newTile.ToString())
                   + "|" + (lastPlayedTile == null ? "none" : lastPlayedTile.ToString());
        }

        private static string StripPlayStatusPrefix(string statusText)
        {
            if (statusText == null)
            {
                return "";
            }
            string text = statusText;
            while (text.StartsWith(AutoPlayPendingPrefix) || text.StartsWith(LlmPlayPendingPrefix))
            {
                text = text.StartsWith(AutoPlayPendingPrefix)
                    ? text.Substring(AutoPlayPendingPrefix.Length)
                    : text.Substring(LlmPlayPendingPrefix.Length);
            }
            return text;
        }

        private string GetPendingPrefix()
        {
            return playMode == PlayMode.Llm ? LlmPlayPendingPrefix : AutoPlayPendingPrefix;
        }

        private void CancelPlayTimer()
        {
            playTimerDeadline = -1f;
            scheduledPlayKey = "";
        }

        private void ExecuteControllerAction(string actionKey)
        {
            if (playMode == PlayMode.None || game.IsOver())
            {
                return;
            }
            if (actionKey == null || actionKey != BuildPlayKey())
            {
                return;
            }

            if (playMode == PlayMode.Llm)
            {
                ExecuteLlmControllerAction(actionKey);
                return;
            }

            PlayDecision decision = autoPlayController.Choose(game, player);
            ExecutePlayDecision(decision);
        }

        private void ExecuteLlmControllerAction(string actionKey)
        {
            llmDecisionInFlight = true;
            llmDecisionKey = actionKey;
            game.ShowStatus("LLM Play is thinking... " + StripPlayStatusPrefix(game.GetStatusText()));

            Thread llmThread = new Thread(() =>
            {
                PlayDecision decision = llmPlayController.Choose(game, player);
                mainThreadActions.Enqueue(() =>
                {
                    if (!llmDecisionInFlight || playMode != PlayMode.Llm || game.IsOver())
                    {
                        return;
                    }
                    if (llmDecisionKey != BuildPlayKey())
                    {
                        llmDecisionInFlight = false;
                        llmDecisionKey = "";
                        return;
                    }
                    llmDecisionInFlight = false;
                    llmDecisionKey = "";
                    ExecutePlayDecision(decision);
                });
            });
            llmThread.Name = "llm-play-controller";
            llmThread.IsBackground = true;
            llmThread.Start();
        }

        private void ExecutePlayDecision(PlayDecision decision)
        {
            PlayerActionContext context = new PlayerActionContext(game, player);
            if (!context.IsLegal(decision))
            {
                game.ShowInvalidInput("Play controller returned an illegal action.");
                return;
            }

            AddControllerDecisionLog(decision);
            switch (decision.GetAction())
            {
                case PlayDecision.ActionType.HU:
                    game.ProcessHu(player);
                    break;
                case PlayDecision.ActionType.CHOW:
                    game.ProcessChou(player);
                    break;
                case PlayDecision.ActionType.PUNG:
                    game.ProcessPung(player);
                    break;
                case PlayDecision.ActionType.KONG:
                    game.ProcessKong(player);
                    break;
                case PlayDecision.ActionType.SKIP:
                    game.ProcessSkip(player);
                    break;
                case PlayDecision.ActionType.DISCARD:
                    Tile discardTile = context.ResolveDiscardTile(decision.GetTile());
                    if (discardTile != null)
                    {
                        player.Plays(discardTile);
                        hoveredTile = null;
                        game.ProcessPlayed();
                    }
                    break;
            }
        }

        private void AddControllerDecisionLog(PlayDecision decision)
        {
            System.Text.StringBuilder message = new System.Text.StringBuilder(decision.GetSource());
            message.Append(" chose ").Append(decision.GetAction().ToString().ToLowerInvariant());
            if (decision.GetTile() != null)
            {
                message.Append(" ").Append(TileCodec.Display(decision.GetTile()));
            }
            if (!string.IsNullOrEmpty(decision.GetReason()))
            {
                message.Append(": ").Append(decision.GetReason());
            }
            game.GetLog().AddMessage(message.ToString());
        }

        // ---- rendering ----

        private void BuildStaticBackground()
        {
            RectTransform backgroundLayer =
                (RectTransform)UiFactory.CreateChild(gameRoot, "BackgroundLayer").transform;
            UiFactory.SetRect(backgroundLayer, 0, 0, Config.SCREEN_WIDTH, Config.SCREEN_HEIGHT);

            if (Config.USE_PHOTO_BACKGROUND && imageLoader.Background != null)
            {
                Sprite background = imageLoader.Background;
                float sourceWidth = background.texture.width;
                float sourceHeight = background.texture.height;
                float scale = Mathf.Max(Config.SCREEN_WIDTH / sourceWidth, Config.SCREEN_HEIGHT / sourceHeight);
                float scaledWidth = Mathf.Ceil(sourceWidth * scale);
                float scaledHeight = Mathf.Ceil(sourceHeight * scale);
                float x = (Config.SCREEN_WIDTH - scaledWidth) / 2f;
                float y = (Config.SCREEN_HEIGHT - scaledHeight) / 2f;

                backgroundImage = UiFactory.CreatePanel(backgroundLayer, "Photo", x, y, scaledWidth, scaledHeight,
                    Color.white);
                backgroundImage.sprite = background;
                backgroundImage.raycastTarget = false;

                UiFactory.CreatePanel(backgroundLayer, "Tint1", 0, 0, Config.SCREEN_WIDTH, Config.SCREEN_HEIGHT,
                    new Color32(0, 20, 32, 120)).raycastTarget = false;
                UiFactory.CreatePanel(backgroundLayer, "Tint2", 0, 0, Config.SCREEN_WIDTH, Config.SCREEN_HEIGHT,
                    new Color32(0, 0, 0, 45)).raycastTarget = false;
            }
            else
            {
                Image felt = UiFactory.CreatePanel(backgroundLayer, "Felt", 0, 0, Config.SCREEN_WIDTH,
                    Config.SCREEN_HEIGHT, Color.white);
                felt.sprite = CreateFeltSprite();
                felt.raycastTarget = false;
            }
        }

        private static Sprite CreateFeltSprite()
        {
            const int scale = 4; // 1 texel = 4 logical pixels, felt squares are 100 logical px
            int width = Config.SCREEN_WIDTH / scale;
            int height = Config.SCREEN_HEIGHT / scale;
            int rectSize = 100 / scale;
            Color32 baseGreen = new Color32(30, 100, 60, 255);
            Color32 highlightGreen = new Color32(60, 130, 80, 255);
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Color32[] pixels = new Color32[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int logicalY = y; // texture rows count from the bottom; pattern is symmetric enough
                    int row = logicalY / rectSize;
                    bool highlight = ((x / rectSize) % 2) == ((row % 2 == 0) ? 0 : 1);
                    pixels[y * width + x] = highlight ? highlightGreen : baseGreen;
                }
            }
            texture.SetPixels32(pixels);
            texture.filterMode = FilterMode.Point;
            texture.Apply();
            return UiFactory.SpriteFromTexture(texture);
        }

        private void RefreshUiIfChanged()
        {
            string stateKey = BuildRenderKey();
            if (stateKey == renderedStateKey)
            {
                return;
            }
            renderedStateKey = stateKey;
            Rebuild();
        }

        private string BuildRenderKey()
        {
            System.Text.StringBuilder key = new System.Text.StringBuilder();
            key.Append(game.GetActionVersion())
               .Append('|').Append(game.GetStatusText())
               .Append('|').Append(game.GetLastActionText())
               .Append('|').Append(playMode)
               .Append('|').Append(llmDecisionInFlight)
               .Append('|').Append(playTimerDeadline >= 0f)
               .Append('|').Append(game.GetWinner() == null ? "none" : game.GetWinner().GetName())
               .Append('|').Append(player.GetHand().GetHashCode())
               .Append('|').Append(player.GetHand().GetNewTile())
               .Append('|').Append(string.Join(",", player.GetStatus()));
            foreach (Player p in game.GetPlayers())
            {
                key.Append('|').Append(p.GetTable().ToList().Count)
                   .Append(':').Append(p.GetHand().GetPungKong().Count);
            }
            Player turnPlayer = game.GetTurnPlayer();
            Player lastActionPlayer = game.GetLastActionPlayer();
            key.Append('|').Append(turnPlayer == null ? -1 : turnPlayer.GetPosition())
               .Append('|').Append(lastActionPlayer == null ? -1 : lastActionPlayer.GetPosition());
            return key.ToString();
        }

        private void Rebuild()
        {
            for (int i = boardLayer.childCount - 1; i >= 0; i--)
            {
                Destroy(boardLayer.GetChild(i).gameObject);
            }
            hoveredTile = null;

            DrawPlayerAreas();
            DrawAvatars();
            DrawLogWindow();

            Dictionary<string, Rect> actionButtons = BuildActionButtonBounds();
            Dictionary<string, Rect> endGameButtons = BuildEndGameButtonBounds();
            Rect winningHandButton = BuildWinningHandButtonBounds();
            DrawStatusBanner(actionButtons, endGameButtons, winningHandButton);

            foreach (Player p in game.GetPlayers())
            {
                DrawTable(p.GetTable().ToList(), p.GetPosition());
                DrawPungKong(p.GetHand().GetPungKong(), p.GetPosition());
            }
            DrawPlayerHand();

            DrawRewardCelebration(endGameButtons);
        }

        private void DrawPlayerAreas()
        {
            DrawPlayerArea(Config.PLAYER_HAND_X, Config.PLAYER_HAND_Y, Config.PLAYER_HAND_WIDTH,
                Config.PLAYER_HAND_HEIGHT, "YOU", 0);
            DrawPlayerArea(Config.PLAYER_TABLE_X, Config.PLAYER_TABLE_Y, Config.PLAYER_TABLE_WIDTH,
                Config.PLAYER_TABLE_HEIGHT, "YOUR DISCARDS", 0);
            DrawPlayerArea(Config.AI1_TABLE_X, Config.AI1_TABLE_Y, Config.AI1_TABLE_WIDTH, Config.AI1_TABLE_HEIGHT,
                "NEXT: AI1", 1);
            DrawPlayerArea(Config.AI2_TABLE_X, Config.AI2_TABLE_Y, Config.AI2_TABLE_WIDTH, Config.AI2_TABLE_HEIGHT,
                "OPPOSITE: AI2", 2);
            DrawPlayerArea(Config.AI3_TABLE_X, Config.AI3_TABLE_Y, Config.AI3_TABLE_WIDTH, Config.AI3_TABLE_HEIGHT,
                "PREV: AI3", 3);
        }

        private void DrawPlayerArea(float x, float y, float width, float height, string label, int position)
        {
            Player turnPlayer = game.GetTurnPlayer();
            Player lastActionPlayer = game.GetLastActionPlayer();
            bool isCurrentTurn = turnPlayer != null && turnPlayer.GetPosition() == position;
            bool isLastAction = lastActionPlayer != null && lastActionPlayer.GetPosition() == position;

            if (isLastAction)
            {
                UiFactory.CreatePanel(boardLayer, "AreaFill " + label, x, y, width, height,
                    new Color32(255, 225, 80, 70)).raycastTarget = false;
            }
            else if (isCurrentTurn)
            {
                UiFactory.CreatePanel(boardLayer, "AreaFill " + label, x, y, width, height,
                    new Color32(90, 190, 255, 45)).raycastTarget = false;
            }

            Color borderColor;
            float thickness;
            if (isLastAction)
            {
                borderColor = HighlightYellow;
                thickness = 4f;
            }
            else if (isCurrentTurn)
            {
                borderColor = TurnBlue;
                thickness = 3f;
            }
            else
            {
                borderColor = Color.white;
                thickness = 1f;
            }
            UiFactory.CreateBorder(boardLayer, "AreaBorder " + label, x, y, width, height, borderColor, thickness);
            UiFactory.CreateText(boardLayer, "AreaLabel " + label, x + 10, y + 7, width - 20, 20, label, 15,
                Color.white, true);
        }

        private void DrawAvatars()
        {
            int[] aiTableX = { Config.AI1_TABLE_X, Config.AI2_TABLE_X, Config.AI3_TABLE_X };
            float aiAvatarY = Config.AI_TABLE_Y - Config.AI_AVATAR_HEIGHT - Config.AI_AVATAR_GAP_BELOW;
            for (int i = 0; i < aiTableX.Length && i < imageLoader.AiAvatars.Count; i++)
            {
                float x = aiTableX[i] + (Config.AI_TABLE_WIDTH - Config.AI_AVATAR_WIDTH) / 2f;
                CreateAvatar("AIAvatar" + (i + 1), imageLoader.AiAvatars[i], x, aiAvatarY,
                    Config.AI_AVATAR_WIDTH, Config.AI_AVATAR_HEIGHT);
            }

            if (imageLoader.PlayerAvatar != null)
            {
                CreateAvatar("PlayerAvatar", imageLoader.PlayerAvatar, Config.PLAYER_AVATAR_X,
                    Config.PLAYER_AVATAR_Y, Config.PLAYER_AVATAR_WIDTH, Config.PLAYER_AVATAR_HEIGHT);
            }
        }

        private void CreateAvatar(string name, Sprite sprite, float x, float y, float width, float height)
        {
            Image image = UiFactory.CreatePanel(boardLayer, name, x, y, width, height, Color.white);
            image.sprite = sprite;
            image.preserveAspect = false;
            image.raycastTarget = false;
        }

        private void DrawLogWindow()
        {
            const int top = 140;
            const int bottomPadding = 80;
            const int logWindowWidth = 400;
            const int textPadding = 20;
            int logWindowX = Config.SCREEN_WIDTH - logWindowWidth - 24;
            int logWindowHeight = Config.SCREEN_HEIGHT - top - bottomPadding;

            UiFactory.CreatePanel(boardLayer, "LogWindow", logWindowX, top, logWindowWidth, logWindowHeight,
                Color.black).raycastTarget = false;

            GameObject mask = UiFactory.CreateChild(boardLayer, "LogMask");
            UiFactory.SetRect((RectTransform)mask.transform, logWindowX + textPadding, top + textPadding,
                logWindowWidth - textPadding * 2, logWindowHeight - textPadding * 2);
            mask.AddComponent<RectMask2D>();
            Image maskImage = mask.AddComponent<Image>();
            maskImage.color = new Color(0f, 0f, 0f, 0f);
            maskImage.raycastTarget = false;

            List<string> messages = game.GetLog().GetLastXMessages(Config.LOG_ITEMS);
            Text text = UiFactory.CreateText(mask.transform, "LogText", 0, 0,
                logWindowWidth - textPadding * 2, 4000, string.Join("\n", messages), 15, Color.white, false,
                TextAnchor.UpperLeft);
            RectTransform textRt = (RectTransform)text.transform;
            textRt.anchorMin = new Vector2(0f, 1f);
            textRt.anchorMax = new Vector2(0f, 1f);
            textRt.pivot = new Vector2(0f, 1f);
            textRt.anchoredPosition = new Vector2(0f, 0f);
        }

        private Dictionary<string, Rect> BuildActionButtonBounds()
        {
            Dictionary<string, Rect> buttons = new Dictionary<string, Rect>();
            if (game.GetWinner() != null)
            {
                return buttons;
            }

            List<string> actions = new List<string>();
            if (player.GetStatus().Contains(PlayerStatusEnum.HU))
            {
                actions.Add("Hu");
            }
            if (player.ContainsChow())
            {
                actions.Add("Chow");
            }
            if (player.ContainsPung())
            {
                actions.Add("Pung");
            }
            if (player.ContainsKong())
            {
                actions.Add("Kong");
            }
            if (actions.Count > 0 && (!player.IsPlaying() || player.ContainsChouPungKong()))
            {
                actions.Add("Skip");
            }

            const int buttonWidth = 92;
            const int buttonHeight = 32;
            const int gap = 10;
            int totalWidth = actions.Count * buttonWidth + Math.Max(0, actions.Count - 1) * gap;
            int startX = Config.SCREEN_WIDTH - totalWidth - 64;
            const int y = 78;
            for (int i = 0; i < actions.Count; i++)
            {
                buttons[actions[i]] = new Rect(startX + i * (buttonWidth + gap), y, buttonWidth, buttonHeight);
            }
            return buttons;
        }

        private Dictionary<string, Rect> BuildEndGameButtonBounds()
        {
            Dictionary<string, Rect> buttons = new Dictionary<string, Rect>();
            if (!game.IsOver())
            {
                return buttons;
            }

            const int buttonHeight = 32;
            const int gap = 10;
            const int y = 78;
            if (game.GetWinner() != null)
            {
                List<string> labels = new List<string>();
                if (player.Equals(game.GetWinner()))
                {
                    labels.Add("Reward");
                }
                labels.Add("New game");
                labels.Add("Exit");

                int totalWidth = 0;
                foreach (string label in labels)
                {
                    totalWidth += GetEndGameButtonWidth(label);
                }
                totalWidth += Math.Max(0, labels.Count - 1) * gap;

                float x = BuildWinningHandButtonBounds().x - gap - totalWidth;
                foreach (string label in labels)
                {
                    int buttonWidth = GetEndGameButtonWidth(label);
                    buttons[label] = new Rect(x, y, buttonWidth, buttonHeight);
                    x += buttonWidth + gap;
                }
            }
            else
            {
                buttons["New game"] = new Rect(Config.SCREEN_WIDTH - 260, y, 125, buttonHeight);
                buttons["Exit"] = new Rect(Config.SCREEN_WIDTH - 125, y, 80, buttonHeight);
            }
            return buttons;
        }

        private static int GetEndGameButtonWidth(string label)
        {
            return label == "Exit" ? 80 : 105;
        }

        private Rect BuildWinningHandButtonBounds()
        {
            if (game.GetWinner() == null)
            {
                return new Rect();
            }
            return new Rect(Config.SCREEN_WIDTH - 260, 78, 205, 32);
        }

        private void DrawStatusBanner(Dictionary<string, Rect> actionButtons, Dictionary<string, Rect> endGameButtons,
                                      Rect winningHandButton)
        {
            const int x = 25;
            const int y = 25;
            const int width = Config.SCREEN_WIDTH - 50;
            const int height = 95;

            UiFactory.CreatePanel(boardLayer, "Banner", x, y, width, height,
                new Color32(0, 0, 0, 105)).raycastTarget = false;
            UiFactory.CreateBorder(boardLayer, "BannerBorder", x, y, width, height, BannerYellow, 2f);

            string lastActionText = game.GetLastActionText();
            if (!string.IsNullOrEmpty(lastActionText))
            {
                UiFactory.CreateText(boardLayer, "LastAction", x + 14, y + 10, width - 28, 26,
                    "Last action: " + lastActionText, 20, BannerYellow, true);
            }

            float statusTextWidth = width - 28;
            Rect llmButton = new Rect(Config.SCREEN_WIDTH - 350, 35, 145, 40);
            Rect autoButton = new Rect(Config.SCREEN_WIDTH - 190, 35, 145, 40);
            statusTextWidth = Mathf.Min(statusTextWidth, llmButton.x - x - 28);
            statusTextWidth = Mathf.Min(statusTextWidth, autoButton.x - x - 28);
            if (endGameButtons.Count > 0)
            {
                float leftMost = float.MaxValue;
                foreach (Rect bounds in endGameButtons.Values)
                {
                    leftMost = Mathf.Min(leftMost, bounds.x);
                }
                statusTextWidth = Mathf.Min(statusTextWidth, leftMost - x - 28);
            }
            else if (game.GetWinner() != null)
            {
                statusTextWidth = Mathf.Min(statusTextWidth, winningHandButton.x - x - 28);
            }
            else if (actionButtons.Count > 0)
            {
                float leftMost = float.MaxValue;
                foreach (Rect bounds in actionButtons.Values)
                {
                    leftMost = Mathf.Min(leftMost, bounds.x);
                }
                statusTextWidth = Mathf.Min(statusTextWidth, leftMost - x - 28);
            }

            Text statusText = UiFactory.CreateText(boardLayer, "StatusText", x + 14, y + 42, statusTextWidth, 46,
                game.GetStatusText(), 18, Color.white, false);
            statusText.verticalOverflow = VerticalWrapMode.Truncate;

            if (game.GetWinner() != null)
            {
                UiFactory.CreateActionButton(boardLayer, "View winning hand", winningHandButton.x,
                    winningHandButton.y, winningHandButton.width, winningHandButton.height, ShowWinningHandDialog);
            }

            foreach (KeyValuePair<string, Rect> entry in endGameButtons)
            {
                string label = entry.Key;
                UiFactory.CreateActionButton(boardLayer, label, entry.Value.x, entry.Value.y, entry.Value.width,
                    entry.Value.height, () => { CancelPlayTimer(); ProcessEndGameButton(label); });
            }

            if (game.GetWinner() == null)
            {
                foreach (KeyValuePair<string, Rect> entry in actionButtons)
                {
                    string label = entry.Key;
                    UiFactory.CreateActionButton(boardLayer, label, entry.Value.x, entry.Value.y, entry.Value.width,
                        entry.Value.height, () => { CancelPlayTimer(); ProcessActionButton(label); });
                }
            }

            DrawModeButton(autoButton, playMode == PlayMode.Auto, playMode == PlayMode.Llm,
                "Auto Play", "Auto Play: ON", ToggleAutoPlay);
            DrawModeButton(llmButton, playMode == PlayMode.Llm, playMode == PlayMode.Auto,
                "LLM Play", "LLM Play: ON", ToggleLlmPlay);
        }

        private void DrawModeButton(Rect bounds, bool enabled, bool disabled, string offLabel, string onLabel,
                                    Action onClick)
        {
            Color fill;
            Color edge;
            Color textColor;
            if (disabled)
            {
                fill = new Color32(210, 205, 155, 255);
                edge = new Color32(110, 110, 100, 255);
                textColor = new Color32(120, 120, 120, 255);
            }
            else if (enabled)
            {
                fill = new Color32(104, 220, 95, 255);
                edge = new Color32(65, 150, 255, 255);
                textColor = new Color32(20, 35, 30, 255);
            }
            else
            {
                fill = new Color32(255, 242, 78, 255);
                edge = new Color32(20, 35, 30, 255);
                textColor = new Color32(20, 35, 30, 255);
            }
            string label = enabled ? onLabel : offLabel;
            Button button = UiFactory.CreateStyledButton(boardLayer, label, bounds.x, bounds.y, bounds.width,
                bounds.height, fill, edge, textColor, onClick);
            button.interactable = !disabled;
        }

        private void DrawTable(List<Tile> tiles, int playerPosition)
        {
            if (playerPosition == 0)
            {
                for (int i = 0; i < tiles.Count; i++)
                {
                    float x = Config.PLAYER_TABLE_X + Config.TABLE_TILE_PADDING * i + Config.TABLE_TILE_WIDTH * i;
                    TileView.Create(boardLayer, this, imageLoader, tiles[i], x, Config.PLAYER_TABLE_Y,
                        Config.TABLE_TILE_WIDTH, Config.TABLE_TILE_HEIGHT, Color.white, false);
                }
            }
            else
            {
                int[] aiTableX = { Config.AI1_TABLE_X, Config.AI2_TABLE_X, Config.AI3_TABLE_X };
                int currentLine = 0;
                int currentTile = 0;
                foreach (Tile tile in tiles)
                {
                    float x = aiTableX[playerPosition - 1] + Config.TABLE_TILE_PADDING * currentTile
                              + Config.TABLE_TILE_WIDTH * currentTile;
                    float y = Config.AI_TABLE_Y + Config.TABLE_TILE_HEIGHT * currentLine
                              + Config.TABLE_TILE_PADDING * currentLine;
                    TileView.Create(boardLayer, this, imageLoader, tile, x, y,
                        Config.TABLE_TILE_WIDTH, Config.TABLE_TILE_HEIGHT, Color.white, false);
                    currentTile++;
                    if (currentTile == Config.AI_TABLE_NUM_TILES_PER_LINE)
                    {
                        currentLine++;
                        currentTile = 0;
                    }
                }
            }
        }

        private void DrawPungKong(List<Group> pungKong, int playerPosition)
        {
            if (pungKong.Count <= 0)
            {
                return;
            }
            int[] pungKongX =
            {
                Config.PLAYER_TABLE_X + Config.PLAYER_TABLE_WIDTH,
                Config.AI1_TABLE_X + Config.AI_TABLE_WIDTH,
                Config.AI2_TABLE_X + Config.AI_TABLE_WIDTH,
                Config.AI3_TABLE_X + Config.AI_TABLE_WIDTH
            };
            int[] pungKongY =
            {
                Config.PLAYER_TABLE_Y, Config.AI1_TABLE_Y, Config.AI2_TABLE_Y, Config.AI3_TABLE_Y
            };

            for (int i = 0; i < pungKong.Count; i++)
            {
                Group group = pungKong[i];
                Color color = group.GetCategory() == GroupEnum.PUNG ? Color.yellow
                    : new Color32(255, 165, 0, 255); // Swing Color.ORANGE
                List<Tile> groupTiles = group.ToList();
                int groupX = pungKongX[playerPosition];
                for (int j = 0; j < groupTiles.Count; j++)
                {
                    float x = groupX + Config.TABLE_TILE_WIDTH * j;
                    float y = pungKongY[playerPosition] + Config.TABLE_TILE_HEIGHT * i
                              + Config.TABLE_TILE_PADDING * i;
                    TileView.Create(boardLayer, this, imageLoader, groupTiles[j], x, y,
                        Config.TABLE_TILE_WIDTH, Config.TABLE_TILE_HEIGHT, color, false);
                }
            }
        }

        private void DrawPlayerHand()
        {
            List<Tile> hand = player.GetHand().ToList();
            for (int i = 0; i < hand.Count; i++)
            {
                float x = Config.PLAYER_HAND_X + Config.PLAYER_HAND_TILE_PADDING * i + Config.TILE_WIDTH * i;
                TileView.Create(boardLayer, this, imageLoader, hand[i], x, Config.PLAYER_HAND_TOP_INDENT,
                    Config.TILE_WIDTH, Config.TILE_HEIGHT, Color.white, true);
            }

            Tile newTile = player.GetHand().GetNewTile();
            if (newTile != null)
            {
                float x = Config.PLAYER_HAND_X + Config.PLAYER_HAND_TILE_PADDING * hand.Count
                          + Config.TILE_WIDTH * hand.Count + Config.FOURTEENTH_TILE_INDENT;
                TileView.Create(boardLayer, this, imageLoader, newTile, x, Config.PLAYER_HAND_TOP_INDENT,
                    Config.TILE_WIDTH, Config.TILE_HEIGHT, Color.white, true);
            }
        }

        private void DrawRewardCelebration(Dictionary<string, Rect> endGameButtons)
        {
            bool shouldShow = game.GetWinner() != null && player.Equals(game.GetWinner())
                              && endGameButtons.ContainsKey("Reward");
            if (!shouldShow)
            {
                return;
            }
            Rect rewardButton = endGameButtons["Reward"];
            RewardCelebration.CreateAnimationIcon(boardLayer, imageLoader, rewardButton.x, rewardButton.y,
                rewardButton.width);
            UiFactory.CreateBorder(boardLayer, "RewardHighlight", rewardButton.x - 3, rewardButton.y - 3,
                rewardButton.width + 6, rewardButton.height + 6, new Color32(255, 245, 120, 190), 2f);

            if (!rewardCelebrationPlayed)
            {
                rewardCelebrationPlayed = true;
                beeper.PlayRewardSound();
            }
        }

        private void ShowWinningHandDialog()
        {
            CancelPlayTimer();
            if (winningHandDialog != null)
            {
                Destroy(winningHandDialog);
            }
            winningHandDialog = WinningHandDialog.Show(dialogLayer, imageLoader, game);
        }
    }
}
