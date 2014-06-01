using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using MyLibrary.Collections;

namespace MyMath
{
    public class Matrix<T> : Vector<Vector<T>>
    {
        /// <summary>
        ///     Создание матрицы, состоящей из матрицы коэффициентов и вектора правой части
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        public Matrix(IEnumerable<IEnumerable<T>> a, IEnumerable<T> b)
        {
            Debug.Assert(a.Count() == b.Count());
            int count = Math.Min(a.Count(), b.Count());
            for (int i = 0; i < count; i++)
                Add(new Vector<T>(new StackListQueue<T>(a.ElementAt(i)) {b.ElementAt(i)}));
        }


        public Matrix(int rows, int cols)
        {
            for (int i = 0; i < rows; i++)
                Add(new Vector<T>(Enumerable.Repeat((T) (dynamic) 0, cols)));
        }

        public Matrix(IEnumerable<IEnumerable<T>> array)
        {
            foreach (var r  in array)
                Add(new Vector<T>(r));
        }

        protected Matrix()
        {
        }

        public int Rows
        {
            get { return Count; }
        }

        public int Columns
        {
            get { return this.Max(row => row.Count); }
        }

        /// <summary>
        ///     Последовательно будем выбирать разрешающий элемент РЭ, который лежит на главной диагонали матрицы.
        ///     На месте разрешающего элемента получаем 1, а в самом столбце записываем нули.
        ///     Все остальные элементы матрицы, включая элементы столбца, определяются по правилу прямоугольника.
        ///     Для этого выбираем четыре числа, которые расположены в вершинах прямоугольника и всегда включают
        ///     разрешающий элемент РЭ.
        ///     НЭ = СЭ - (А*В)/РЭ
        ///     РЭ - разрешающий элемент, А и В - элементы матрицы, образующие прямоугольник с элементами СЭ и РЭ.
        /// </summary>
        public void GaussJordan()
        {
            int row = Rows;
            int col = Columns;

            Debug.Assert(this.All(r => r.Count == col));

            var prev = new T[row, col];
            var next = new T[row, col];

            var read = new object();
            var write = new object();

            Parallel.ForEach(
                from i in Enumerable.Range(0, Rows)
                from j in Enumerable.Range(0, Columns)
                select new[] {i, j}, pair =>
                {
                    int i = pair[0];
                    int j = pair[1];
                    T x;
                    lock (read) x = this[i][j];
                    lock (write) prev[i, j] = x;
                });

            for (int i = 0; i < Math.Min(Rows, Columns) && FindNotZero(prev, i, ref row, ref col); i++)
            {
                GaussJordanStep(prev, next, row, col);
                T[,] t = prev;
                prev = next;
                next = t;
                row = Rows;
                col = Columns;
            }

            Parallel.ForEach(
                from i in Enumerable.Range(0, Rows)
                from j in Enumerable.Range(0, Columns)
                select new[] {i, j}, pair =>
                {
                    int i = pair[0];
                    int j = pair[1];
                    T x;
                    lock (read) x = prev[i, j];
                    lock (write) this[i][j] = x;
                });
        }

        private static bool FindNotZero(T[,] items, int i, ref int row, ref int col)
        {
            Debug.Assert(row <= items.GetLength(0));
            Debug.Assert(col <= items.GetLength(1));
            int total = (row - i)*(col - i);
            int n = col - i;
            for (int j = 0; j < total; j++)
            {
                row = i + (j/n);
                col = i + (j%n);
                Debug.Assert(row <= items.GetLength(0));
                Debug.Assert(col <= items.GetLength(1));
                if (!IsZero(items[row, col]))
                    return true;
            }
            return false;
        }

        public static void GaussJordanStep(T[,] prev, T[,] next, int row, int col)
        {
            Debug.Assert(prev.GetLength(0) == next.GetLength(0));
            Debug.Assert(prev.GetLength(1) == next.GetLength(1));

            T d = prev[row, col];

            var read = new object();
            var write = new object();

            Parallel.ForEach(
                from i in Enumerable.Range(0, prev.GetLength(0))
                from j in Enumerable.Range(0, prev.GetLength(1))
                select new[] {i, j}, pair =>
                {
                    int i = pair[0];
                    int j = pair[1];
                    if (i == row && j == col)
                        lock (write) next[i, j] = (T) (dynamic) 1;
                    else if (j == col)
                        lock (write) next[i, j] = (T) (dynamic) 0;
                    else if (i == row)
                    {
                        T a;
                        lock (read) a = prev[i, j];
                        T y = Div(a, d);
                        lock (write) next[i, j] = y;
                    }
                    else
                    {
                        T a, b, c;
                        lock (read)
                        {
                            a = prev[i, j];
                            b = prev[i, col];
                            c = prev[row, j];
                        }
                        T y = SubMulDiv(a, b, c, d);
                        lock (write) next[i, j] = y;
                    }
                });
        }

        private static T Div(T a, T b)
        {
            return (dynamic) a/(dynamic) b;
        }

        private static T SubMulDiv(T a, T b, T c, T d)
        {
            return (dynamic) a - ((dynamic) b*(dynamic) c/(dynamic) d);
        }

        public static bool IsZero(T a)
        {
            return unchecked((dynamic) a == (T) (dynamic) 0);
        }

        public static int CompareByIndexOfFirstNotZero(Vector<T> x, Vector<T> y)
        {
            int index1 = x.IndexOf(x.First(NotZero));
            int index2 = y.IndexOf(y.First(NotZero));
            return index1 - index2;
        }

        private static bool NotZero(T arg)
        {
            return (dynamic) arg != (T) (dynamic) 0;
        }

        /// <summary>
        ///     Определитель матрицы
        ///     Матрица обратима тогда и только тогда,
        ///     когда определитель матрицы отличен от нуля
        /// </summary>
        public T Det()
        {
            Debug.Assert(Rows == Columns);
            // Приведение матрицы к каноническому виду
            GaussJordan();
            // Проверка на нулевые строки
            if (this.Any(IsZero)) return (T) (dynamic) 0;
            int parity = this.Sum(row => row.IndexOf(row.First(NotZero)));
            T s = this.Select(row => row.First(NotZero)).Aggregate((x, y) => (T) ((dynamic) x*(dynamic) y));
            return ((parity & 1) == 0) ? s : (T) (- (dynamic) s);
        }
    }
}