using System;
using System.Collections.Generic;
using SichuanMahjong.Core.Model;
using SichuanMahjong.Core.Validation;

namespace SichuanMahjong.Core
{
    public class Game
    {
        private bool ended;
        private readonly List<Tile> tiles;
        private readonly List<Player> players;
        private readonly Player player;
        private Player turnPlayer;
        private Player lastActionPlayer;
        private Player lastDiscardPlayer;
        private Player winner;
        private Tile lastPlayedTile;
        private Tile winningTile;
        private string lastActionText;
        private string statusText;
        private int actionVersion;
        private GameTurn gameTurn;
        private readonly Log log;
        private static readonly Random random = new Random();

        public Game()
        {
            ended = false;
            lastActionText = "";
            statusText = "Game starting...";
            actionVersion = 0;
            log = new Log();
            tiles = new List<Tile>();
            players = new List<Player>
            {
                new Player("player", new List<Tile>(), 0),
                new AI1("ai1", new List<Tile>(), 1),
                new AI2("ai2", new List<Tile>(), 2),
                new AI3("ai3", new List<Tile>(), 3)
            };
            player = players[0];
            TileTypeEnum[] categories = { TileTypeEnum.B, TileTypeEnum.C, TileTypeEnum.D };
            foreach (TileTypeEnum category in categories)
            {
                for (int i = 1; i <= 9; i++)
                {
                    for (int j = 0; j < 4; j++)
                    {
                        tiles.Add(new Tile(category, i));
                    }
                }
            }
            Shuffle(tiles);
            log.AddMessage("Tiles created and shuffled");
            Deal();
            Next();
            log.AddMessage("AIs played their first turn");
        }

        private static void Shuffle(List<Tile> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                Tile tmp = list[i];
                list[i] = list[j];
                list[j] = tmp;
            }
        }

        private void Deal()
        {
            foreach (Player p in players)
            {
                List<Tile> hand = new List<Tile>();
                for (int i = 0; i < 13; i++)
                {
                    hand.Add(GetNextTile());
                }
                p.SetHand(new HandTiles(hand));
            }
            int randomIndex = random.Next(players.Count);
            Player startingPlayer = players[randomIndex];
            startingPlayer.AddTile(GetNextTile());
            startingPlayer.SetPlayingStatus();
            lastActionPlayer = startingPlayer;
            lastActionText = startingPlayer.GetName() + " starts";
            statusText = startingPlayer.GetName() + " starts the game.";

            log.AddMessage("Tiles dealt");
            log.AddMessage(startingPlayer.GetName() + " starts the game");
            gameTurn = new GameTurn(players, startingPlayer);
        }

        private Tile GetNextTile()
        {
            if (tiles.Count == 0)
            {
                EndDrawGame();
                return null;
            }
            Tile tile = tiles[0];
            tiles.RemoveAt(0);
            return tile;
        }

        private void Next()
        {
            if (ended)
            {
                return;
            }
            turnPlayer = gameTurn.Next();
            if (turnPlayer.GetStatus().Contains(PlayerStatusEnum.HU))
            {
                ended = true;
                return;
            }
            log.AddMessage(turnPlayer.GetName() + "'s turn");
            Tile drawnTile = null;
            if (gameTurn.GetRound() != 1)
            {
                drawnTile = GetNextTile();
                if (drawnTile == null)
                {
                    return;
                }
                turnPlayer.AddTile(drawnTile);
            }
            turnPlayer.SetPlayingStatus();
            new PlayerStatusChecker(turnPlayer, turnPlayer.GetHand().GetNewTile());
            if (turnPlayer == player)
            {
                statusText = BuildPlayerTurnPrompt(drawnTile);
            }
            else
            {
                statusText = turnPlayer.GetName() + " is taking a turn.";
            }
            Action();
        }

        private void Action()
        {
            if (ended)
            {
                return;
            }
            if (turnPlayer == player)
            {
                log.AddMessage("directing to " + turnPlayer.GetName() + " for action");
            }
            else
            {
                try
                {
                    turnPlayer.PlayAction();
                    ProcessPlayed();
                }
                catch (Exception e)
                {
                    EndErroredGame(turnPlayer, e);
                }
            }
        }

        public void ProcessPlayed()
        {
            List<Player> next3Players = gameTurn.Peek3();
            Tile playedTile = turnPlayer.GetTable().GetLast();
            lastDiscardPlayer = turnPlayer;
            lastPlayedTile = playedTile;
            foreach (Player p in next3Players)
            {
                new PlayerStatusChecker(p, playedTile);
            }
            RecordAction(turnPlayer, "played " + playedTile);
            log.AddMessage(turnPlayer.GetName() + " played " + turnPlayer.GetTable().GetLast());
            turnPlayer.SetWaitingStatus();

            foreach (Player p in next3Players)
            {
                if (p.ContainsHu() && p == player)
                {
                    statusText = BuildResponsePrompt(turnPlayer, playedTile);
                    log.AddMessage("For " + playedTile + ", press h to hu, or s to skip");
                    return;
                }
                if (p.ContainsHu())
                {
                    ProcessHu(p);
                    return;
                }
                if (p.ContainsChouPungKong() && p == player)
                {
                    statusText = BuildResponsePrompt(turnPlayer, playedTile);
                    log.AddMessage("For " + playedTile + ", press c to chow, p to pung, k to kong, or s to skip");
                    return;
                }
                if (p.ContainsChouPungKong())
                {
                    PlayerActionEnum? action = p.OtherAction(playedTile);
                    if (action == PlayerActionEnum.CHOW)
                    {
                        ProcessChou(p);
                        return;
                    }
                    if (action == PlayerActionEnum.PUNG)
                    {
                        ProcessPung(p);
                        return;
                    }
                    if (action == PlayerActionEnum.KONG)
                    {
                        ProcessKong(p);
                        return;
                    }
                    p.ClearStatus();
                    RecordAction(p, "skip");
                    log.AddMessage(p.GetName() + " skipped");
                }
            }
            Next();
        }

        public void ProcessHu(Player huPlayer)
        {
            Tile tile = huPlayer.GetHand().GetNewTile();
            if (huPlayer != turnPlayer && lastPlayedTile != null)
            {
                tile = lastPlayedTile;
                if (turnPlayer != null && turnPlayer.GetTable().ToList().Count > 0
                    && turnPlayer.GetTable().GetLast().Equals(lastPlayedTile))
                {
                    turnPlayer.GetTable().RemoveLast();
                }
                if (huPlayer.GetHand().GetNewTile() == null)
                {
                    huPlayer.AddTile(tile);
                }
            }
            winningTile = tile;
            winner = huPlayer;
            ended = true;
            RecordAction(huPlayer, "wins");
            statusText = huPlayer.GetName() + " wins"
                         + (tile == null ? "" : " on " + FormatTile(tile))
                         + ". Click View winning hand to inspect the result.";
            log.AddMessage(huPlayer.GetName() + " wins");
            CoreEnv.Println(huPlayer.GetHand().ToString());
        }

        public void ProcessChou(Player chowPlayer)
        {
            chowPlayer.SetHuStatus();
            turnPlayer.GetTable().RemoveLast();
            RecordAction(chowPlayer, "chow");
            log.AddMessage(chowPlayer.GetName() + " chou");
            ProcessHu(chowPlayer);
        }

        public void ProcessPung(Player pungPlayer)
        {
            Tile claimedTile = turnPlayer.GetTable().GetLast();
            pungPlayer.GetHand().AddPung(claimedTile);
            turnPlayer.GetTable().RemoveLast();
            RecordAction(pungPlayer, "pung");
            gameTurn = new GameTurn(players, pungPlayer);
            turnPlayer = gameTurn.Next();
            turnPlayer.SetPlayingStatus();
            turnPlayer.ClearStatus();
            statusText = BuildClaimTurnPrompt(pungPlayer, "punged", claimedTile);
            log.AddMessage(turnPlayer.GetName() + " pung, now play 1 tile");
            Action();
        }

        public void ProcessKong(Player kongPlayer)
        {
            Tile claimedTile = turnPlayer.GetTable().GetLast();
            if (kongPlayer.GetStatus().Contains(PlayerStatusEnum.NORMAL_KONG))
            {
                kongPlayer.GetHand().AddNormalKong(claimedTile);
                turnPlayer.GetTable().RemoveLast();
            }
            else if (kongPlayer.GetStatus().Contains(PlayerStatusEnum.ADD_KONG))
            {
                claimedTile = kongPlayer.GetHand().GetNewTile();
                kongPlayer.GetHand().AddAddKong();
            }
            else if (kongPlayer.GetStatus().Contains(PlayerStatusEnum.HIDDEN_KONG))
            {
                claimedTile = kongPlayer.GetHand().GetNewTile();
                kongPlayer.GetHand().AddHiddenKong();
            }
            else
            {
                throw new InvalidOperationException("Invalid kong");
            }
            RecordAction(kongPlayer, "kong");
            Tile drawnTile = GetNextTile();
            if (drawnTile == null)
            {
                return;
            }
            kongPlayer.AddTile(drawnTile);
            kongPlayer.SetPlayingStatus();
            new PlayerStatusChecker(kongPlayer, kongPlayer.GetHand().GetNewTile());
            gameTurn = new GameTurn(players, kongPlayer);
            turnPlayer = gameTurn.Next();
            turnPlayer.SetPlayingStatus();
            if (kongPlayer == player)
            {
                statusText = BuildPostKongPlayerPrompt(claimedTile, drawnTile);
                log.AddMessage("player kong, now choose action or play 1 tile");
                return;
            }

            if (kongPlayer.ContainsHu())
            {
                ProcessHu(kongPlayer);
                return;
            }
            turnPlayer.ClearStatus();
            statusText = BuildClaimTurnPrompt(kongPlayer, "konged", claimedTile);
            log.AddMessage(turnPlayer.GetName() + " kong, now play 1 tile");
            Action();
        }

        public void ProcessSkip(Player skipPlayer)
        {
            if (skipPlayer == turnPlayer && skipPlayer.IsPlaying())
            {
                skipPlayer.ClearKongStatus();
                skipPlayer.SetPlayingStatus();
                RecordAction(skipPlayer, "skip kong");
                statusText = BuildPlayerTurnPrompt(skipPlayer.GetHand().GetNewTile());
                log.AddMessage(skipPlayer.GetName() + " skipped kong, now play 1 tile");
                return;
            }
            skipPlayer.ClearStatus();
            gameTurn.GetPlayerAfter(turnPlayer).SetPlayingStatus();
            RecordAction(skipPlayer, "skip");
            log.AddMessage(skipPlayer.GetName() + " skipped");
            Next();
        }

        private void RecordAction(Player actionPlayer, string actionText)
        {
            lastActionPlayer = actionPlayer;
            lastActionText = actionPlayer.GetName() + " " + actionText;
            actionVersion++;
            CoreEnv.Beep();
        }

        private void EndDrawGame()
        {
            if (ended)
            {
                return;
            }
            log.AddMessage("No more tiles");
            ended = true;
            lastActionPlayer = turnPlayer;
            lastActionText = "wall exhausted";
            statusText = "No more tiles. The hand ends in a draw.";
            actionVersion++;
            CoreEnv.Beep();
        }

        private void EndErroredGame(Player actor, Exception e)
        {
            if (ended)
            {
                return;
            }
            ended = true;
            lastActionPlayer = actor;
            lastActionText = actor.GetName() + " error";
            statusText = actor.GetName() + " hit an AI/action error: " + e.Message
                         + ". Start a new game.";
            actionVersion++;
            log.AddMessage(statusText);
            CoreEnv.Println(e.ToString());
            CoreEnv.Beep();
        }

        private string BuildPlayerTurnPrompt(Tile drawnTile)
        {
            Tile currentNewTile = drawnTile != null ? drawnTile : player.GetHand().GetNewTile();
            System.Text.StringBuilder prompt = new System.Text.StringBuilder();
            if (drawnTile != null && lastDiscardPlayer != null && lastDiscardPlayer != player && lastPlayedTile != null)
            {
                prompt.Append(lastDiscardPlayer.GetName())
                      .Append(" played ")
                      .Append(FormatTile(lastPlayedTile))
                      .Append("; you cannot respond, drew ")
                      .Append(FormatTile(drawnTile))
                      .Append(". ");
            }
            else if (currentNewTile != null)
            {
                prompt.Append("You drew ")
                      .Append(FormatTile(currentNewTile))
                      .Append(". ");
            }
            prompt.Append("Click one tile to discard.");

            List<string> actions = new List<string>();
            if (player.GetStatus().Contains(PlayerStatusEnum.HU))
            {
                actions.Add("H Hu");
            }
            if (player.ContainsKong())
            {
                actions.Add("K Kong");
                actions.Add("S Skip Kong");
            }
            if (actions.Count > 0)
            {
                prompt.Append(" Available: ").Append(string.Join(" / ", actions)).Append(".");
            }
            return prompt.ToString();
        }

        private string BuildPostKongPlayerPrompt(Tile kongTile, Tile drawnTile)
        {
            System.Text.StringBuilder prompt = new System.Text.StringBuilder("You konged ");
            prompt.Append(FormatTile(kongTile))
                  .Append(" and drew ")
                  .Append(FormatTile(drawnTile))
                  .Append(". Click one tile to discard.");

            List<string> actions = new List<string>();
            if (player.GetStatus().Contains(PlayerStatusEnum.HU))
            {
                actions.Add("H Hu");
            }
            if (player.ContainsKong())
            {
                actions.Add("K Kong");
                actions.Add("S Skip Kong");
            }
            if (actions.Count > 0)
            {
                prompt.Append(" Available: ").Append(string.Join(" / ", actions)).Append(".");
            }
            return prompt.ToString();
        }

        private string BuildResponsePrompt(Player actor, Tile tile)
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
            return actor.GetName() + " played " + FormatTile(tile)
                   + "; choose response: " + string.Join(" / ", actions) + ".";
        }

        private string BuildClaimTurnPrompt(Player claimPlayer, string action, Tile tile)
        {
            if (claimPlayer == player)
            {
                return "You " + action + " " + FormatTile(tile) + ". Click one tile to discard.";
            }
            return claimPlayer.GetName() + " " + action + " " + FormatTile(tile) + " and must discard.";
        }

        private static string FormatTile(Tile tile)
        {
            if (tile == null)
            {
                return "";
            }
            string[] chineseNumbers = { "", "一", "二", "三", "四", "五", "六", "七", "八", "九" };
            if (tile.GetTileType() == TileTypeEnum.B && tile.GetNumber() == 1)
            {
                return "幺鸡";
            }
            return chineseNumbers[tile.GetNumber()] + tile.GetTileType().GetChinese();
        }

        public void ShowInvalidInput(string message)
        {
            statusText = message;
            CoreEnv.Beep();
        }

        public void ShowStatus(string message)
        {
            statusText = message;
        }

        // Getters
        public GameState GetGameState()
        {
            HandTiles playerHand = players[0].GetHand();

            List<Tile> playerHandList = playerHand.ToList();

            List<Tile> playerTable = players[0].GetTable().ToList();
            List<Tile> ai1Table = players[1].GetTable().ToList();
            List<Tile> ai2Table = players[2].GetTable().ToList();
            List<Tile> ai3Table = players[3].GetTable().ToList();

            List<Tile> kong = new List<Tile>();
            playerHand.GetKong().ForEach(group => kong.AddRange(group.ToList()));
            List<Tile> pung = new List<Tile>();
            playerHand.GetPung().ForEach(group => pung.AddRange(group.ToList()));
            Tile newTile = playerHand.GetNewTile();

            return new GameState(turnPlayer, players, gameTurn.GetRound(), playerHandList,
                kong, pung, newTile, playerTable, ai1Table, ai2Table, ai3Table);
        }

        public List<Player> GetPlayers()
        {
            return players;
        }

        public Log GetLog()
        {
            return log;
        }

        public bool IsOver()
        {
            return ended;
        }

        public Player GetTurnPlayer()
        {
            return turnPlayer;
        }

        public Player GetLastActionPlayer()
        {
            return lastActionPlayer;
        }

        public Player GetWinner()
        {
            return winner;
        }

        public Tile GetWinningTile()
        {
            return winningTile;
        }

        public Tile GetLastPlayedTile()
        {
            return lastPlayedTile;
        }

        public string GetLastActionText()
        {
            return lastActionText;
        }

        public string GetStatusText()
        {
            return statusText;
        }

        public int GetActionVersion()
        {
            return actionVersion;
        }
    }
}
