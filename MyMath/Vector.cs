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
                T x, y;
                lock (read) x = (i < a.Count) ? a.ElementAt(i) : (T) (dynamic) 0;
                lock (read) y = (i < b.Count) ? b.ElementAt(i) : (T) (dynamic) 0;
                T z = (dynamic) x + (dynamic) y;
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
                T x, y;
                lock (read) x = (i < a.Count) ? a.ElementAt(i) : (T) (dynamic) 0;
                lock (read) y = (i < b.Count) ? b.ElementAt(i) : (T) (dynamic) 0;
                T z = (dynamic) x - (dynamic) y;
                lock (write) result[i] = z;
            });
            return new Vector<T>(result);
        }

        public static Vector<T> operator -(Vector<T> a)
        {
            return new Vector<T>(a.Select(t => (T) (-(dynamic) t)));
        }

        public static bool IsZero(T a)
        {
            return unchecked((dynamic) a == (T) (dynamic) 0);
        }

        public static bool IsZero(Vector<T> a)
        {
            return a.Count == 0 || a.All(IsZero);
        }

        public static T Scalar(Vector<T> a, Vector<T> b)
        {
            int count = Math.Min(a.Count, b.Count);
            var read = new object();
            var write = new object();
            var buffer = new T[count];
            Parallel.ForEach(Enumerable.Range(0, count), i =>
            {
                T x, y;
                lock (read) x = (i < a.Count) ? a.ElementAt(i) : (T) (dynamic) 0;
                lock (read) y = (i < b.Count) ? b.ElementAt(i) : (T) (dynamic) 0;
                T z = (dynamic) x*(dynamic) y;
                lock (write) buffer[i] = z;
            });
            return buffer.Aggregate((T) (dynamic) 0, (current, y) => (dynamic) current + (dynamic) y);
        }
    }
}