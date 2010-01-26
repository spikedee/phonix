using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Phonix.UnitTest
{
    using NUnit.Framework;

    [TestFixture]
    public class SymbolTest
    {
        private static Symbol _symbolA;
        public static Symbol SymbolA
        {
            get
            {
                if (_symbolA == null)
                {
                    _symbolA = new Symbol("a", FeatureMatrixTest.MatrixA);
                }
                return _symbolA;
            }
        }

        private static Symbol _symbolB;
        public static Symbol SymbolB
        {
            get
            {
                if (_symbolB == null)
                {
                    _symbolB = new Symbol("b", FeatureMatrixTest.MatrixB);
                }
                return _symbolB;
            }
        }

        private static Symbol _symbolC;
        public static Symbol SymbolC
        {
            get
            {
                if (_symbolC == null)
                {
                    _symbolC = new Symbol("c", FeatureMatrixTest.MatrixC);
                }
                return _symbolC;
            }
        }

        [Test]
        public void Ctor()
        {
            Symbol s = new Symbol("s", FeatureMatrixTest.MatrixA);

            Assert.AreSame(FeatureMatrixTest.MatrixA, s.FeatureMatrix);
            Assert.AreEqual("s", s.Label);
            Assert.AreSame(s.Label, s.ToString());
        }

        [Test]
        public void Matches()
        {
            Symbol s = SymbolA;

            Assert.IsTrue(s.Matches(null, FeatureMatrixTest.MatrixA));
            Assert.IsFalse(s.Matches(null, FeatureMatrix.Empty));
        }

        [Test]
        public void Combine()
        {
            Symbol s = SymbolA;

            var fm = s.Combine(null, FeatureMatrix.Empty);
            Assert.AreSame(s.FeatureMatrix, fm);

            fm = s.Combine(null, FeatureMatrixTest.MatrixA);
            Assert.AreSame(s.FeatureMatrix, fm);
        }
    }
}
