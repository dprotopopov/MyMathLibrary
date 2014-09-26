using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MyMath.Double;

namespace MyMath.UnitTest
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod0()
        {
            var x = new BigInt<uint>();
            var y = new BigInt<uint>();
            x++;
            x++;
            y--;
            y--;
            Assert.IsTrue((x*x) == (y*y));
        }

        [TestMethod]
        public void TestMethod1()
        {
            var x = new BigInt<uint>();
            x--;
            x--;
            for (int i = 0; i < 10; i++)
            {
                x = BigInt<uint>.Square(x);
            }
            Assert.IsTrue(BigInt<uint>.IsPositive(x));
            Assert.IsTrue(BigInt<uint>.IsNegative(-x));
            x >>= 1024;
            Assert.IsFalse(BigInt<uint>.IsZero(x));
            x >>= 1;
            Assert.IsTrue(BigInt<uint>.IsZero(x));
        }

        [TestMethod]
        public void TestMethod2()
        {
            Assert.AreEqual("11", new BigInt<uint>("11").ToString());
            Assert.AreEqual("121", BigInt<uint>.Pow(new BigInt<uint>("11"), 2).ToString());
            Assert.AreEqual("1331", BigInt<uint>.Pow(new BigInt<uint>("11"), 3).ToString());
            Assert.AreEqual("12345678987654321",
                ((new BigInt<uint>("-111111111"))*(new BigInt<uint>("-111111111"))).ToString());
            Assert.IsTrue(BigInt<uint>.IsNegative(new BigInt<uint>("-111111111")));
            Assert.AreEqual("12345678987654321", (BigInt<uint>.Square(new BigInt<uint>("111111111"))).ToString());
            Assert.AreEqual("12345678987654321", (BigInt<uint>.Square(new BigInt<uint>("-111111111"))).ToString());
            Assert.AreEqual("12345678987654321", (BigInt<uint>.Pow(new BigInt<uint>("-111111111"), 2)).ToString());
            Assert.AreEqual("1000000000000000000",
                BigInt<uint>.Pow(-BigInt<uint>.Pow(new BigInt<uint>("-10"), 9), 2).ToString());
            Assert.AreEqual(BigInt<uint>.Pow(BigInt<uint>.Pow(new BigInt<uint>("-9"), 100), 2).ToString(),
                BigInt<uint>.Pow(-BigInt<uint>.Pow(new BigInt<uint>("3"), 200), 2).ToString());
        }

        [TestMethod]
        public void TestMethod3()
        {
            try
            {
                dynamic xx = default(double);
                dynamic yy = default(double);
                dynamic zz = xx*yy;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Assert.IsTrue(false);
            }
        }

        [TestMethod]
        public void TestMethod4()
        {
            int n = 10;
            var random = new Random();
            var a = new DoubleMatrix(n, n);
            for (int i = 0; i < n; i++)
                for (int j = 0; j < n; j++)
                    if (i != j)
                        a[i][j] = random.NextDouble();

            var e = new DoubleMatrix(n, n);
            for (int i = 0; i < n; i++) e[i][i] = 1.0;

            DoubleMatrix b = e - a;

            var z = new DoubleMatrix(b.Select(row => new DoubleVector(row)));

            // Дописывание к выборке единичной матрицы справа
            b.AppendColumns(e);

            // Приведение выборки к каноническому виду преобразованиями по строкам
            b.GaussJordan(
                DoubleMatrix.Search.SearchByRows,
                DoubleMatrix.Transform.TransformByRows,
                0, n);

            // Сортировка строк для приведения канонической матрицы к единичной матрице
            var dic = new Dictionary<int, DoubleVector>();
            for (int i = 0; i < n; i++)
            {
                int j = 0;
                DoubleVector vector = b[i];
                while (DoubleVector.IsZero(vector[j])) j++;
                dic.Add(j, vector);
            }

            // Получение обратной матрицы для E-A
            var c = new DoubleMatrix();
            for (int i = 0; i < n; i++) c.Add(new DoubleVector(dic[i].GetRange(n, n)));

            // Проверяем, что полученная матрица действительно является обратной
            DoubleMatrix y = c*z;
            for (int i = 0; i < n; i++)
                for (int j = 0; j < n; j++)
                    if (i == j)
                    {
                        Assert.IsTrue(DoubleMatrix.IsZero(y[i][j] - 1.0));
                    }
                    else
                    {
                        Assert.IsTrue(DoubleMatrix.IsZero(y[i][j]));
                    }
        }

        [TestMethod]
        public void TestMethod5()
        {
            try
            {
                double x = M<double>.Test(1.0, 1.0);
                Assert.IsTrue(x == 2.0);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Assert.IsTrue(false);
            }
        }

        private class L<T> : List<T>
        {
        }

        private class M<T> : V<V<T>>
        {
            public static T Test(T a, T b)
            {
                dynamic x = a;
                dynamic y = b;
                T z = x + y; //Stack Overflow HERE
                return z;
            }
        }

        private class V<T> : L<T>
        {
        }

        private class X<T> : List<T>
        {
        }

        private class Y<T> : X<X<T>>
        {
            public static T Test(T a, T b)
            {
                dynamic x = a;
                dynamic y = b;
                T z = x + y; //Ok
                return z;
            }
        }
    }
}