using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using MyLibrary.Collections;

namespace MyMath
{
    public class Ratio<T>
    {
        public Ratio(T integer, T fraction, T divider)
        {
            Debug.Assert(IsPositive(divider));
            Debug.Assert(!IsZero(divider));
            Integer = integer;
            Fraction = fraction;
            Divisor = (!IsZero(integer)) ? (dynamic) fraction + (dynamic) integer*(dynamic) divider : fraction;
            Divider = divider;
        }

        public Ratio(T divisor, T divider)
        {
            Debug.Assert(IsPositive(divider));
            Debug.Assert(!IsZero(divider));
            Integer = (dynamic) divisor/(dynamic) divider;
            Fraction = (dynamic) divisor%(dynamic) divider;
            Divisor = divisor;
            Divider = divider;
        }

        public Ratio(T number)
        {
            Divisor = number;
            Integer = number;
            Fraction = (T) (dynamic) 0;
            Divider = (T) (dynamic) 1;
        }

        private T Divisor { get; set; }
        private T Divider { get; set; }
        private T Fraction { get; set; }
        private T Integer { get; set; }

        protected bool Equals(Ratio<T> other)
        {
            return EqualityComparer<T>.Default.Equals(Divisor, other.Divisor) &&
                   EqualityComparer<T>.Default.Equals(Divider, other.Divider);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((Ratio<T>) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (EqualityComparer<T>.Default.GetHashCode(Divisor)*397) ^
                       EqualityComparer<T>.Default.GetHashCode(Divider);
            }
        }

        // Overloading '+' operator: 
        public static Ratio<T> operator +(Ratio<T> a, Ratio<T> b)
        {
            T divisor = (dynamic) a.Fraction*(dynamic) b.Divider + (dynamic) b.Fraction*(dynamic) a.Divider;
            T divider = (dynamic) a.Divider*(dynamic) b.Divider;
            T gcd = Gcd(divisor, divider);
            divisor = (dynamic) divisor/(dynamic) gcd;
            divider = (dynamic) divider/(dynamic) gcd;
            T integer = ((dynamic) a.Integer + (dynamic) b.Integer) + ((dynamic) divisor/(dynamic) divider);
            T fraction = (dynamic) divisor%(dynamic) divider;
            return new Ratio<T>((dynamic) integer, (dynamic) fraction, (dynamic) divider);
        }

        // Overloading '-' operator: 
        public static Ratio<T> operator -(Ratio<T> a, Ratio<T> b)
        {
            T divisor = (dynamic) a.Fraction*(dynamic) b.Divider - (dynamic) b.Fraction*(dynamic) a.Divider;
            T divider = (dynamic) a.Divider*(dynamic) b.Divider;
            T gcd = Gcd(divisor, divider);
            divisor = (dynamic) divisor/(dynamic) gcd;
            divider = (dynamic) divider/(dynamic) gcd;
            T integer = ((dynamic) a.Integer - (dynamic) b.Integer) + ((dynamic) divisor/(dynamic) divider);
            T fraction = (dynamic) divisor%(dynamic) divider;
            return new Ratio<T>((dynamic) integer, (dynamic) fraction, (dynamic) divider);
        }

        // Overloading '*' operator: 
        public static Ratio<T> operator *(Ratio<T> a, Ratio<T> b)
        {
            Debug.Assert(IsOne(Gcd(a.Divisor, a.Divider)));
            Debug.Assert(IsOne(Gcd(b.Divisor, b.Divider)));
            T gcd = (dynamic) Gcd(a.Divisor, b.Divider)*(dynamic) Gcd(a.Divider, b.Divisor);
            T divisor = (dynamic) a.Divisor*(dynamic) b.Divisor/(dynamic) gcd;
            T divider = (dynamic) a.Divider*(dynamic) b.Divider/(dynamic) gcd;
            return new Ratio<T>((dynamic) divisor, (dynamic) divider);
        }

        // Overloading '*' operator: 
        public static Ratio<T> operator /(Ratio<T> a, Ratio<T> b)
        {
            Debug.Assert(IsOne(Gcd(a.Divisor, a.Divider)));
            Debug.Assert(IsOne(Gcd(b.Divisor, b.Divider)));
            T gcd = (dynamic) Gcd(a.Divisor, b.Divisor)*(dynamic) Gcd(a.Divider, b.Divider);
            T divisor = (dynamic) a.Divisor*(dynamic) b.Divider/(dynamic) gcd;
            T divider = (dynamic) a.Divider*(dynamic) b.Divisor/(dynamic) gcd;
            return new Ratio<T>((dynamic) divisor, (dynamic) divider);
        }

        // Overloading '*' operator: 
        public static Ratio<T> operator ==(Ratio<T> a, Ratio<T> b)
        {
            return (dynamic) a.Divisor == (dynamic) b.Divisor && (dynamic) a.Divider == (dynamic) b.Divider;
        }

        public static Ratio<T> operator !=(Ratio<T> a, Ratio<T> b)
        {
            return (dynamic) a.Divisor != (dynamic) b.Divisor || (dynamic) a.Divider != (dynamic) b.Divider;
        }

        /// <summary>
        ///     Алгоритм нахождения наибольшего общего делителя
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="c"></param>
        /// <param name="d"></param>
        /// <returns></returns>
        public static T Gcd(T a, T b, out T c, out T d)
        {
            var stack = new StackListQueue<T>();
            while (!IsZero(a))
            {
                T q = (dynamic) b/(dynamic) a;
                T r = (dynamic) b%(dynamic) a;
                b = a;
                a = r;
                stack.Push(q);
            }
            c = (T) (dynamic) 1;
            d = (T) (dynamic) 0;
            while (stack.Any())
            {
                T q = stack.Pop();
                T r = (dynamic) q*(dynamic) c + (dynamic) d;
                c = d;
                d = r;
            }
            return b;
        }

        public static T Gcd(T a, T b)
        {
            while (a != (dynamic) 0)
            {
                T r = (dynamic) b%(dynamic) a;
                b = a;
                a = r;
            }
            return b;
        }

        private bool IsPositive(T a)
        {
            return (dynamic)a >= (T)(dynamic)0;
        }

        public static bool IsZero(T a)
        {
            return (dynamic) a == (T) (dynamic) 0;
        }

        private static bool IsOne(T a)
        {
            return (dynamic) a == (T) (dynamic) 1;
        }
    }
}