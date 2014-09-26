using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyMath
{
    public class Vector<T> : List<T>
    {
        public Vector()
        {
        }

        public Vector(T value)
        {
            Add(value);
        }

        public Vector(IEnumerable<T> vector)
            : base(vector)
        {
        }

        public static Vector<T> operator +(Vector<T> a, Vector<T> b)
        {
            int count = Math.Max(a.Count, b.Count);
            var result = new T[count];
            Parallel.ForEach(Enumerable.Range(0, count), i =>
            {
                dynamic x = (i < a.Count) ? a.ElementAt(i) : default(T);
                dynamic y = (i < b.Count) ? b.ElementAt(i) : default(T);
                result[i] = x + y;
            });
            return new Vector<T>(result);
        }

        public static Vector<T> operator -(Vector<T> a, Vector<T> b)
        {
            int count = Math.Max(a.Count, b.Count);
            var result = new T[count];
            Parallel.ForEach(Enumerable.Range(0, count), i =>
            {
                dynamic x = (i < a.Count) ? a.ElementAt(i) : default(T);
                dynamic y = (i < b.Count) ? b.ElementAt(i) : default(T);
                result[i] = x - y;
            });
            return new Vector<T>(result);
        }

        public static Vector<T> operator -(Vector<T> a)
        {
            return new Vector<T>(a.Select(t => (T) (-(dynamic) t)));
        }

        public static bool IsZero(Vector<T> a)
        {
            return (!a.Any()) || a.All(IsZero);
        }

        public static bool IsZero(T arg)
        {
            return (dynamic)arg == default(T);
        }

        public static bool NotZero(T arg)
        {
            return (dynamic)arg != default(T);
        }

        public static T Scalar(Vector<T> a, Vector<T> b)
        {
            int count = Math.Min(a.Count, b.Count);
            var buffer = new T[count];
            Parallel.ForEach(Enumerable.Range(0, count), i =>
            {
                dynamic x = (i < a.Count) ? a.ElementAt(i) : default(T);
                dynamic y = (i < b.Count) ? b.ElementAt(i) : default(T);
                buffer[i] = x * y;
            });
            return buffer.Aggregate(default(T), (current, y) => current + (dynamic) y);
        }

        public void Add(IEnumerable<T> value)
        {
            if (!value.Any()) return;
            base.AddRange(value);
        }
    }
}