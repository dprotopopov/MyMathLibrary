using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using MyLibrary.Collections;
using Boolean = MyLibrary.Types.Boolean;

namespace MyMath
{
    /// <summary>
    ///     Большое целое число
    ///     Класс базируется на заданном беззнаковом типе данных
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class BigInt<T> : StackListQueue<T>
    {
        private static readonly int HalfSize = Marshal.SizeOf(typeof (T)) << 2;
        private static readonly int Size = Marshal.SizeOf(typeof (T)) << 3;

        private static readonly T HalfMask =
            (T) (((dynamic) ((T) (dynamic) 1) << (Marshal.SizeOf(typeof (T)) << 2)) - (T) (dynamic) 1);

        private static readonly T SignBitMask =
            (T) ((dynamic) ((T) (dynamic) 1) << ((Marshal.SizeOf(typeof (T)) << 3) - 1));

        private static readonly BigInt<T> Ten = new BigInt<T>((T) (dynamic) 10);

        private static readonly Dictionary<char, BigInt<T>> Digit = new Dictionary<char, BigInt<T>>
        {
            {'0', new BigInt<T>()},
            {'1', new BigInt<T>((T) (dynamic) 1)},
            {'2', new BigInt<T>((T) (dynamic) 2)},
            {'3', new BigInt<T>((T) (dynamic) 3)},
            {'4', new BigInt<T>((T) (dynamic) 4)},
            {'5', new BigInt<T>((T) (dynamic) 5)},
            {'6', new BigInt<T>((T) (dynamic) 6)},
            {'7', new BigInt<T>((T) (dynamic) 7)},
            {'8', new BigInt<T>((T) (dynamic) 8)},
            {'9', new BigInt<T>((T) (dynamic) 9)},
        };

        public BigInt(string s)
        {
            if (s[0] == '-')
                Add(-new BigInt<T>(s.Substring(1)));
            else
                Add(s.Aggregate(new BigInt<T>(), (current, t) => current*Ten + Digit[t]));
        }

        public BigInt()
        {
        }

        public BigInt(T x)
        {
            if (!IsZero(x)) Add(x);
        }

        public BigInt(IEnumerable<T> items)
        {
            var stackListQueue = new StackListQueue<T>(items);
            int count = stackListQueue.Count();
            for (;
                count > 1 && IsZero(stackListQueue.ElementAt(count - 1)) &&
                IsZero((dynamic) stackListQueue.ElementAt(count - 2) & (dynamic) SignBitMask);
                count--)
                stackListQueue.Pop();
            for (;
                count > 1 && IsMinusOne(stackListQueue.ElementAt(count - 1)) &&
                !IsZero((dynamic) stackListQueue.ElementAt(count - 2) & (dynamic) SignBitMask);
                count--)
                stackListQueue.Pop();
            if (count == 1 && IsZero(stackListQueue.First())) return;
            if (count == 0) return;
            Add(stackListQueue.GetRange(0, count));
        }

        /// <summary>
        ///     Знак числа
        ///     Значения:
        ///     0 - положительное
        ///     -1 - отрицательное
        /// </summary>
        public T Sign
        {
            get
            {
                return
                    (T) unchecked(
                        (IsZero(this) || IsZero((dynamic) this.Last() & (dynamic) SignBitMask))
                            ? 0
                            : (dynamic) (-1));
            }
        }

        public override string ToString()
        {
            if (IsZero(this)) return "0";
            if (Count == 1) return this.First().ToString();
            if (IsNegative(this)) return string.Format("-{0}", -this);
            var q = new BigInt<T>(this);
            var list = new StackListQueue<string>();
            while (!IsZero(q))
                list.Add(DivRem(ref q, Ten).ToString());
            return string.Join(string.Empty, list.GetReverse());
        }

        protected bool Equals(BigInt<T> other)
        {
            return this.SequenceEqual(other);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == GetType() && this.SequenceEqual(obj as BigInt<T>);
        }

        public override int GetHashCode()
        {
            throw new NotImplementedException();
        }

        public static BigInt<T> operator <<(BigInt<T> a, int n)
        {
            Debug.WriteLine("Begin {0}::{1}", typeof (BigInt<T>).Name, MethodBase.GetCurrentMethod().Name);
            Debug.WriteLine("Count={0},Sign={1},{2}", a.Count, a.Sign,
                string.Join(string.Empty, a.GetReverse().Select(v => string.Format("{0:x}", (dynamic) v))));
            Debug.WriteLine("n={0},", n);
            if (IsZero(a)) return new BigInt<T>();
            int count = a.Count;
            int n0 = n/Size;
            int n1 = n%Size;
            int n2 = Size - n1;
            Debug.Assert((n1 + n2) == (Marshal.SizeOf(typeof (T)) << 3));

            var read = new object();
            var write = new object();

            var result = new T[count + n0 + ((n1 == 0) ? 0 : 1)];
            if (n1 != 0)
            {
                result[n0] = (T) unchecked((dynamic) a.First() << n1);
                var mask = (T) (((dynamic) ((T) (dynamic) 1) << n1) - (T) (dynamic) 1);
                Parallel.ForEach(Enumerable.Range(1, count - 1), i =>
                {
                    T x, y;
                    lock (read) x = a.ElementAt(i - 1);
                    lock (read) y = a.ElementAt(i);
                    var z = (T) unchecked((((dynamic) x >> n2) & (dynamic) mask) + ((dynamic) y << n1));
                    lock (write) result[n0 + i] = z;
                });
                result[n0 + a.Count] =
                    (T) unchecked((((dynamic) a.Last() >> n2) & (dynamic) mask) + (dynamic) a.Sign << n1);
            }
            else
                Parallel.ForEach(Enumerable.Range(0, count), i =>
                {
                    T x;
                    lock (read) x = a.ElementAt(i);
                    lock (write) result[n0 + i] = x;
                });
            var r = new BigInt<T>(result);
            Debug.WriteLine("Count={0},Sign={1},{2}", r.Count, r.Sign,
                string.Join(string.Empty, r.GetReverse().Select(v => string.Format("{0:x}", (dynamic) v))));
            Debug.WriteLine("End {0}::{1}", typeof (BigInt<T>).Name, MethodBase.GetCurrentMethod().Name);
            return r;
        }

        public static BigInt<T> operator >>(BigInt<T> a, int n)
        {
            Debug.WriteLine("Begin {0}::{1}", typeof (BigInt<T>).Name, MethodBase.GetCurrentMethod().Name);
            Debug.WriteLine("Count={0},Sign={1},{2}", a.Count, a.Sign,
                string.Join(string.Empty, a.GetReverse().Select(v => string.Format("{0:x}", (dynamic) v))));
            Debug.WriteLine("n={0},", n);
            if (IsZero(a)) return new BigInt<T>();
            int count = a.Count;
            int n0 = n/Size;
            int n1 = n%Size;
            int n2 = Size - n1;
            Debug.Assert((n1 + n2) == Size);
            if (count <= n0) return new BigInt<T>();

            var read = new object();
            var write = new object();

            var result = new T[Math.Max(1, count - n0)];
            if (n1 != 0)
            {
                var mask = (T) (((dynamic) ((T) (dynamic) 1) << n2) - (T) (dynamic) 1);
                Parallel.ForEach(Enumerable.Range(0, count - n0), i =>
                {
                    T x, y;
                    lock (read) x = a.ElementAt(n0 + i);
                    lock (read) y = (n0 + i + 1 < count) ? a.ElementAt(n0 + i + 1) : a.Sign;
                    var z = (T) unchecked((((dynamic) x >> n1) & (dynamic) mask) + ((dynamic) y << n2));
                    lock (write) result[i] = z;
                });
            }
            else
                Parallel.ForEach(Enumerable.Range(0, a.Count - n0), i =>
                {
                    T x;
                    lock (read) x = a.ElementAt(n0 + i);
                    lock (write) result[i] = x;
                });
            var r = new BigInt<T>(result);
            Debug.WriteLine("Count={0},Sign={1},{2}", r.Count, r.Sign,
                string.Join(string.Empty, r.GetReverse().Select(v => string.Format("{0:x}", (dynamic) v))));
            Debug.WriteLine("End {0}::{1}", typeof (BigInt<T>).Name, MethodBase.GetCurrentMethod().Name);
            return r;
        }

        public static BigInt<T> Square(BigInt<T> a)
        {
            Debug.WriteLine("Begin {0}::{1}", typeof (BigInt<T>).Name, MethodBase.GetCurrentMethod().Name);
            Debug.WriteLine("Count={0},Sign={1},{2}", a.Count, a.Sign,
                string.Join(string.Empty, a.GetReverse().Select(v => string.Format("{0:x}", (dynamic) v))));
            int count = a.Count;
            if (count == 0) return new BigInt<T>();
            if (count == 1) return a*a;
            int count1 = count >> 1;
            int count2 = count - count1;
            var a1 = new BigInt<T>(a.GetRange(0, count1));
            var a2 = new BigInt<T>(a.GetRange(count1, count2));
            BigInt<T> r = Square(a1) + (a1*a2 << (count1*Size + 1)) + (Square(a2) << (count1*Size << 1));
            Debug.WriteLine("Count={0},Sign={1},{2}", r.Count, r.Sign,
                string.Join(string.Empty, r.GetReverse().Select(v => string.Format("{0:x}", (dynamic) v))));
            Debug.WriteLine("End {0}::{1}", typeof (BigInt<T>).Name, MethodBase.GetCurrentMethod().Name);
            return r;
        }

        public static BigInt<T> Pow(BigInt<T> a, int p)
        {
            Debug.WriteLine("Begin {0}::{1}", typeof (BigInt<T>).Name, MethodBase.GetCurrentMethod().Name);
            Debug.WriteLine("Count={0},Sign={1},{2}", a.Count, a.Sign,
                string.Join(string.Empty, a.GetReverse().Select(v => string.Format("{0:x}", (dynamic) v))));
            Debug.WriteLine("p={0},", p);
            if (p == 0) return new BigInt<T>((T) (dynamic) 1);
            if (IsZero(a)) return new BigInt<T>();
            int x = p;
            int k1;
            for (k1 = 0; (x & 1) == 0; k1++) x >>= 1;
            int k2;
            for (k2 = 0; x != 0; k2++) x >>= 1;
            BigInt<T> r = a;
            BigInt<T> square = a;
            for (int i = k1 + 1; i < k1 + k2; i++)
            {
                square = Square(square);
                if (((p >> i) & 1) == 1) r = (dynamic) r*(dynamic) square;
            }
            while (k1-- > 0) r = Square(r);
            Debug.WriteLine("Count={0},Sign={1},{2}", r.Count, r.Sign,
                string.Join(string.Empty, r.GetReverse().Select(v => string.Format("{0:x}", (dynamic) v))));
            Debug.WriteLine("End {0}::{1}", typeof (BigInt<T>).Name, MethodBase.GetCurrentMethod().Name);
            return r;
        }

        public static BigInt<T> operator *(BigInt<T> a, BigInt<T> b)
        {
            Debug.WriteLine("Begin {0}::{1}", typeof (BigInt<T>).Name, MethodBase.GetCurrentMethod().Name);
            Debug.WriteLine("Count={0},Sign={1},{2}", a.Count, a.Sign,
                string.Join(string.Empty, a.GetReverse().Select(v => string.Format("{0:x}", (dynamic) v))));
            Debug.WriteLine("Count={0},Sign={1},{2}", b.Count, b.Sign,
                string.Join(string.Empty, b.GetReverse().Select(v => string.Format("{0:x}", (dynamic) v))));
            if (IsZero(a) || IsZero(b)) return new BigInt<T>();
            Debug.Assert(Math.Log(a.Count) < Size);
            Debug.Assert(Math.Log(b.Count) < Size);
            int count = a.Count + b.Count;

            var read = new object();
            var write = new object();

            var list = new StackListQueue<KeyValuePair<int, T>>();

            Parallel.ForEach(
                from i in Enumerable.Range(0, count)
                from j in Enumerable.Range(0, count)
                select new[] {i, j}, pair =>
                {
                    int i = pair[0];
                    int j = pair[1];
                    int k = i + j;
                    T x, y;
                    lock (read) x = (i < a.Count) ? a.ElementAt(i) : a.Sign;
                    lock (read) y = (j < b.Count) ? b.ElementAt(j) : b.Sign;
                    try
                    {
                        var z = (T) checked((dynamic) x*(dynamic) y);
                        if (IsZero(z)) return;
                        lock (write) list.Add(new KeyValuePair<int, T>(k, z));
                    }
                    catch (OverflowException)
                    {
                        foreach (var pair0 in from i0 in Enumerable.Range(0, 2)
                            from j0 in Enumerable.Range(0, 2)
                            select new[] {i0, j0})
                        {
                            int i0 = pair0[0];
                            int j0 = pair0[1];
                            int k0 = (k << 1) + (i0 + j0);
                            T x0 = (T) ((dynamic) x >> (i0*HalfSize)) & (dynamic) HalfMask;
                            T y0 = (T) ((dynamic) y >> (j0*HalfSize)) & (dynamic) HalfMask;
                            var z0 = (T) unchecked((dynamic) x0*(dynamic) y0);
                            if ((k0 & 1) == 0)
                            {
                                if (IsZero(z0)) continue;
                                lock (write) list.Add(new KeyValuePair<int, T>(k0 >> 1, z0));
                            }
                            else
                            {
                                var z00 = (T) unchecked(((dynamic) z0 & (dynamic) HalfMask) << HalfSize);
                                var z01 = (T) unchecked(((dynamic) z0 >> HalfSize) & (dynamic) HalfMask);
                                if (!IsZero(z00))
                                    lock (write) list.Add(new KeyValuePair<int, T>(k0 >> 1, z00));
                                if (!IsZero(z01))
                                    lock (write) list.Add(new KeyValuePair<int, T>((k0 >> 1) + 1, z01));
                            }
                        }
                    }
                });

            var result = new T[count];
            Parallel.ForEach(Enumerable.Range(0, count), i => { lock (write) result[i] = (T) (dynamic) 0; });

            while (list.Any())
            {
                Dictionary<int, IEnumerable<T>> dictionary = new StackListQueue<int>(
                    list.Select(p => p.Key).Distinct().Where(key => key < count))
                    .ToDictionary(
                        key => key,
                        key => list.Where(p => p.Key == key).Select(p => p.Value));
                var next = new StackListQueue<KeyValuePair<int, T>>();
                Parallel.ForEach(dictionary, pair =>
                {
                    int i = pair.Key;
                    IEnumerable<T> items = pair.Value;
                    if (i >= count + 1) return;
                    var x = (T) (dynamic) 0;
                    var inc = (T) (dynamic) 0;
                    foreach (T y in items)
                        try
                        {
                            x = (T) checked((dynamic) x + (dynamic) y);
                        }
                        catch (OverflowException)
                        {
                            x = (T) unchecked((dynamic) x + (dynamic) y);
                            inc = (T) unchecked((dynamic) inc + (T) (dynamic) 1);
                        }
                    if (!IsZero(x))
                        lock (write)
                        {
                            T z = default(T);
                            T y = result[i];
                            try
                            {
                                z = (T) checked((dynamic) x + (dynamic) y);
                            }
                            catch (OverflowException)
                            {
                                z = (T) unchecked((dynamic) x + (dynamic) y);
                                inc = (T) unchecked((dynamic) inc + (T) (dynamic) 1);
                            }
                            finally
                            {
                                result[i] = z;
                            }
                        }
                    if (IsZero(inc)) return;
                    lock (write) next.Add(new KeyValuePair<int, T>(i + 1, inc));
                });
                Debug.WriteLine(string.Join(" ", result.Select(v => string.Format("{0:x}", (dynamic) v))));
                list = next;
            }
            var r = new BigInt<T>(result);
            Debug.WriteLine("Count={0},Sign={1},{2}", r.Count, r.Sign,
                string.Join(string.Empty, r.GetReverse().Select(v => string.Format("{0:x}", (dynamic) v))));
            Debug.WriteLine("End {0}::{1}", typeof (BigInt<T>).Name, MethodBase.GetCurrentMethod().Name);
            return r;
        }

        public static BigInt<T> operator +(BigInt<T> a, BigInt<T> b)
        {
            Debug.WriteLine("Begin {0}::{1}", typeof (BigInt<T>).Name, MethodBase.GetCurrentMethod().Name);
            Debug.WriteLine("Count={0},Sign={1},{2}", a.Count, a.Sign,
                string.Join(string.Empty, a.GetReverse().Select(v => string.Format("{0:x}", (dynamic) v))));
            Debug.WriteLine("Count={0},Sign={1},{2}", b.Count, b.Sign,
                string.Join(string.Empty, b.GetReverse().Select(v => string.Format("{0:x}", (dynamic) v))));
            if (IsZero(a) && IsZero(b)) return new BigInt<T>();
            if (IsZero(a)) return new BigInt<T>(b);
            if (IsZero(b)) return new BigInt<T>(a);

            var read = new object();
            var write = new object();

            int count = Math.Max(a.Count, b.Count) + 1;
            var result = new T[count];

            var list = new StackListQueue<KeyValuePair<int, T>>();
            Parallel.ForEach(Enumerable.Range(0, count), i =>
            {
                T x, y, z = default(T);
                lock (read) x = (i < a.Count) ? a.ElementAt(i) : a.Sign;
                lock (read) y = (i < b.Count) ? b.ElementAt(i) : b.Sign;
                try
                {
                    z = (T) checked((dynamic) x + (dynamic) y);
                }
                catch (OverflowException)
                {
                    z = (T) unchecked((dynamic) x + (dynamic) y);
                    lock (write) list.Add(new KeyValuePair<int, T>(i + 1, (T) (dynamic) 1));
                }
                finally
                {
                    lock (write) result[i] = z;
                }
            });

            while (list.Any())
            {
                var next = new StackListQueue<KeyValuePair<int, T>>();
                Parallel.ForEach(list, pair =>
                {
                    int i = pair.Key;
                    T inc = pair.Value;
                    if (i >= count) return;
                    lock (write)
                    {
                        T z = default(T);
                        T x = result[i];
                        try
                        {
                            z = (T) checked((dynamic) x + (dynamic) inc);
                        }
                        catch (OverflowException)
                        {
                            z = (T) unchecked((dynamic) x + (dynamic) inc);
                            next.Add(new KeyValuePair<int, T>(i + 1, (T) (dynamic) 1));
                        }
                        finally
                        {
                            result[i] = z;
                        }
                    }
                });
                list = next;
            }
            var r = new BigInt<T>(result);
            Debug.WriteLine("Count={0},Sign={1},{2}", r.Count, r.Sign,
                string.Join(string.Empty, r.GetReverse().Select(v => string.Format("{0:x}", (dynamic) v))));
            Debug.WriteLine("End {0}::{1}", typeof (BigInt<T>).Name, MethodBase.GetCurrentMethod().Name);
            return r;
        }

        public static BigInt<T> operator -(BigInt<T> a)
        {
            Debug.WriteLine("Begin {0}::{1}", typeof (BigInt<T>).Name, MethodBase.GetCurrentMethod().Name);
            Debug.WriteLine("Count={0},Sign={1},{2}", a.Count, a.Sign,
                string.Join(string.Empty, a.GetReverse().Select(v => string.Format("{0:x}", (dynamic) v))));
            BigInt<T> r = ~a;
            r++;
            Debug.WriteLine("Count={0},Sign={1},{2}", r.Count, r.Sign,
                string.Join(string.Empty, r.GetReverse().Select(v => string.Format("{0:x}", (dynamic) v))));
            Debug.WriteLine("End {0}::{1}", typeof (BigInt<T>).Name, MethodBase.GetCurrentMethod().Name);
            return r;
        }

        public static BigInt<T> operator ~(BigInt<T> a)
        {
            Debug.WriteLine("Begin {0}::{1}", typeof (BigInt<T>).Name, MethodBase.GetCurrentMethod().Name);
            Debug.WriteLine("Count={0},Sign={1},{2}", a.Count, a.Sign,
                string.Join(string.Empty, a.GetReverse().Select(v => string.Format("{0:x}", (dynamic) v))));
            var r = new BigInt<T>(new StackListQueue<T>(a.Select(v => (T) unchecked(~(dynamic) v)))
            {
                (T) unchecked (~(dynamic) a.Sign)
            });
            Debug.WriteLine("Count={0},Sign={1},{2}", r.Count, r.Sign,
                string.Join(string.Empty, r.GetReverse().Select(v => string.Format("{0:x}", (dynamic) v))));
            Debug.WriteLine("End {0}::{1}", typeof (BigInt<T>).Name, MethodBase.GetCurrentMethod().Name);
            return r;
        }

        public static BigInt<T> operator -(BigInt<T> a, BigInt<T> b)
        {
            Debug.WriteLine("Begin {0}::{1}", typeof (BigInt<T>).Name, MethodBase.GetCurrentMethod().Name);
            Debug.WriteLine("Count={0},Sign={1},{2}", a.Count, a.Sign,
                string.Join(string.Empty, a.GetReverse().Select(v => string.Format("{0:x}", (dynamic) v))));
            Debug.WriteLine("Count={0},Sign={1},{2}", b.Count, b.Sign,
                string.Join(string.Empty, b.GetReverse().Select(v => string.Format("{0:x}", (dynamic) v))));
            if (IsZero(a) && IsZero(b)) return new BigInt<T>();
            if (IsZero(a)) return -b;
            if (IsZero(b)) return new BigInt<T>(a);

            BigInt<T> r = a + (-b);

            Debug.WriteLine("Count={0},Sign={1},{2}", r.Count, r.Sign,
                string.Join(string.Empty, r.GetReverse().Select(v => string.Format("{0:x}", (dynamic) v))));
            Debug.WriteLine("End {0}::{1}", typeof (BigInt<T>).Name, MethodBase.GetCurrentMethod().Name);
            return r;
        }

        public static BigInt<T> operator ^(BigInt<T> a, BigInt<T> b)
        {
            Debug.WriteLine("Begin {0}::{1}", typeof (BigInt<T>).Name, MethodBase.GetCurrentMethod().Name);
            Debug.WriteLine("Count={0},Sign={1},{2}", a.Count, a.Sign,
                string.Join(string.Empty, a.GetReverse().Select(v => string.Format("{0:x}", (dynamic) v))));
            Debug.WriteLine("Count={0},Sign={1},{2}", b.Count, b.Sign,
                string.Join(string.Empty, b.GetReverse().Select(v => string.Format("{0:x}", (dynamic) v))));
            if (IsZero(a) && IsZero(b)) return new BigInt<T>();

            var read = new object();
            var write = new object();

            int count = Math.Max(a.Count, b.Count);
            var result = new T[count];
            Parallel.ForEach(Enumerable.Range(0, count), i =>
            {
                T x, y;
                lock (read) x = (i < a.Count) ? a.ElementAt(i) : a.Sign;
                lock (read) y = (i < b.Count) ? b.ElementAt(i) : b.Sign;
                var z = (T) unchecked((dynamic) x ^ (dynamic) y);
                lock (write) result[i] = z;
            });
            Debug.WriteLine("End {0}::{1}", typeof (BigInt<T>).Name, MethodBase.GetCurrentMethod().Name);
            return new BigInt<T>(result);
        }

        public static BigInt<T> operator |(BigInt<T> a, BigInt<T> b)
        {
            Debug.WriteLine("Begin {0}::{1}", typeof (BigInt<T>).Name, MethodBase.GetCurrentMethod().Name);
            Debug.WriteLine("Count={0},Sign={1},{2}", a.Count, a.Sign,
                string.Join(string.Empty, a.GetReverse().Select(v => string.Format("{0:x}", (dynamic) v))));
            Debug.WriteLine("Count={0},Sign={1},{2}", b.Count, b.Sign,
                string.Join(string.Empty, b.GetReverse().Select(v => string.Format("{0:x}", (dynamic) v))));
            if (IsZero(a) && IsZero(b)) return new BigInt<T>();

            var read = new object();
            var write = new object();

            int count = Math.Max(a.Count, b.Count);
            var result = new T[count];
            Parallel.ForEach(Enumerable.Range(0, count), i =>
            {
                T x, y;
                lock (read) x = (i < a.Count) ? a.ElementAt(i) : a.Sign;
                lock (read) y = (i < b.Count) ? b.ElementAt(i) : b.Sign;
                var z = (T) unchecked((dynamic) x | (dynamic) y);
                lock (write) result[i] = z;
            });
            var r = new BigInt<T>(result);
            Debug.WriteLine("Count={0},Sign={1},{2}", r.Count, r.Sign,
                string.Join(string.Empty, r.GetReverse().Select(v => string.Format("{0:x}", (dynamic) v))));
            Debug.WriteLine("End {0}::{1}", typeof (BigInt<T>).Name, MethodBase.GetCurrentMethod().Name);
            return r;
        }

        #region

        public static bool IsZero(BigInt<T> a)
        {
            return a.Count == 0;
        }

        public static bool IsPositive(BigInt<T> a)
        {
            return IsZero(a.Sign);
        }

        public static bool IsNegative(BigInt<T> a)
        {
            return IsMinusOne(a.Sign);
        }

        private static bool IsMinusOne(BigInt<T> a)
        {
            return a.Count == 1 && IsMinusOne(a.First());
        }

        private static bool IsOne(BigInt<T> a)
        {
            return a.Count == 1 && IsOne(a.First());
        }

        #endregion

        #region

        public static bool IsZero(T a)
        {
            return unchecked((dynamic) a == (T) (dynamic) 0);
        }

        private static bool IsOne(T a)
        {
            return unchecked((dynamic) a == (T) (dynamic) 1);
        }

        private static bool IsMinusOne(T a)
        {
            return unchecked((dynamic) a == (T) (dynamic) (-1));
        }

        #endregion

        #region

        private static BigInt<T> DivRem(ref BigInt<T> a, BigInt<T> b)
        {
            Debug.WriteLine("Begin {0}::{1}", typeof (BigInt<T>).Name, MethodBase.GetCurrentMethod().Name);
            Debug.WriteLine("Count={0},Sign={1},{2}", a.Count, a.Sign,
                string.Join(string.Empty, a.GetReverse().Select(v => string.Format("{0:x}", (dynamic) v))));
            Debug.WriteLine("Count={0},Sign={1},{2}", b.Count, b.Sign,
                string.Join(string.Empty, b.GetReverse().Select(v => string.Format("{0:x}", (dynamic) v))));
            if (IsZero(b)) throw new DivideByZeroException();
            if (IsOne(b)) return new BigInt<T>(a);
            if (IsMinusOne(b)) return -a;
            if (Boolean.Or(IsNegative(a), IsNegative(b)))
            {
                BigInt<T> c = (IsPositive(a)) ? a : -a;
                BigInt<T> d = (IsPositive(b)) ? b : -b;
                BigInt<T> cd = c/d;
                return Boolean.Xor(IsPositive(a), IsPositive(b)) ? cd : -cd;
            }
            int count1 = a.Count;
            int count2 = b.Count;
            if (count1 < count2) return new BigInt<T>();
            int diff = (count1 - count2 + 1)*Size;
            var q = new BigInt<T>();
            var r = new BigInt<T>(a);
            BigInt<T> divider = b << diff;
            while (diff-- > 0)
            {
                q <<= 1;
                r <<= 1;
                if (r < divider) continue;
                r -= divider;
                q++;
            }
            r = (count1 - count2 + 1 < r.Count)
                ? new BigInt<T>(r.GetRange(count1 - count2 + 1, r.Count - (count1 - count2 + 1)))
                : new BigInt<T>();
            a.ReplaceAll(q);
            Debug.WriteLine("Count={0},Sign={1},{2}", r.Count, r.Sign,
                string.Join(string.Empty, r.GetReverse().Select(v => string.Format("{0:x}", (dynamic) v))));
            Debug.WriteLine("End {0}::{1}", typeof (BigInt<T>).Name, MethodBase.GetCurrentMethod().Name);
            return r;
        }

        public static BigInt<T> operator /(BigInt<T> a, BigInt<T> b)
        {
            Debug.WriteLine("Begin {0}::{1}", typeof (BigInt<T>).Name, MethodBase.GetCurrentMethod().Name);
            Debug.WriteLine("Count={0},Sign={1},{2}", a.Count, a.Sign,
                string.Join(string.Empty, a.GetReverse().Select(v => string.Format("{0:x}", (dynamic) v))));
            Debug.WriteLine("Count={0},Sign={1},{2}", b.Count, b.Sign,
                string.Join(string.Empty, b.GetReverse().Select(v => string.Format("{0:x}", (dynamic) v))));
            if (IsZero(b)) throw new DivideByZeroException();
            if (IsOne(b)) return new BigInt<T>(a);
            if (IsMinusOne(b)) return -a;
            if (Boolean.Or(IsNegative(a), IsNegative(b)))
            {
                BigInt<T> c = (IsPositive(a)) ? a : -a;
                BigInt<T> d = (IsPositive(b)) ? b : -b;
                BigInt<T> cd = c/d;
                return Boolean.Xor(IsPositive(a), IsPositive(b)) ? cd : -cd;
            }

            int count1 = a.Count;
            int count2 = b.Count;
            if (count1 < count2) return new BigInt<T>();
            int diff = (count2 - count2 + 1)*Size;
            var q = new BigInt<T>();
            var r = new BigInt<T>(a);
            BigInt<T> divider = b << diff;
            while (diff-- > 0)
            {
                q <<= 1;
                r <<= 1;
                if (r < divider) continue;
                r -= divider;
                q++;
            }
            Debug.WriteLine("Count={0},Sign={1},{2}", q.Count, q.Sign,
                string.Join(string.Empty, q.GetReverse().Select(v => string.Format("{0:x}", (dynamic) v))));
            Debug.WriteLine("End {0}::{1}", typeof (BigInt<T>).Name, MethodBase.GetCurrentMethod().Name);
            return q;
        }

        public static BigInt<T> operator %(BigInt<T> a, BigInt<T> b)
        {
            Debug.WriteLine("Begin {0}::{1}", typeof (BigInt<T>).Name, MethodBase.GetCurrentMethod().Name);
            Debug.WriteLine("Count={0},Sign={1},{2}", a.Count, a.Sign,
                string.Join(string.Empty, a.GetReverse().Select(v => string.Format("{0:x}", (dynamic) v))));
            Debug.WriteLine("Count={0},Sign={1},{2}", b.Count, b.Sign,
                string.Join(string.Empty, b.GetReverse().Select(v => string.Format("{0:x}", (dynamic) v))));
            if (IsZero(b)) throw new DivideByZeroException();
            if (IsOne(b)) return new BigInt<T>(a);
            if (IsMinusOne(b)) return -a;
            if (Boolean.Or(IsNegative(a), IsNegative(b)))
            {
                BigInt<T> c = (IsPositive(a)) ? a : -a;
                BigInt<T> d = (IsPositive(b)) ? b : -b;
                BigInt<T> cd = c/d;
                return Boolean.Xor(IsPositive(a), IsPositive(b)) ? cd : -cd;
            }

            int count1 = a.Count;
            int count2 = b.Count;
            if (count1 < count2) return new BigInt<T>();
            int diff = (count2 - count2 + 1)*Size;
            var q = new BigInt<T>();
            var r = new BigInt<T>(a);
            BigInt<T> divider = b << diff;
            while (diff-- > 0)
            {
                q <<= 1;
                r <<= 1;
                if (r < divider) continue;
                r -= divider;
                q++;
            }
            r = (count1 - count2 + 1 < r.Count)
                ? new BigInt<T>(r.GetRange(count1 - count2 + 1, r.Count - (count1 - count2 + 1)))
                : new BigInt<T>();
            Debug.WriteLine("Count={0},Sign={1},{2}", r.Count, r.Sign,
                string.Join(string.Empty, r.GetReverse().Select(v => string.Format("{0:x}", (dynamic) v))));
            Debug.WriteLine("End {0}::{1}", typeof (BigInt<T>).Name, MethodBase.GetCurrentMethod().Name);
            return r;
        }

        #endregion

        #region Операторы сравнения

        public static bool operator <(BigInt<T> a, BigInt<T> b)
        {
            if (IsNegative(a) && IsNegative(b)) return (-a) > (-b);
            if ((dynamic) a.Sign < (dynamic) b.Sign) return true;
            if ((dynamic) a.Sign > (dynamic) b.Sign) return false;
            if (IsZero(a) && IsZero(b)) return false;
            if (IsZero(a)) return true;
            if (IsZero(b)) return false;
            int value = a.Count - b.Count;
            if (value < 0) return true;
            if (value > 0) return false;
            int count = Math.Max(a.Count, b.Count);
            while (count-- > 0)
            {
                T x = a.ElementAt(count);
                T y = b.ElementAt(count);
                if ((dynamic) x < (dynamic) y) return true;
                if ((dynamic) x > (dynamic) y) return false;
            }
            return false;
        }

        public static bool operator >(BigInt<T> a, BigInt<T> b)
        {
            if (IsNegative(a) && IsNegative(b)) return (-a) < (-b);
            if ((dynamic) a.Sign > (dynamic) b.Sign) return true;
            if ((dynamic) a.Sign < (dynamic) b.Sign) return false;
            if (IsZero(a) && IsZero(b)) return false;
            if (IsZero(b)) return true;
            if (IsZero(a)) return false;
            int value = a.Count - b.Count;
            if (value > 0) return true;
            if (value < 0) return false;
            int count = Math.Max(a.Count, b.Count);
            while (count-- > 0)
            {
                T x = a.ElementAt(count);
                T y = b.ElementAt(count);
                if ((dynamic) x > (dynamic) y) return true;
                if ((dynamic) x < (dynamic) y) return false;
            }
            return false;
        }

        public static bool operator >=(BigInt<T> a, BigInt<T> b)
        {
            return (a == b) || (a > b);
        }

        public static bool operator <=(BigInt<T> a, BigInt<T> b)
        {
            return (a == b) || (a < b);
        }

        public static bool operator ==(BigInt<T> a, BigInt<T> b)
        {
            return a.SequenceEqual(b);
        }

        public static bool operator !=(BigInt<T> a, BigInt<T> b)
        {
            return !(a.SequenceEqual(b));
        }

        #endregion

        #region ++/--

        public static BigInt<T> operator ++(BigInt<T> a)
        {
            Debug.WriteLine("Begin {0}::{1}", typeof (BigInt<T>).Name, MethodBase.GetCurrentMethod().Name);
            Debug.WriteLine("Count={0},Sign={1},{2}", a.Count, a.Sign,
                string.Join(string.Empty, a.GetReverse().Select(v => string.Format("{0:x}", (dynamic) v))));
            a.Add(a.Sign);
            int count = a.Count;
            for (int i = 0; i < count; i++)
            {
                var x = (T) unchecked((dynamic) a[i] + (T) (dynamic) 1);
                a[i] = x;
                if (!IsZero(x)) break;
            }
            for (;
                count > 1 && IsZero(a.ElementAt(count - 1)) &&
                IsZero((dynamic) a.ElementAt(count - 2) & (dynamic) SignBitMask);
                count--)
                a.Pop();
            for (;
                count > 1 && IsMinusOne(a.ElementAt(count - 1)) &&
                !IsZero((dynamic) a.ElementAt(count - 2) & (dynamic) SignBitMask);
                count--)
                a.Pop();
            if (count == 1 && IsZero(a.ElementAt(count - 1))) a.Pop();
            Debug.WriteLine("Count={0},Sign={1},{2}", a.Count, a.Sign,
                string.Join(string.Empty, a.GetReverse().Select(v => string.Format("{0:x}", (dynamic) v))));
            Debug.WriteLine("End {0}::{1}", typeof (BigInt<T>).Name, MethodBase.GetCurrentMethod().Name);
            return a;
        }

        public static BigInt<T> operator --(BigInt<T> a)
        {
            Debug.WriteLine("Begin {0}::{1}", typeof (BigInt<T>).Name, MethodBase.GetCurrentMethod().Name);
            Debug.WriteLine("Count={0},Sign={1},{2}", a.Count, a.Sign,
                string.Join(string.Empty, a.GetReverse().Select(v => string.Format("{0:x}", (dynamic) v))));
            a.Add(a.Sign);
            int count = a.Count;
            for (int i = 0; i < count; i++)
            {
                var x = (T) unchecked((dynamic) a[i] - (T) (dynamic) 1);
                a[i] = x;
                if (!IsMinusOne(x)) break;
            }
            for (;
                count > 1 && IsZero(a.ElementAt(count - 1)) &&
                IsZero((dynamic) a.ElementAt(count - 2) & (dynamic) SignBitMask);
                count--)
                a.Pop();
            for (;
                count > 1 && IsMinusOne(a.ElementAt(count - 1)) &&
                !IsZero((dynamic) a.ElementAt(count - 2) & (dynamic) SignBitMask);
                count--)
                a.Pop();
            if (count == 1 && IsZero(a.ElementAt(count - 1))) a.Pop();
            Debug.WriteLine("Count={0},Sign={1},{2}", a.Count, a.Sign,
                string.Join(string.Empty, a.GetReverse().Select(v => string.Format("{0:x}", (dynamic) v))));
            Debug.WriteLine("End {0}::{1}", typeof (BigInt<T>).Name, MethodBase.GetCurrentMethod().Name);
            return a;
        }

        #endregion
    }
}