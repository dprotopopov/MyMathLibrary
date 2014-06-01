using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using MyLibrary.Collections;

namespace MyMath.GF2
{
    public class BooleanMatrix : Vector<BooleanVector>
    {
        public BooleanMatrix(IEnumerable<IEnumerable<bool>> list)
        {
            foreach (var item in list)
                Add(new BooleanVector(item));
        }

        public BooleanMatrix(IEnumerable<int> indexes)
        {
            foreach (int item in indexes)
                Add(new BooleanVector(Enumerable.Repeat(false, item))
                {
                    true,
                    Enumerable.Repeat(false, indexes.Count() - item - 1)
                });
            for (int i = 0; i < indexes.Count(); i++) this[i][i] = true;
            Debug.Assert(Count == Length);
        }

        /// <summary>
        ///     Количество столбцов матрицы
        /// </summary>
        public int Length
        {
            get { return this.Max(row => row.Count); }
        }

        /// <summary>
        ///     Таким образом, каждый базис в этом пространстве получается из данного базиса при помощи цепочки элементарных
        ///     преобразований. А на матричном языке проблема распознавания планарности сводится к нахождению такой матрицы в
        ///     классе эквивалентных матриц (т.е. матриц, которые получаются друг из друга при помощи элементарных преобразований
        ///     над строками), у которой в каждом столбце содержится не более двух единиц [6].
        ///     Указанный критерий позволяет разработать методику определения планарности графа, сводя проблему планарности к
        ///     отысканию минимума некоторого функционала на множестве базисов подпространства квазициклов. Определим следующий
        ///     функционал на матрице С, соответствующий базису подпространства квазициклов (и будем его впредь называть
        ///     функционалом Мак-Лейна)
        ///     Очевидно, что матрица С соответствует базису Мак-Лейна (т.е. базису, удовлетворяющему условию Мак-Лейна) тогда и
        ///     только тогда, когда F(С) = 0.
        /// </summary>
        public int MacLane
        {
            get
            {
                var list = new StackListQueue<int>();
                int length = Length;
                for (int i = 0; i < length; i++)
                {
                    list.Add(this.Count(row => (row.Count > i) && row[i]));
                }
                return list.Sum(s => s*s - 3*s) + 2*length;
            }
        }

        public int E
        {
            get { return this.Sum(item1 => this.Sum(item2 => BooleanVector.Module(item1, item2))); }
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
        ///     Определитель булевой матрицы
        ///     Матрица обратима тогда и только тогда,
        ///     когда определитель матрицы отличен от нуля
        /// </summary>
        public bool Det()
        {
            Debug.Assert(Rows == Columns);
            // Приведение матрицы к каноническому виду
            GaussJordan();
            // Проверка на нулевые строки
            return this.All(NotZero);
        }

        private bool NotZero(BooleanVector arg)
        {
            return arg.Any(NotZero);
        }

        private bool NotZero(bool arg)
        {
            return arg;
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

            var prev = new bool[row, col];
            var next = new bool[row, col];

            var read = new object();
            var write = new object();

            Parallel.ForEach(
                from i in Enumerable.Range(0, Rows)
                from j in Enumerable.Range(0, Columns)
                select new[] {i, j}, pair =>
                {
                    int i = pair[0];
                    int j = pair[1];
                    bool x;
                    lock (read) x = this[i][j];
                    lock (write) prev[i, j] = x;
                });

            for (int i = 0; i < Math.Min(Rows, Columns) && FindNotZero(prev, i, ref row, ref col); i++)
            {
                GaussJordanStep(prev, next, row, col);
                bool[,] t = prev;
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
                    bool x;
                    lock (read) x = prev[i, j];
                    lock (write) this[i][j] = x;
                });
        }

        private static bool FindNotZero(bool[,] items, int i, ref int row, ref int col)
        {
            Debug.Assert(row <= items.GetLength(0));
            Debug.Assert(col <= items.GetLength(1));
            int total = (row - i)*(col - i);
            int n = col - i;
            for (int j = 0; j < total; j++)
            {
                row = i + j/n;
                col = i + (j%n);
                Debug.Assert(row <= items.GetLength(0));
                Debug.Assert(col <= items.GetLength(1));
                if (items[row, col])
                    return true;
            }
            return false;
        }

        public static void GaussJordanStep(bool[,] prev, bool[,] next, int row, int col)
        {
            Debug.Assert(prev.GetLength(0) == next.GetLength(0));
            Debug.Assert(prev.GetLength(1) == next.GetLength(1));

            bool d = prev[row, col];

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
                        lock (write) next[i, j] = true;
                    else if (j == col)
                        lock (write) next[i, j] = false;
                    else if (i == row)
                    {
                        bool a;
                        lock (read) a = prev[i, j];
                        bool y = Div(a, d);
                        lock (write) next[i, j] = y;
                    }
                    else
                    {
                        bool a, b, c;
                        lock (read)
                        {
                            a = prev[i, j];
                            b = prev[i, col];
                            c = prev[row, j];
                        }
                        bool y = SubMulDiv(a, b, c, d);
                        lock (write) next[i, j] = y;
                    }
                });
        }

        public static int CompareByIndexOfFirstNotZero(BooleanVector x, BooleanVector y)
        {
            int index1 = x.IndexOf(true);
            int index2 = y.IndexOf(true);
            return index1 - index2;
        }

        private static bool Div(bool a, bool b)
        {
            return a && b;
        }

        private static bool SubMulDiv(bool a, bool b, bool c, bool d)
        {
            return a ^ (b && c && d);
        }

        public override string ToString()
        {
            return string.Join(Environment.NewLine, this.Select(item => item.ToString()));
        }
    }
}