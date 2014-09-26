using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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

        private static readonly dynamic Uno = (T) (dynamic) 1;

        private static readonly dynamic HalfMask = (T) ((Uno << (Marshal.SizeOf(typeof (T)) << 2)) - Uno);
        private static readonly dynamic SignMask = (T) (Uno << ((Marshal.SizeOf(typeof (T)) << 3) - 1));

        private static readonly BigInt<T> Zero = new BigInt<T>();
        private static readonly BigInt<T> One = new BigInt<T>((T) (dynamic) 1);
        private static readonly BigInt<T> Two = new BigInt<T>((T) (dynamic) 2);
        private static readonly BigInt<T> Three = new BigInt<T>((T) (dynamic) 3);
        private static readonly BigInt<T> Four = new BigInt<T>((T) (dynamic) 4);
        private static readonly BigInt<T> Five = new BigInt<T>((T) (dynamic) 5);
        private static readonly BigInt<T> Six = new BigInt<T>((T) (dynamic) 6);
        private static readonly BigInt<T> Seven = new BigInt<T>((T) (dynamic) 7);
        private static readonly BigInt<T> Eight = new BigInt<T>((T) (dynamic) 8);
        private static readonly BigInt<T> Nine = new BigInt<T>((T) (dynamic) 9);
        private static readonly BigInt<T> Ten = new BigInt<T>((T) (dynamic) 10);

        private static readonly Dictionary<char, BigInt<T>> Digit = new Dictionary<char, BigInt<T>>
        {
            {'0', Zero},
            {'1', One},
            {'2', Two},
            {'3', Three},
            {'4', Four},
            {'5', Five},
            {'6', Six},
            {'7', Seven},
            {'8', Eight},
            {'9', Nine},
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
                IsZero(stackListQueue.ElementAt(count - 2) & SignMask);
                count--)
                stackListQueue.Pop();
            for (;
                count > 1 && IsOne(~(dynamic)stackListQueue.ElementAt(count - 1)) &&
                !IsZero(stackListQueue.ElementAt(count - 2) & SignMask);
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
        public dynamic Sign
        {
            get
            {
                return unchecked((IsZero(this) || IsZero(this.Last() & SignMask))
                    ? default(T)
                    : ~(dynamic)default(T));
            }
        }

        public override string ToString()
        {
            if (IsZero(this)) return "0";
            if (Count == 1) return this.First().ToString();
            if (IsNegative(this)) return string.Format("-{0}", -this);
            var q = new BigInt<T>(this);
            var list = new StackListQueue<string>();
            while (!IsZero(q)) list.Add(DivRem(ref q, Ten).ToString());
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
                result[n0] = unchecked((dynamic) a.First() << n1);
                dynamic mask = (T) ((Uno << n1) - Uno);
                Parallel.ForEach(Enumerable.Range(1, count - 1), i =>
                {
                    dynamic x, y;
                    lock (read) x = a.ElementAt(i - 1);
                    lock (read) y = a.ElementAt(i);
                    lock (write) result[n0 + i] = unchecked(((x >> n2) & mask) + (y << n1));
                });
                result[n0 + a.Count] =
                    unchecked((((dynamic) a.Last() >> n2) & mask) + a.Sign << n1);
            }
            else
                Parallel.ForEach(Enumerable.Range(0, count), i =>
                {
                    T x;
                    lock (read) x = a.ElementAt(i);
                    lock (write) result[n0 + i] = x;
                });
            var r = new BigInt<T>(result);
            return r;
        }

        public static BigInt<T> operator >>(BigInt<T> a, int n)
        {
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
                dynamic mask = (T) (((dynamic) ((T) (dynamic) 1) << n2) - (T) (dynamic) 1);
                Parallel.ForEach(Enumerable.Range(0, count - n0), i =>
                {
                    dynamic x, y;
                    lock (read) x = a.ElementAt(n0 + i);
                    lock (read) y = (n0 + i + 1 < count) ? a.ElementAt(n0 + i + 1) : a.Sign;
                    lock (write) result[i] = unchecked(((x >> n1) & mask) + (y << n2));
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
            return r;
        }

        public static BigInt<T> Square(BigInt<T> a)
        {
            BigInt<T> b = (IsNegative(a)) ? -a : a;
            int count = b.Count;
            if (count == 0) return new BigInt<T>();
            if (count == 1) return b*b;
            int count1 = count >> 1;
            int count2 = count - count1;
            var a1 = new BigInt<T>(b.GetRange(0, count1));
            var a2 = new BigInt<T>(b.GetRange(count1, count2));
            BigInt<T> r = Square(a1) + (a1*a2 << (count1*Size + 1)) + (Square(a2) << (count1*Size << 1));
            return r;
        }

        public static BigInt<T> Pow(BigInt<T> a, int p)
        {
            if (p == 0) return new BigInt<T>(Uno);
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
                if (((p >> i) & 1) == 1) r = r*square;
            }
            while (k1-- > 0) r = Square(r);
            return r;
        }

        public static BigInt<T> operator *(BigInt<T> a, BigInt<T> b)
        {
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
                    dynamic x, y;
                    lock (read) x = (i < a.Count) ? a.ElementAt(i) : a.Sign;
                    lock (read) y = (j < b.Count) ? b.ElementAt(j) : b.Sign;
                    try
                    {
                        dynamic z = checked(x*y);
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
                            dynamic x0 = (T) (x >> (i0*HalfSize)) & HalfMask;
                            dynamic y0 = (T) (y >> (j0*HalfSize)) & HalfMask;
                            dynamic z0 = unchecked(x0*y0);
                            if ((k0 & 1) == 0)
                            {
                                if (IsZero(z0)) continue;
                                lock (write) list.Add(new KeyValuePair<int, T>(k0 >> 1, z0));
                            }
                            else
                            {
                                dynamic z00 = unchecked((z0 & HalfMask) << HalfSize);
                                dynamic z01 = unchecked((z0 >> HalfSize) & HalfMask);
                                if (!IsZero(z00))
                                    lock (write) list.Add(new KeyValuePair<int, T>(k0 >> 1, z00));
                                if (!IsZero(z01))
                                    lock (write) list.Add(new KeyValuePair<int, T>((k0 >> 1) + 1, z01));
                            }
                        }
                    }
                });

            var result = new T[count];
            Parallel.ForEach(Enumerable.Range(0, count), i => { lock (write) result[i] = default(T); });

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
                    T x = default(T);
                    dynamic inc = default(T);
                    foreach (dynamic y in items)
                        try
                        {
                            x = checked(x + y);
                        }
                        catch (OverflowException)
                        {
                            x = unchecked(x + y);
                            inc = unchecked(inc + Uno);
                        }
                    if (!IsZero(x))
                        lock (write)
                        {
                            T z = default(T);
                            dynamic y = result[i];
                            try
                            {
                                z = checked(x + y);
                            }
                            catch (OverflowException)
                            {
                                z = unchecked(x + y);
                                inc = unchecked(inc + Uno);
                            }
                            finally
                            {
                                result[i] = z;
                            }
                        }
                    if (IsZero(inc)) return;
                    lock (write) next.Add(new KeyValuePair<int, T>(i + 1, inc));
                });
                list = next;
            }
            var r = new BigInt<T>(result);
            return r;
        }

        public static BigInt<T> operator +(BigInt<T> a, BigInt<T> b)
        {
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
                T z = default(T);
                dynamic x, y;
                lock (read) x = (i < a.Count) ? a.ElementAt(i) : a.Sign;
                lock (read) y = (i < b.Count) ? b.ElementAt(i) : b.Sign;
                try
                {
                    z = checked(x + y);
                }
                catch (OverflowException)
                {
                    z = unchecked(x + y);
                    lock (write) list.Add(new KeyValuePair<int, T>(i + 1, Uno));
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
                    dynamic inc = pair.Value;
                    if (i >= count) return;
                    lock (write)
                    {
                        T z = default(T);
                        dynamic x = result[i];
                        try
                        {
                            z = checked(x + inc);
                        }
                        catch (OverflowException)
                        {
                            z = unchecked(x + inc);
                            next.Add(new KeyValuePair<int, T>(i + 1, Uno));
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
            return r;
        }

        public static BigInt<T> operator -(BigInt<T> a)
        {
            BigInt<T> r = ~a;
            r++;
            return r;
        }

        public static BigInt<T> operator ~(BigInt<T> a)
        {
            var r = new BigInt<T>(new StackListQueue<T>(a.Select(v => unchecked((T) ~(dynamic) v)))
            {
                unchecked (~a.Sign)
            });
            return r;
        }

        public static BigInt<T> operator -(BigInt<T> a, BigInt<T> b)
        {
            if (IsZero(a) && IsZero(b)) return new BigInt<T>();
            if (IsZero(a)) return -b;
            if (IsZero(b)) return new BigInt<T>(a);

            BigInt<T> r = a + (-b);

            return r;
        }

        public static BigInt<T> operator ^(BigInt<T> a, BigInt<T> b)
        {
            if (IsZero(a) && IsZero(b)) return new BigInt<T>();

            var read = new object();
            var write = new object();

            int count = Math.Max(a.Count, b.Count);
            var result = new T[count];
            Parallel.ForEach(Enumerable.Range(0, count), i =>
            {
                dynamic x, y;
                lock (read) x = (i < a.Count) ? a.ElementAt(i) : a.Sign;
                lock (read) y = (i < b.Count) ? b.ElementAt(i) : b.Sign;
                lock (write) result[i] = unchecked(x ^ y);
            });
            return new BigInt<T>(result);
        }

        public static BigInt<T> operator |(BigInt<T> a, BigInt<T> b)
        {
            if (IsZero(a) && IsZero(b)) return new BigInt<T>();

            var read = new object();
            var write = new object();

            int count = Math.Max(a.Count, b.Count);
            var result = new T[count];
            Parallel.ForEach(Enumerable.Range(0, count), i =>
            {
                dynamic x, y;
                lock (read) x = (i < a.Count) ? a.ElementAt(i) : a.Sign;
                lock (read) y = (i < b.Count) ? b.ElementAt(i) : b.Sign;
                lock (write) result[i] = unchecked(x | y);
            });
            var r = new BigInt<T>(result);
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
            return IsZero(~(a.Sign));
        }

        private static bool IsOne(BigInt<T> a)
        {
            return a.Count == 1 && IsOne(a.First());
        }

        #endregion

        #region

        public static bool IsZero(T a)
        {
            return unchecked((dynamic)a == default(T));
        }

        private static bool IsOne(T a)
        {
            return unchecked((dynamic)a == (T)(dynamic)1);
        }

        #endregion

        #region

        private static BigInt<T> DivRem(ref BigInt<T> a, BigInt<T> b)
        {
            if (IsZero(b)) throw new DivideByZeroException();
            if (IsOne(b)) return new BigInt<T>(a);
            if (IsOne(~b)) return -a;
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
            return r;
        }

        public static BigInt<T> operator /(BigInt<T> a, BigInt<T> b)
        {
            if (IsZero(b)) throw new DivideByZeroException();
            if (IsOne(b)) return new BigInt<T>(a);
            if (IsOne(~b)) return -a;
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
            return q;
        }

        public static BigInt<T> operator %(BigInt<T> a, BigInt<T> b)
        {
            if (IsZero(b)) throw new DivideByZeroException();
            if (IsOne(b)) return new BigInt<T>(a);
            if (IsOne(~b)) return -a;
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
            return r;
        }

        #endregion

        #region Операторы сравнения

        public static bool operator <(BigInt<T> a, BigInt<T> b)
        {
            if (IsNegative(a) && IsNegative(b)) return (-a) > (-b);
            if (a.Sign < b.Sign) return true;
            if (a.Sign > b.Sign) return false;
            if (IsZero(a) && IsZero(b)) return false;
            if (IsZero(a)) return true;
            if (IsZero(b)) return false;
            int value = a.Count - b.Count;
            if (value < 0) return true;
            if (value > 0) return false;
            int count = Math.Max(a.Count, b.Count);
            while (count-- > 0)
            {
                dynamic x = a.ElementAt(count);
                dynamic y = b.ElementAt(count);
                if (x < y) return true;
                if (x > y) return false;
            }
            return false;
        }

        public static bool operator >(BigInt<T> a, BigInt<T> b)
        {
            if (IsNegative(a) && IsNegative(b)) return (-a) < (-b);
            if (a.Sign > b.Sign) return true;
            if (a.Sign < b.Sign) return false;
            if (IsZero(a) && IsZero(b)) return false;
            if (IsZero(b)) return true;
            if (IsZero(a)) return false;
            int value = a.Count - b.Count;
            if (value > 0) return true;
            if (value < 0) return false;
            int count = Math.Max(a.Count, b.Count);
            while (count-- > 0)
            {
                dynamic x = a.ElementAt(count);
                dynamic y = b.ElementAt(count);
                if (x > y) return true;
                if (x < y) return false;
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
            a.Add(a.Sign);
            int count = a.Count;
            for (int i = 0; i < count; i++)
                if (!IsZero(a[i] = unchecked((dynamic) a[i] + Uno)))
                    break;
            for (;
                count > 1 && IsZero(a.ElementAt(count - 1)) &&
                IsZero((dynamic) a.ElementAt(count - 2) & SignMask);
                count--)
                a.Pop();
            for (;
                count > 1 && IsOne(~(dynamic)a.ElementAt(count - 1)) &&
                !IsZero((dynamic) a.ElementAt(count - 2) & SignMask);
                count--)
                a.Pop();
            if (count == 1 && IsZero(a.ElementAt(count - 1))) a.Pop();
            return a;
        }

        public static BigInt<T> operator --(BigInt<T> a)
        {
            a.Add(a.Sign);
            int count = a.Count;
            for (int i = 0; i < count; i++)
                if (!IsOne(~(dynamic)unchecked(a[i] = (dynamic)a[i] - Uno)))
                    break;
            for (;
                count > 1 && IsZero(a.ElementAt(count - 1)) &&
                IsZero(a.ElementAt(count - 2) & SignMask);
                count--)
                a.Pop();
            for (;
                count > 1 && IsOne(~(dynamic)a.ElementAt(count - 1)) &&
                !IsZero(a.ElementAt(count - 2) & SignMask);
                count--)
                a.Pop();
            if (count == 1 && IsZero(a.ElementAt(count - 1))) a.Pop();
            return a;
        }

        #endregion
    }
}