namespace SichuanMahjong.Core.Algorithm
{
    public class AITableInfo
    {
        public bool jiang;
        public double p;

        public override string ToString()
        {
            return " 将" + (jiang ? "1" : "0") + " 几率" + p;
        }
    }
}
