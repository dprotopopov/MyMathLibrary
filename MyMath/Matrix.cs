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

        public Matrix(IEnumerable<Vector<T>> array)
        {
            AddRange(array);
        }

        public Matrix(IEnumerable<IEnumerable<T>> array)
        {
            foreach (var vector in array)
                Add(new Vector<T>(vector));
        }

        public Matrix()
        {
        }

        public Matrix(T x) : base(new Vector<T>(x))
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
            foreach (var row in this) row.Add(default(T));
        }

        public Matrix<T> SubMatrix(IEnumerable<int> rows, IEnumerable<int> columns)
        {
            var subMatrix = new Matrix<T>(rows.Count(), columns.Count());

            Parallel.ForEach(
                from i in Enumerable.Range(0, rows.Count())
                from j in Enumerable.Range(0, columns.Count())
                select new {row = i, col = j}, pair =>
                {
                    int i = pair.row;
                    int j = pair.col;
                    subMatrix[i][j] = this[rows.ElementAt(i)][columns.ElementAt(j)];
                });

            return subMatrix;
        }

        public static Matrix<T> operator -(Matrix<T> a, Matrix<T> b)
        {
            return new Matrix<T>(a as Vector<Vector<T>> - b as Vector<Vector<T>>);
        }

        public static Matrix<T> operator +(Matrix<T> a, Matrix<T> b)
        {
            return new Matrix<T>(a as Vector<Vector<T>> + b as Vector<Vector<T>>);
        }

        public static Matrix<T> operator *(Matrix<T> a, Matrix<T> b)
        {
            Debug.Assert(a.Columns == b.Rows);
            int rows = a.Rows;
            int columns = b.Columns;
            int commons = Math.Max(a.Columns, b.Rows);
            var result = new Matrix<T>(rows, columns);
            Parallel.ForEach(
                from i in Enumerable.Range(0, rows)
                from j in Enumerable.Range(0, columns)
                select new {row = i, col = j}, pair =>
                {
                    int i = pair.row;
                    int j = pair.col;
                    T t = default(T);
                    for (int k = 0; k < commons; k++)
                    {
                        dynamic x = a[i][k];
                        dynamic y = b[k][j];
                        t = t + x*y;
                    }
                    result[i][j] = t;
                });
            return result;
        }

        public static bool IsZero(Matrix<T> a)
        {
            return a.All(row => row.All(IsZero));
        }

        public static bool IsZero(T arg)
        {
            return (dynamic) arg == default(T);
        }

        public void AddRow()
        {
            Add(new Vector<T>(Enumerable.Repeat(default(T), Columns)));
        }

        public void AppendColumns(IEnumerable<IEnumerable<T>> b)
        {
            Debug.Assert(Rows == b.Count());
            Debug.Assert(this.All(row => row.Count == Columns));

            int index = 0;
            foreach (var row in this)
                row.Add(b.ElementAt(index++));
        }

        public void AppendRows(IEnumerable<IEnumerable<T>> b)
        {
            foreach (var row in b)
                Add(new Vector<T>(row));
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

            for (int i = first;
                i < Math.Min(Math.Min(Rows, Columns), last) && FindNotZero(search, i, ref row, ref col);
                i++)
            {
                GaussJordanStep(transform, row, col);
                row = Math.Min(Rows, last);
                col = Math.Min(Columns, last);
            }
        }

        private bool FindNotZero(Search search, int i, ref int row, ref int col)
        {
            Debug.Assert(row <= Rows);
            Debug.Assert(col <= Columns);
            switch (search)
            {
                case Search.SearchByRows:
                    for (int j = 0, total = (row - i)*col, n = col; j < total; j++)
                    {
                        row = i + (j/n);
                        col = (j%n);
                        if (!IsZero(this[row][col])) return true;
                    }
                    return false;
                case Search.SearchByColumns:
                    for (int j = 0, total = row*(col - i), n = row; j < total; j++)
                    {
                        col = i + (j/n);
                        row = (j%n);
                        if (!IsZero(this[row][col])) return true;
                    }
                    return false;
            }
            throw new NotImplementedException();
        }

        public void GaussJordanStep(Transform transform, int row, int col)
        {
            dynamic d = this[row][col];

            var rows = new StackListQueue<int>();
            var cols = new StackListQueue<int>();

            for (int i = 0; i < Rows; i++) if (!IsZero(this[i][col])) rows.Add(i);
            for (int j = 0; j < Columns; j++) if (!IsZero(this[row][j])) cols.Add(j);

            rows.Remove(row);
            cols.Remove(col);

            Parallel.ForEach(
                from i in rows
                from j in cols
                select new {row = i, col = j}, pair =>
                {
                    int i = pair.row;
                    int j = pair.col;
                    dynamic a = this[i][j];
                    dynamic b = this[i][col];
                    dynamic c = this[row][j];
                    this[i][j] = a - b*c/d;
                });

            switch (transform)
            {
                case Transform.TransformByRows:
                    Parallel.ForEach(rows, i => { this[i][col] = default(T); });
                    Parallel.ForEach(cols, j =>
                    {
                        dynamic a = this[row][j];
                        this[row][j] = a/d;
                    });
                    break;
                case Transform.TransformByColumns:
                    Parallel.ForEach(cols, j => { this[row][j] = default(T); });
                    Parallel.ForEach(rows, i =>
                    {
                        dynamic a = this[i][col];
                        this[i][col] = a/d;
                    });
                    break;
            }

            this[row][col] = (T) (dynamic) 1;
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
                    .Aggregate((x, y) => ((T) ((dynamic) x*(dynamic) y)));
            return ((parity & 1) == 0) ? s : (- (dynamic) s);
        }
    }
}