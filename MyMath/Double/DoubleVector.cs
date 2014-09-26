using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyMath.Double
{
    public class DoubleVector : Vector<double>
    {
        public DoubleVector(IEnumerable<double> doubles)
            : base(doubles)
        {
        }

        public DoubleVector()
        {
        }

        public DoubleVector(double value)
            : base(value)
        {
        }

        public static DoubleVector operator +(DoubleVector a, DoubleVector b)
        {
            int count = Math.Max(a.Count, b.Count);
            var result = new double[count];
            Parallel.ForEach(Enumerable.Range(0, count), i =>
            {
                double x = (i < a.Count) ? a.ElementAt(i) : default(double);
                double y = (i < b.Count) ? b.ElementAt(i) : default(double);
                result[i] = x + y;
            });
            return new DoubleVector(result);
        }

        public static DoubleVector operator -(DoubleVector a, DoubleVector b)
        {
            int count = Math.Max(a.Count, b.Count);
            var result = new double[count];
            Parallel.ForEach(Enumerable.Range(0, count), i =>
            {
                double x = (i < a.Count) ? a.ElementAt(i) : default(double);
                double y = (i < b.Count) ? b.ElementAt(i) : default(double);
                result[i] = x - y;
            });
            return new DoubleVector(result);
        }

        public static DoubleVector operator -(DoubleVector a)
        {
            return new DoubleVector(a.Select(t => -t));
        }

        public static bool IsZero(DoubleVector a)
        {
            return (!a.Any()) || a.All(IsZero);
        }

        public new static bool IsZero(double arg)
        {
            return Math.Abs(arg - default(double)) < 0.00000001;
        }

        public new static bool NotZero(double arg)
        {
            return Math.Abs(arg - default(double)) > 0.00000001;
        }
    }
}