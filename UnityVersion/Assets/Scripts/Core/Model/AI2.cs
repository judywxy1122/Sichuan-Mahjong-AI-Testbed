using System.Collections.Generic;
using SichuanMahjong.Core.AiModel;

namespace SichuanMahjong.Core.Model
{
    public class AI2 : Player
    {
        private readonly IAI ai;

        public AI2(string name, List<Tile> hand, int position) : base(name, hand, position)
        {
            ai = new ProbabilityAI();
        }

        public override void PlayAction()
        {
            ai.SetHand(GetHand());
            Plays(ai.GetTileToPlay());
        }

        public override PlayerActionEnum? OtherAction(Tile tile)
        {
            ai.SetHand(GetHand());
            if (ContainsPung() && ai.ShouldPung(tile))
            {
                return PlayerActionEnum.PUNG;
            }
            if (ContainsChow() && ai.ShouldChow(tile))
            {
                return PlayerActionEnum.CHOW;
            }
            if (ContainsKong() && ai.ShouldKong(tile))
            {
                return PlayerActionEnum.KONG;
            }
            return PlayerActionEnum.SKIP;
        }
    }
}
