using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MyLibrary.Collections;

namespace MyMath
{
    public class Vector<T> : StackListQueue<T>
    {
        public Vector()
        {
        }

        public Vector(T value)
            : base(value)
        {
        }

        public Vector(IEnumerable<T> vector)
            : base(vector)
        {
        }

        public static Vector<T> operator +(Vector<T> a, Vector<T> b)
        {
            int count = Math.Max(a.Count, b.Count);
            var read = new object();
            var write = new object();
            var result = new T[count];
            Parallel.ForEach(Enumerable.Range(0, count), i =>
            {
                dynamic x, y;
                lock (read) x = (i < a.Count) ? a.ElementAt(i) : default(T);
                lock (read) y = (i < b.Count) ? b.ElementAt(i) : default(T);
                dynamic z = x + y;
                lock (write) result[i] = z;
            });
            return new Vector<T>(result);
        }

        public static Vector<T> operator -(Vector<T> a, Vector<T> b)
        {
            int count = Math.Max(a.Count, b.Count);
            var read = new object();
            var write = new object();
            var result = new T[count];
            Parallel.ForEach(Enumerable.Range(0, count), i =>
            {
                dynamic x, y;
                lock (read) x = (i < a.Count) ? a.ElementAt(i) : default(T);
                lock (read) y = (i < b.Count) ? b.ElementAt(i) : default(T);
                dynamic z = x - y;
                lock (write) result[i] = z;
            });
            return new Vector<T>(result);
        }

        public static Vector<T> operator -(Vector<T> a)
        {
            return new Vector<T>(a.Select(t => (T) (-(dynamic) t)));
        }

        public static bool IsZero(IEnumerable<T> a)
        {
            return (!a.Any()) || a.All(IsZero);
        }

        public static bool IsZero(T arg)
        {
            double x = Math.Abs(Convert.ToDouble(arg));
            return x <= 0.0*x;
        }

        public static bool NotZero(T arg)
        {
            return Math.Abs(Convert.ToDouble(arg)) > 0.0;
        }

        public static T Scalar(Vector<T> a, Vector<T> b)
        {
            int count = Math.Min(a.Count, b.Count);
            var read = new object();
            var write = new object();
            var buffer = new double[count];
            Parallel.ForEach(Enumerable.Range(0, count), i =>
            {
                dynamic x, y;
                lock (read) x = (i < a.Count) ? a.ElementAt(i) : default(T);
                lock (read) y = (i < b.Count) ? b.ElementAt(i) : default(T);
                double z = Convert.ToDouble(x)*Convert.ToDouble(y);
                lock (write) buffer[i] = z;
            });
            return (T) (dynamic) buffer.Aggregate(0.0, (current, y) => current + y);
        }
    }
}