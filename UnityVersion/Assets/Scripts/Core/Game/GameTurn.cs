using System.Collections.Generic;
using SichuanMahjong.Core.Model;

namespace SichuanMahjong.Core
{
    public class GameTurn
    {
        private int round;
        private readonly List<Player> turns;
        private readonly List<Player> original;

        public GameTurn(List<Player> players, Player startingPlayer)
        {
            round = 0;
            original = players;
            int startingIndex = players.IndexOf(startingPlayer);
            turns = new List<Player> { startingPlayer };
            for (int i = 1; i <= 3; i++)
            {
                int nextIndex = (startingIndex + i) % players.Count;
                turns.Add(players[nextIndex]);
            }
        }

        public Player Next()
        {
            Player player = turns[0];
            turns.RemoveAt(0);
            turns.Add(player);
            round++;
            return player;
        }

        public Player Peek()
        {
            return turns[0];
        }

        public List<Player> Peek3()
        {
            return new List<Player>(turns.GetRange(0, 3));
        }

        public Player GetPlayerAfter(Player player)
        {
            int index = original.IndexOf(player);
            int nextIndex = (index + 1) % original.Count;
            return original[nextIndex];
        }

        public int GetRound()
        {
            return round;
        }

        public int GetRoundsUntilPlayer()
        {
            Player player = original[0];
            return turns.IndexOf(player) + 1;
        }
    }
}
