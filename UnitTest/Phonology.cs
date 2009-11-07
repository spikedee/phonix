using System;
using System.Collections.Generic;
using System.Linq;
using Phonix;

namespace Phonix.UnitTest
{
    using NUnit.Framework;

    [TestFixture]
    public class PhonologyTest
    {
        [Test]
        public void Ctor()
        {
            var phono = new Phonology();

            Assert.IsNotNull(phono.FeatureSet);
            Assert.IsNotNull(phono.SymbolSet);
            Assert.IsNotNull(phono.RuleSet);
        }

        [Test]
        public void Ctor2()
        {
            var fs = FeatureSetTest.GetTestSet();
            var ss = SymbolSetTest.GetTestSet();
            var rs = RuleSetTest.GetTestSet();
            var phono = new Phonology(fs, ss, rs);

            Assert.AreSame(fs, phono.FeatureSet);
            Assert.AreSame(ss, phono.SymbolSet);
            Assert.AreSame(rs, phono.RuleSet);
        }

        [Test]
        public void MergeWithEmpty()
        {
            var phono = new Phonology(
                    FeatureSetTest.GetTestSet(),
                    SymbolSetTest.GetTestSet(),
                    RuleSetTest.GetTestSet()
                    );

            var empty = new Phonology();

            empty.Merge(phono);

            AssertEquivalent(phono, empty);
        }

        [Test]
        public void MergeWithFull()
        {
            var phono = new Phonology(
                    FeatureSetTest.GetTestSet(),
                    SymbolSetTest.GetTestSet(),
                    RuleSetTest.GetTestSet()
                    );
            var phono2 = new Phonology(
                    FeatureSetTest.GetTestSet(),
                    SymbolSetTest.GetTestSet(),
                    RuleSetTest.GetTestSet()
                    );

            var empty = new Phonology();

            phono.Merge(empty);

            AssertEquivalent(phono, phono2);
        }

        private void AssertEquivalent(Phonology a, Phonology b)
        {
            Assert.AreNotSame(a.FeatureSet, b.FeatureSet);
            foreach (var f in a.FeatureSet)
            {
                Assert.AreSame(f, b.FeatureSet.Get<Feature>(f.Name));
            }

            Assert.AreNotSame(a.SymbolSet, b.SymbolSet);
            foreach (var s in a.SymbolSet)
            {
                Assert.AreSame(s.Value, b.SymbolSet[s.Key]);
            }

            Assert.AreNotSame(a.RuleSet, b.RuleSet);
            foreach (var r in a.RuleSet)
            {
                Assert.IsTrue(b.RuleSet.Contains(r));
            }
        }
    }
}
