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
            Parallel.ForEach(Enumerable.Range(0, indexes.Count()), i => { this[i][i] = true; });
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
            return this.All(BooleanVector.NotZero);
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
                row = Rows;
                col = Columns;
            }
        }

        private bool FindNotZero(Search search, int i, ref int row, ref int col)
        {
            Debug.Assert(row <= Rows);
            Debug.Assert(col <= Columns);
            switch (search)
            {
                case Search.SearchByRows:
                    for (int j = 0, total = (row - i) * col, n = col; j < total; j++)
                    {
                        row = i + (j / n);
                        col = (j % n);
                        if (this[row][col]) return true;
                    }
                    return false;
                case Search.SearchByColumns:
                    for (int j = 0, total = row * (col - i), n = row; j < total; j++)
                    {
                        col = i + (j / n);
                        row = (j % n);
                        if (this[row][col]) return true;
                    }
                    return false;
            }
            throw new NotImplementedException();
        }

        public void GaussJordanStep(Transform transform, int row, int col)
        {
            var rows = new StackListQueue<int>();
            var cols = new StackListQueue<int>();

            for (int i = 0; i < Rows; i++) if (this[i][col]) rows.Add(i);
            for (int j = 0; j < Columns; j++) if (this[row][j]) cols.Add(j);

            rows.Remove(row);
            cols.Remove(col);

            Debug.Assert(this[row][col]);

            Parallel.ForEach(
                from i in rows
                from j in cols
                select new { row = i, col = j }, pair =>
                {
                    int i = pair.row;
                    int j = pair.col;
                    this[i][j] = !this[i][j];
                });

            switch (transform)
            {
                case Transform.TransformByRows:
                    Parallel.ForEach(rows, i => { this[i][col] = false; });
                    break;
                case Transform.TransformByColumns:
                    Parallel.ForEach(cols, j => { this[row][j] = false; });
                    break;
            }
        }

        public static int CompareByIndexOfFirstNotZero(BooleanVector x, BooleanVector y)
        {
            int index1 = x.IndexOf(true);
            int index2 = y.IndexOf(true);
            return index1 - index2;
        }

        public override string ToString()
        {
            return string.Join(Environment.NewLine, this.Select(item => item.ToString()));
        }
    }
}