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
    }
}
