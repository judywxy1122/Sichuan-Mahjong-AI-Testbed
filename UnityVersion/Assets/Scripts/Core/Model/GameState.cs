using System.Collections.Generic;

namespace SichuanMahjong.Core.Model
{
    public class GameState
    {
        private readonly Player turnPlayer;
        private readonly List<Player> allPlayers;
        private readonly int round;

        private readonly List<Tile> playerHand;
        private readonly List<Tile> playerKong;
        private readonly List<Tile> playerPung;
        private readonly Tile playerNewTile;
        private readonly List<Tile> playerTable;
        private readonly List<Tile> ai1Table;
        private readonly List<Tile> ai2Table;
        private readonly List<Tile> ai3Table;
        private readonly List<Tile> tilesToDraw;

        public GameState(Player turnPlayer, List<Player> allPlayers, int round, List<Tile> playerHand,
                         List<Tile> playerKong, List<Tile> playerPung, Tile playerNewTile, List<Tile> playerTable,
                         List<Tile> ai1Table, List<Tile> ai2Table, List<Tile> ai3Table)
        {
            this.turnPlayer = turnPlayer;
            this.allPlayers = allPlayers;
            this.round = round;
            this.playerHand = playerHand;
            this.playerKong = playerKong;
            this.playerPung = playerPung;
            this.playerNewTile = playerNewTile;
            this.playerTable = playerTable;
            this.ai1Table = ai1Table;
            this.ai2Table = ai2Table;
            this.ai3Table = ai3Table;

            tilesToDraw = new List<Tile>(playerHand);
            if (this.playerNewTile != null)
            {
                tilesToDraw.Add(this.playerNewTile);
            }
        }

        public List<Tile> GetPlayerHand()
        {
            List<Tile> handWithNewTile = new List<Tile>(playerHand);
            if (playerNewTile != null)
            {
                handWithNewTile.Add(playerNewTile);
            }
            return handWithNewTile;
        }

        public Tile GetPlayerNewTile()
        {
            return playerNewTile;
        }

        public List<Tile> GetTilesToDraw()
        {
            return tilesToDraw;
        }
    }
}
