﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
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
                Add(new Vector<T>(Enumerable.Repeat(default(T), cols)));
        }

        public Matrix(IEnumerable<IEnumerable<T>> array)
        {
            foreach (var vector in array)
                Add(new Vector<T>(vector));
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
            Debug.WriteLine("Begin {0}::{1}", GetType().Name, MethodBase.GetCurrentMethod().Name);
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
                select new {row = i, col = j}, pair =>
                {
                    int i = pair.row;
                    int j = pair.col;
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
                select new {row = i, col = j}, pair =>
                {
                    int i = pair.row;
                    int j = pair.col;
                    T x;
                    lock (read) x = prev[i, j];
                    lock (write) this[i][j] = x;
                });
            Debug.WriteLine("End {0}::{1}", GetType().Name, MethodBase.GetCurrentMethod().Name);
        }

        private static bool FindNotZero(T[,] items, int i, ref int row, ref int col)
        {
            Debug.WriteLine("Begin {0}::{1}", typeof (Matrix<T>).Name, MethodBase.GetCurrentMethod().Name);
            Debug.Assert(row <= items.GetLength(0));
            Debug.Assert(col <= items.GetLength(1));
            int total = (row - i)*(col - i);
            int n = col - i;
            int j;
            for (j = 0; j < total; j++)
            {
                row = i + (j/n);
                col = i + (j%n);
                Debug.Assert(row <= items.GetLength(0));
                Debug.Assert(col <= items.GetLength(1));
                T x = items[row, col];
                if (Math.Abs(Convert.ToDouble(x)) > 0.0) break;
            }
            Debug.WriteLine("End {0}::{1}", typeof (Matrix<T>).Name, MethodBase.GetCurrentMethod().Name);
            return j < total;
        }

        public static void GaussJordanStep(T[,] prev, T[,] next, int row, int col)
        {
            Debug.WriteLine("Begin {0}::{1}", typeof (Matrix<T>).Name, MethodBase.GetCurrentMethod().Name);
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
                    else if (j == col)
                        lock (write) next[i, j] = default(T);
                    else if (i == row)
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
                            (T) Convert.ChangeType(Convert.ToDouble(a) - (Convert.ToDouble(b)*Convert.ToDouble(c)/d),
                                typeof (T));
                        lock (write) next[i, j] = y;
                    }
                });
            Debug.WriteLine("End {0}::{1}", typeof (Matrix<T>).Name, MethodBase.GetCurrentMethod().Name);
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