using Microsoft.VisualStudio.TestTools.UnitTesting;

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
            Assert.AreEqual("12345678987654321", (BigInt<uint>.Pow(new BigInt<uint>("-111111111"), 2)).ToString());
            Assert.AreEqual("1000000000000000000",
                BigInt<uint>.Pow(-BigInt<uint>.Pow(new BigInt<uint>("-10"), 9), 2).ToString());
        }
    }
}