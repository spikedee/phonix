using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Phonix.UnitTest
{
    using NUnit.Framework;

    [TestFixture]
    public class SymbolSetTest
    {
        public static SymbolSet GetTestSet()
        {
            SymbolSet ss = new SymbolSet();
            ss.Add(SymbolTest.SymbolA);
            ss.Add(SymbolTest.SymbolB);
            ss.Add(SymbolTest.SymbolC);
            return ss;
        }

        [Test]
        public void Add()
        {
            SymbolSet ss = new SymbolSet();
            ss.Add(SymbolTest.SymbolA);

            Assert.IsTrue(ss.BaseSymbols.ContainsKey(SymbolTest.SymbolA.Label));
            Assert.AreSame(SymbolTest.SymbolA, ss.BaseSymbols[SymbolTest.SymbolA.Label]);
            Assert.IsTrue(ss.BaseSymbols.ContainsValue(SymbolTest.SymbolA));
        }

        [Test]
        public void Spell()
        {
            SymbolSet ss = GetTestSet();
            ss.Add(new Symbol("z", FeatureMatrix.Empty));

            Symbol a = ss.Spell(FeatureMatrixTest.MatrixA);
            Assert.AreSame(a, SymbolTest.SymbolA);

            Symbol b = ss.Spell(FeatureMatrixTest.MatrixB);
            Assert.AreSame(b, SymbolTest.SymbolB);

            Symbol c = ss.Spell(FeatureMatrixTest.MatrixC);
            Assert.AreSame(c, SymbolTest.SymbolC);

            Symbol z = ss.Spell(FeatureMatrix.Empty);
            Assert.AreEqual("z", z.Label);

            var fms = new FeatureMatrix[] { FeatureMatrix.Empty, FeatureMatrixTest.MatrixA };
            var syms = ss.Spell(fms);

            Assert.AreEqual(2, syms.Count);
            Assert.AreSame(z, syms[0]);
            Assert.AreSame(a, syms[1]);
        }

        [Test]
        [ExpectedException(typeof(SpellingException))]
        public void SpellException()
        {
            SymbolSet ss = new SymbolSet();
            var s = ss.Spell(FeatureMatrixTest.MatrixA);
            Assert.Fail("Shouldn't have gotten " + s);
        }

        [Test]
        public void Pronounce()
        {
            SymbolSet ss = GetTestSet();

            var list = ss.Pronounce("abc");

            Assert.AreEqual(3, list.Count);
            Assert.AreSame(SymbolTest.SymbolA.FeatureMatrix, list[0]);
            Assert.AreSame(SymbolTest.SymbolB.FeatureMatrix, list[1]);
            Assert.AreSame(SymbolTest.SymbolC.FeatureMatrix, list[2]);
        }

        [Test]
        public void PronounceTwo()
        {
            SymbolSet ss = GetTestSet();
            ss.Add(new Symbol("z!", FeatureMatrix.Empty));

            var list = ss.Pronounce("az!b");

            Assert.AreEqual(3, list.Count);
            Assert.AreSame(ss.BaseSymbols["a"].FeatureMatrix, list[0]);
            Assert.AreSame(ss.BaseSymbols["z!"].FeatureMatrix, list[1]);
            Assert.AreSame(ss.BaseSymbols["b"].FeatureMatrix, list[2]);
        }

        [Test]
        public void PronounceThree()
        {
            SymbolSet ss = GetTestSet();
            ss.Add(new Symbol("z!", FeatureMatrix.Empty));

            var list = ss.Pronounce("z!ab");

            Assert.AreEqual(3, list.Count);
            Assert.AreSame(ss.BaseSymbols["z!"].FeatureMatrix, list[0]);
            Assert.AreSame(ss.BaseSymbols["a"].FeatureMatrix, list[1]);
            Assert.AreSame(ss.BaseSymbols["b"].FeatureMatrix, list[2]);
        }

        [Test]
        [ExpectedException(typeof(SpellingException))]
        public void PronounceException()
        {
            SymbolSet ss = new SymbolSet();
            ss.Add(SymbolTest.SymbolA);
            var list = ss.Pronounce("z");
            Assert.Fail("Shouldn't have gotten " + list);
        }

        [Test]
        public void SymbolDefined()
        {
            int calledDefined = 0;
            Symbol gotSymbol = null;

            var ss = new SymbolSet();
            var ts = new Symbol("test", FeatureMatrixTest.MatrixA);

            ss.SymbolDefined += s => { calledDefined++; gotSymbol = s; };

            ss.Add(ts);

            Assert.AreEqual(1, calledDefined);
            Assert.AreSame(ts, gotSymbol);
        }

        [Test]
        public void SymbolRedefined()
        {
            int calledRedefined = 0;
            Symbol newSymbol = null;
            Symbol oldSymbol = null;

            var ss = new SymbolSet();
            var os = new Symbol("test", FeatureMatrixTest.MatrixA);
            var ns = new Symbol("test", FeatureMatrixTest.MatrixB);

            ss.SymbolRedefined += (old, newer) => { calledRedefined++; oldSymbol = old; newSymbol = newer; };

            ss.Add(os);
            ss.Add(ns);

            Assert.AreEqual(1, calledRedefined);
            Assert.AreSame(os, oldSymbol);
            Assert.AreSame(ns, newSymbol);
        }

        [Test]
        public void SymbolDuplicate()
        {
            int calledDuplicate = 0;
            Symbol newSymbol = null;
            Symbol oldSymbol = null;

            var ss = new SymbolSet();
            var os = new Symbol("y", FeatureMatrixTest.MatrixA);
            var ns = new Symbol("x", FeatureMatrixTest.MatrixA);

            ss.SymbolDuplicate += (old, newer) => { calledDuplicate++; oldSymbol = old; newSymbol = newer; };

            ss.Add(os);
            ss.Add(ns);

            Assert.AreEqual(1, calledDuplicate);
            Assert.AreNotSame(oldSymbol, newSymbol);
            Assert.IsTrue(oldSymbol == os || oldSymbol == ns);
            Assert.IsTrue(newSymbol == os || newSymbol == ns);
        }

        [Test]
        public void PronounceDiacritic()
        {
                SymbolSet ss = GetTestSet();
                ss.AddDiacritic(new Diacritic("+", FeatureMatrix.Empty));

                var list = ss.SplitSymbols("a+bc");

                Assert.AreEqual(3, list.Count);
                Assert.IsTrue(list[0] is CompositeSymbol);

                var compos = list[0] as CompositeSymbol;
                Assert.AreSame(ss.BaseSymbols["a"].FeatureMatrix, compos.BaseSymbol.FeatureMatrix);
                Assert.IsTrue(compos.Diacritics.Contains(ss.Diacritics["+"] as Diacritic));

                Assert.AreEqual(ss.BaseSymbols["a"].FeatureMatrix, list[0].FeatureMatrix);
                Assert.AreSame(ss.BaseSymbols["b"], list[1]);
                Assert.AreSame(ss.BaseSymbols["c"], list[2]);
        }

        [Test]
        public void PronounceDiacriticTwo()
        {
                SymbolSet ss = GetTestSet();
                ss.AddDiacritic(new Diacritic("+", FeatureMatrix.Empty));

                var list = ss.SplitSymbols("abc+");

                Assert.AreEqual(3, list.Count);
                Assert.IsTrue(list[2] is CompositeSymbol);

                var compos = list[2] as CompositeSymbol;
                Assert.AreSame(ss.BaseSymbols["c"].FeatureMatrix, compos.BaseSymbol.FeatureMatrix);
                Assert.IsTrue(compos.Diacritics.Contains(ss.Diacritics["+"] as Diacritic));

                Assert.AreSame(ss.BaseSymbols["a"], list[0]);
                Assert.AreSame(ss.BaseSymbols["b"], list[1]);
                Assert.AreEqual(ss.BaseSymbols["c"].FeatureMatrix, list[2].FeatureMatrix);
        }

        [Test]
        public void SpellDiacritic()
        {
            SymbolSet ss = GetTestSet();
            ss.AddDiacritic(new Diacritic("+", new FeatureMatrix(
                            new FeatureValue[] { FeatureSetTest.GetTestSet().Get<UnaryFeature>("un2").Value })));

            var aPlusFvs = SymbolTest.SymbolA.FeatureMatrix.ToList();
            aPlusFvs.AddRange(ss.Diacritics["+"].FeatureMatrix);
            var aPlusFm = new FeatureMatrix(aPlusFvs);

            Symbol aPlus = ss.Spell(aPlusFm);
            Assert.IsTrue(aPlus is CompositeSymbol);
            Assert.AreEqual("a+", aPlus.Label);
            Assert.AreSame(SymbolTest.SymbolA, (aPlus as CompositeSymbol).BaseSymbol);
            Assert.IsTrue((aPlus as CompositeSymbol).Diacritics.Contains(ss.Diacritics["+"] as Diacritic));
        }

        [Test]
        [ExpectedException(typeof(SpellingException))]
        public void SpellDiacriticException()
        {
            SymbolSet ss = GetTestSet();
            ss.AddDiacritic(new Diacritic("+", new FeatureMatrix(
                            new FeatureValue[] { FeatureSetTest.GetTestSet().Get<UnaryFeature>("un2").Value })));

            var notFoundFvs = SymbolTest.SymbolA.FeatureMatrix.ToList();
            notFoundFvs.Add(FeatureSetTest.GetTestSet().Get<ScalarFeature>("sc2").Value(5));
            var notFoundFm = new FeatureMatrix(notFoundFvs);

            Symbol notFound = ss.Spell(notFoundFm);
            Assert.Fail("Should have thrown SpellingException, got " + notFound);
        }
    }
}
