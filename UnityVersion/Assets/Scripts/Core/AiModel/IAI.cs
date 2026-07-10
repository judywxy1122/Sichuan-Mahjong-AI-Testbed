using SichuanMahjong.Core.Model;

namespace SichuanMahjong.Core.AiModel
{
    /// <summary>Port of the Java aimodel.AI interface.</summary>
    public interface IAI
    {
        void SetHand(HandTiles hand);
        bool ShouldPung(Tile tile);
        bool ShouldKong(Tile tile);
        bool ShouldChow(Tile tile);
        bool ShouldSkip(Tile tile);
        Tile GetTileToPlay();
    }
}
