using SichuanMahjong.Core.Model;

namespace SichuanMahjong.Core.Gameplay
{
    public interface IPlayController
    {
        string GetName();

        PlayDecision Choose(Game game, Player player);
    }
}
