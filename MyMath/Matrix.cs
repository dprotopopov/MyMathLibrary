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
        public enum Search
        {
            SearchByRows = 1,
            SearchByColumns = -1,
        };

        public enum Transform
        {
            TransformByRows = 1,
            TransformByColumns = -1,
        };

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
                Add(new Vector<T>(Enumerable.Repeat(default(T), cols)));
        }

        public Matrix(IEnumerable<IEnumerable<T>> array)
        {
            foreach (var vector in array)
                Add(new Vector<T>(vector));
        }

        public Matrix()
        {
        }

        public int Rows
        {
            get { return Count; }
        }

        public int Columns
        {
            get { return this.Any() ? this.Max(row => row.Count) : 0; }
        }

        public void AddColumn()
        {
            foreach (var row in this) row.Append(default(T));
        }

        public Matrix<T> SubMatrix(IEnumerable<int> rows, IEnumerable<int> columns)
        {
            var subMatrix = new Matrix<T>(rows.Count(), columns.Count());
            var read = new object();
            var write = new object();

            Parallel.ForEach(
                from i in Enumerable.Range(0, rows.Count())
                from j in Enumerable.Range(0, columns.Count())
                select new {row = i, col = j}, pair =>
                {
                    int i = pair.row;
                    int j = pair.col;
                    T x;
                    lock (read) x = this[rows.ElementAt(i)][columns.ElementAt(j)];
                    lock (write) subMatrix[i][j] = x;
                });

            return subMatrix;
        }

        public static Matrix<T> operator -(Matrix<T> a, Matrix<T> b)
        {
            Debug.Assert(a.Rows == b.Rows);
            Debug.Assert(a.Columns == b.Columns);
            int rows = Math.Max(a.Rows, b.Rows);
            int columns = Math.Max(a.Columns, b.Columns);
            var result = new Matrix<T>(rows, columns);
            for (int i = 0; i < rows; i++)
                for (int j = 0; j < columns; j++)
                {
                    T x = a[i][j];
                    T y = b[i][j];
                    result[i][j] = (T) Convert.ChangeType(Convert.ToDouble(x) - Convert.ToDouble(y), typeof (T));
                }
            return result;
        }

        public static Matrix<T> operator +(Matrix<T> a, Matrix<T> b)
        {
            Debug.Assert(a.Rows == b.Rows);
            Debug.Assert(a.Columns == b.Columns);
            int rows = Math.Max(a.Rows, b.Rows);
            int columns = Math.Max(a.Columns, b.Columns);
            var result = new Matrix<T>(rows, columns);
            for (int i = 0; i < rows; i++)
                for (int j = 0; j < columns; j++)
                {
                    T x = a[i][j];
                    T y = b[i][j];
                    result[i][j] = (T) Convert.ChangeType(Convert.ToDouble(x) + Convert.ToDouble(y), typeof (T));
                }
            return result;
        }

        public static Matrix<T> operator *(Matrix<T> a, Matrix<T> b)
        {
            Debug.Assert(a.Columns == b.Rows);
            int rows = a.Rows;
            int columns = b.Columns;
            int commons = Math.Max(a.Columns, b.Rows);
            var result = new Matrix<T>(rows, columns);
            for (int i = 0; i < rows; i++)
                for (int j = 0; j < columns; j++)
                {
                    T t = default(T);
                    for (int k = 0; k < commons; k++)
                    {
                        T x = a[i][k];
                        T y = b[k][j];
                        t =
                            (T)
                                Convert.ChangeType(Convert.ToDouble(t) + (Convert.ToDouble(x)*Convert.ToDouble(y)),
                                    typeof (T));
                    }
                    result[i][j] = t;
                }
            return result;
        }

        public static bool IsZero(Matrix<T> a)
        {
            return a.All(row => row.All(IsZero));
        }

        public static bool IsZero(T arg)
        {
            double x = Math.Abs(Convert.ToDouble(arg));
            return x <= 0.0*x;
        }

        public void AddRow()
        {
            Append(new Vector<T>(Enumerable.Repeat(default(T), Columns)));
        }

        public void AppendColumns(IEnumerable<IEnumerable<T>> b)
        {
            Debug.Assert(Rows == b.Count());
            Debug.Assert(this.All(row => row.Count == Columns));

            int index = 0;
            foreach (var row in this)
            {
                row.Append(b.ElementAt(index++));
            }
        }

        public void AppendRows(IEnumerable<IEnumerable<T>> b)
        {
            foreach (var row in b)
            {
                Append(new Vector<T>(row));
            }
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
        public void GaussJordan(Search search = Search.SearchByRows,
            Transform transform = Transform.TransformByRows,
            int first = 0,
            int last = Int32.MaxValue)
        {
            int row = Math.Min(Rows, last);
            int col = Math.Min(Columns, last);

            var prev = new T[Rows, Columns];
            var next = new T[Rows, Columns];

            var read = new object();
            var write = new object();

            Parallel.ForEach(
                from i in Enumerable.Range(0, Rows)
                from j in Enumerable.Range(0, Columns)
                select new {row = i, col = j}, pair =>
                {
                    int i = pair.row;
                    int j = pair.col;
                    T x;
                    lock (read) x = this[i][j];
                    lock (write) prev[i, j] = x;
                });

            for (int i = first;
                i < Math.Min(Math.Min(Rows, Columns), last) && FindNotZero(search, prev, i, ref row, ref col);
                i++)
            {
                GaussJordanStep(transform, prev, next, row, col);
                T[,] t = prev;
                prev = next;
                next = t;
                row = Math.Min(Rows, last);
                col = Math.Min(Columns, last);
            }

            Parallel.ForEach(
                from i in Enumerable.Range(0, Rows)
                from j in Enumerable.Range(0, Columns)
                select new {row = i, col = j}, pair =>
                {
                    int i = pair.row;
                    int j = pair.col;
                    T x;
                    lock (read) x = prev[i, j];
                    lock (write) this[i][j] = x;
                });
        }

        private static bool FindNotZero(Search search, T[,] items, int i, ref int row, ref int col)
        {
            Debug.Assert(row <= items.GetLength(0));
            Debug.Assert(col <= items.GetLength(1));
            switch (search)
            {
                case Search.SearchByRows:
                    for (int j = 0, total = (row - i)*col, n = col; j < total; j++)
                    {
                        row = i + (j/n);
                        col = (j%n);
                        Debug.Assert(row <= items.GetLength(0));
                        Debug.Assert(col <= items.GetLength(1));
                        T x = items[row, col];
                        if (Math.Abs(Convert.ToDouble(x)) > 0.0) return true;
                    }
                    return false;
                case Search.SearchByColumns:
                    for (int j = 0, total = row*(col - i), n = row; j < total; j++)
                    {
                        col = i + (j/n);
                        row = (j%n);
                        Debug.Assert(row <= items.GetLength(0));
                        Debug.Assert(col <= items.GetLength(1));
                        T x = items[row, col];
                        if (Math.Abs(Convert.ToDouble(x)) > 0.0) return true;
                    }
                    return false;
            }
            throw new NotImplementedException();
        }

        public static void GaussJordanStep(Transform transform, T[,] prev, T[,] next, int row, int col)
        {
            Debug.Assert(prev.GetLength(0) == next.GetLength(0));
            Debug.Assert(prev.GetLength(1) == next.GetLength(1));

            int rows = prev.GetLength(0);
            int cols = prev.GetLength(1);

            var read = new object();
            var write = new object();

            T x;
            lock (read) x = prev[row, col];
            double d = Convert.ToDouble(x);
            Debug.Assert(Math.Abs(d) > 0.0);

            Parallel.ForEach(
                from i in Enumerable.Range(0, rows)
                from j in Enumerable.Range(0, cols)
                select new {row = i, col = j}, pair =>
                {
                    int i = pair.row;
                    int j = pair.col;
                    if (i == row && j == col)
                    {
                        var one = (T) Convert.ChangeType(1, typeof (T));
                        lock (write) next[i, j] = one;
                    }
                    else if (j == col && transform == Transform.TransformByRows ||
                             i == row && transform == Transform.TransformByColumns)
                        lock (write) next[i, j] = default(T);
                    else if (i == row && transform == Transform.TransformByRows ||
                             j == col && transform == Transform.TransformByColumns)
                    {
                        T a;
                        lock (read) a = prev[i, j];
                        var y = (T) Convert.ChangeType(Convert.ToDouble(a)/d, typeof (T));
                        lock (write) next[i, j] = y;
                    }
                    else
                    {
                        T a, b, c;
                        lock (read) a = prev[i, j];
                        lock (read) b = prev[i, col];
                        lock (read) c = prev[row, j];
                        var y =
                            (T)
                                Convert.ChangeType(
                                    Convert.ToDouble(a) - (Convert.ToDouble(b)*Convert.ToDouble(c)/d),
                                    typeof (T));
                        lock (write) next[i, j] = y;
                    }
                });
        }

        public static int CompareByIndexOfFirstNotZero(Vector<T> x, Vector<T> y)
        {
            int index1 = x.IndexOf(x.First(Vector<T>.NotZero));
            int index2 = y.IndexOf(y.First(Vector<T>.NotZero));
            return index1 - index2;
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
            if (this.Any(IsZero)) return default(T);
            int parity = this.Sum(row => row.IndexOf(row.First(Vector<T>.NotZero)));
            T s =
                this.Select(row => row.First(Vector<T>.NotZero))
                    .Aggregate((x, y) => ((dynamic) x*(dynamic) y));
            return ((parity & 1) == 0) ? s : (- (dynamic) s);
        }
    }
}