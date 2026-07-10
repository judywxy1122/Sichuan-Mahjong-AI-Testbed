using System.Collections.Generic;
using SichuanMahjong.Core.Algorithm;
using SichuanMahjong.Core.Model;

namespace SichuanMahjong.Core.AiModel
{
    public class ProbabilityAI : IAI
    {
        private List<Tile> hand;
        private List<int> cards;

        public void SetHand(HandTiles hand)
        {
            this.hand = hand.ToList();
            if (hand.GetNewTile() != null)
            {
                this.hand.Add(hand.GetNewTile());
            }
            cards = TilesToCards(this.hand);
        }

        public bool ShouldPung(Tile tile)
        {
            int card = TileToCard(tile);
            return AIUtil.PengAI(cards, new List<int>(), card, 0.00);
        }

        public bool ShouldKong(Tile tile)
        {
            int card = TileToCard(tile);
            return AIUtil.GangAI(cards, new List<int>(), card, 0.00);
        }

        public bool ShouldChow(Tile tile)
        {
            return true;
        }

        public bool ShouldSkip(Tile tile)
        {
            return !ShouldPung(tile) && !ShouldKong(tile) && !ShouldChow(tile);
        }

        public Tile GetTileToPlay()
        {
            return CardToTile(AIUtil.OutAI(cards, new List<int>()));
        }

        private int TileToCard(Tile tile)
        {
            if (tile.GetTileType() == TileTypeEnum.C)
            {
                return MaJiangDef.ToCard(MaJiangDef.TYPE_WAN, tile.GetNumber() - 1);
            }
            if (tile.GetTileType() == TileTypeEnum.D)
            {
                return MaJiangDef.ToCard(MaJiangDef.TYPE_TONG, tile.GetNumber() - 1);
            }
            return MaJiangDef.ToCard(MaJiangDef.TYPE_TIAO, tile.GetNumber() - 1);
        }

        private Tile CardToTile(int card)
        {
            if (card >= 1 && card <= 9)
            {
                return new Tile(TileTypeEnum.C, card);
            }
            if (card >= 10 && card <= 18)
            {
                return new Tile(TileTypeEnum.D, card - 9);
            }
            return new Tile(TileTypeEnum.B, card - 18);
        }

        private List<int> TilesToCards(List<Tile> tiles)
        {
            List<int> result = new List<int>();
            foreach (Tile tile in tiles)
            {
                result.Add(TileToCard(tile));
            }
            return result;
        }
    }
}
