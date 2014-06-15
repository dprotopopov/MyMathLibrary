using System.Collections.Generic;

namespace MyMath.GF2
{
    public class BooleanVectorIndexOfTrueComparer : IComparer<BooleanVector>
    {
        public int Compare(BooleanVector x, BooleanVector y)
        {
            return x.IndexOf(true) - y.IndexOf(true);
        }
    }
}