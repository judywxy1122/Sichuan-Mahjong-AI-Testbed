using System;
using System.Collections.Generic;
using System.IO;
using SichuanMahjong.Core;
using SichuanMahjong.Core.AiModel;
using SichuanMahjong.Core.Algorithm;
using SichuanMahjong.Core.Gameplay;
using SichuanMahjong.Core.Model;
using SichuanMahjong.Core.Validation;

namespace SichuanMahjong.Harness
{
    /// <summary>
    /// Headless verification harness for the C# core port. Not part of the
    /// Unity build — run with:
    ///   dotnet run -- [tables-dir] [command] [args...]
    /// Commands: tests | simulate N | parity hands.txt
    /// </summary>
    public static class Program
    {
        public static int Main(string[] args)
        {
            string tablesDir = args.Length > 0 ? args[0] : "../../Assets/StreamingAssets/probability";
            string command = args.Length > 1 ? args[1] : "tests";

            Console.WriteLine("Loading tables from " + tablesDir + " ...");
            DateTime start = DateTime.Now;
            AITableJian.Load(File.ReadLines(Path.Combine(tablesDir, "majiang_ai_jian.txt")));
            AITableFeng.Load(File.ReadLines(Path.Combine(tablesDir, "majiang_ai_feng.txt")));
            AITable.Load(File.ReadLines(Path.Combine(tablesDir, "majiang_ai_normal.txt")));
            Console.WriteLine("Tables loaded in " + (DateTime.Now - start).TotalSeconds.ToString("F1") + "s ("
                              + AITable.table.Count + " normal keys)");

            switch (command)
            {
                case "tests":
                    RunTests();
                    return 0;
                case "simulate":
                    return Simulate(args.Length > 2 ? int.Parse(args[2]) : 20);
                case "parity":
                    return Parity(args[2]);
                default:
                    Console.WriteLine("unknown command " + command);
                    return 2;
            }
        }

        /// <summary>Ports of the three Java test mains.</summary>
        private static void RunTests()
        {
            // GameTurnTest
            List<Player> players = new List<Player>
            {
                new Player("player", new List<Tile>(), 0),
                new AI1("ai1", new List<Tile>(), 1),
                new AI2("ai2", new List<Tile>(), 2),
                new AI3("ai3", new List<Tile>(), 3)
            };
            GameTurn gameTurn = new GameTurn(players, players[1]);
            Console.WriteLine("GameTurnTest roundsUntilPlayer=" + gameTurn.GetRoundsUntilPlayer());

            // TestAI
            List<Tile> hand = new List<Tile>
            {
                new Tile(TileTypeEnum.C, 6), new Tile(TileTypeEnum.C, 2), new Tile(TileTypeEnum.C, 7),
                new Tile(TileTypeEnum.C, 7), new Tile(TileTypeEnum.C, 8), new Tile(TileTypeEnum.C, 8),
                new Tile(TileTypeEnum.C, 1), new Tile(TileTypeEnum.C, 2), new Tile(TileTypeEnum.C, 3),
                new Tile(TileTypeEnum.C, 3), new Tile(TileTypeEnum.C, 4), new Tile(TileTypeEnum.C, 5),
                new Tile(TileTypeEnum.C, 5)
            };
            ProbabilityAI ai = new ProbabilityAI();
            ai.SetHand(new HandTiles(hand));
            Console.WriteLine("TestAI tileToPlay=" + ai.GetTileToPlay());

            // StatusCheckerInitTest
            List<Tile> tiles = new List<Tile>
            {
                new Tile(TileTypeEnum.B, 1), new Tile(TileTypeEnum.B, 1), new Tile(TileTypeEnum.B, 1),
                new Tile(TileTypeEnum.B, 3), new Tile(TileTypeEnum.B, 3), new Tile(TileTypeEnum.B, 3),
                new Tile(TileTypeEnum.B, 5), new Tile(TileTypeEnum.B, 5), new Tile(TileTypeEnum.C, 2),
                new Tile(TileTypeEnum.C, 3), new Tile(TileTypeEnum.C, 7), new Tile(TileTypeEnum.C, 8),
                new Tile(TileTypeEnum.C, 9), new Tile(TileTypeEnum.C, 7)
            };
            Player p2 = new Player("TestPlayer2", tiles, 0);
            p2.GetHand().AddHiddenKong();
            p2.AddTile(new Tile(TileTypeEnum.B, 2));
            new PlayerStatusChecker(p2, new Tile(TileTypeEnum.C, 6));
            Console.WriteLine("StatusCheckerInitTest hand=" + p2.GetHand());
            Console.WriteLine("StatusCheckerInitTest status=[" + string.Join(", ", p2.GetStatus()) + "]");
        }

        /// <summary>
        /// Runs full games with the human seat driven by AutoPlayController,
        /// mirroring what GamePanel does in Auto Play mode.
        /// </summary>
        private static int Simulate(int games)
        {
            int wins = 0, draws = 0, errors = 0;
            Dictionary<string, int> winners = new Dictionary<string, int>();
            for (int i = 0; i < games; i++)
            {
                Game game = new Game();
                Player human = game.GetPlayers()[0];
                AutoPlayController controller = new AutoPlayController();
                int safety = 0;
                while (!game.IsOver() && safety++ < 2000)
                {
                    PlayerActionContext context = new PlayerActionContext(game, human);
                    if (!context.IsActionNeeded())
                    {
                        Console.WriteLine("game " + i + ": stuck without human action needed");
                        errors++;
                        break;
                    }
                    PlayDecision decision = controller.Choose(game, human);
                    if (!context.IsLegal(decision))
                    {
                        Console.WriteLine("game " + i + ": controller returned illegal decision "
                                          + decision.GetAction());
                        errors++;
                        break;
                    }
                    switch (decision.GetAction())
                    {
                        case PlayDecision.ActionType.HU:
                            game.ProcessHu(human);
                            break;
                        case PlayDecision.ActionType.CHOW:
                            game.ProcessChou(human);
                            break;
                        case PlayDecision.ActionType.PUNG:
                            game.ProcessPung(human);
                            break;
                        case PlayDecision.ActionType.KONG:
                            game.ProcessKong(human);
                            break;
                        case PlayDecision.ActionType.SKIP:
                            game.ProcessSkip(human);
                            break;
                        case PlayDecision.ActionType.DISCARD:
                            Tile discardTile = context.ResolveDiscardTile(decision.GetTile());
                            if (discardTile == null)
                            {
                                Console.WriteLine("game " + i + ": unresolvable discard");
                                errors++;
                                safety = 999999;
                                break;
                            }
                            human.Plays(discardTile);
                            game.ProcessPlayed();
                            break;
                        default:
                            Console.WriteLine("game " + i + ": NONE decision: " + decision.GetReason());
                            errors++;
                            safety = 999999;
                            break;
                    }
                }
                if (game.GetWinner() != null)
                {
                    wins++;
                    string name = game.GetWinner().GetName();
                    winners[name] = winners.TryGetValue(name, out int c) ? c + 1 : 1;
                }
                else if (game.IsOver())
                {
                    if (game.GetStatusText().Contains("error"))
                    {
                        errors++;
                        Console.WriteLine("game " + i + ": " + game.GetStatusText());
                    }
                    else
                    {
                        draws++;
                    }
                }
                if ((i + 1) % 10 == 0)
                {
                    Console.WriteLine("... " + (i + 1) + "/" + games + " games done");
                }
            }
            Console.WriteLine("simulated=" + games + " wins=" + wins + " draws=" + draws + " errors=" + errors);
            foreach (KeyValuePair<string, int> entry in winners)
            {
                Console.WriteLine("  winner " + entry.Key + ": " + entry.Value);
            }
            return errors == 0 ? 0 : 1;
        }

        /// <summary>
        /// Reads hands (one per line, comma-separated tile codes like C7,B1)
        /// and prints the ProbabilityAI discard plus pung/kong votes for the
        /// last tile — output is diffed against the Java reference dump.
        /// </summary>
        private static int Parity(string handsFile)
        {
            foreach (string line in File.ReadLines(handsFile))
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }
                string[] codes = line.Trim().Split(',');
                List<Tile> tiles = new List<Tile>();
                foreach (string code in codes)
                {
                    Tile tile = TileCodec.FromCode(code.Trim());
                    if (tile == null)
                    {
                        Console.WriteLine("bad tile code: " + code);
                        return 2;
                    }
                    tiles.Add(tile);
                }
                Tile claim = tiles[tiles.Count - 1];
                List<Tile> handTiles = tiles.GetRange(0, tiles.Count - 1);

                ProbabilityAI ai = new ProbabilityAI();
                ai.SetHand(new HandTiles(new List<Tile>(handTiles)));
                Tile discard = ai.GetTileToPlay();
                bool pung = ai.ShouldPung(claim);
                bool kong = ai.ShouldKong(claim);

                Player checkedPlayer = new Player("p", new List<Tile>(handTiles), 0);
                new PlayerStatusChecker(checkedPlayer, claim);
                List<string> status = new List<string>();
                foreach (PlayerStatusEnum s in checkedPlayer.GetStatus())
                {
                    status.Add(s.ToString());
                }
                status.Sort(StringComparer.Ordinal);

                Console.WriteLine(line.Trim() + " => discard=" + discard
                                  + " pung=" + pung + " kong=" + kong
                                  + " status=" + string.Join("|", status));
            }
            return 0;
        }
    }
}
