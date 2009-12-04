using System;
using System.Collections.Generic;
using System.Linq;
using Phonix;

namespace Phonix.UnitTest
{
    using NUnit.Framework;

    [TestFixture]
    public class TraceTest
    {
        [Test]
        public void FeatureDefined()
        {
            int calledDefined = 0;
            Feature gotFeature = null;

            Trace.OnFeatureDefined += f => { calledDefined++; gotFeature = f; };

            var fs = new FeatureSet();
            var bf = new BinaryFeature("test", "alt1", "alt2");
            fs.Add(bf);

            Assert.AreEqual(1, calledDefined);
            Assert.AreSame(bf, gotFeature);
        }

        [Test]
        public void FeatureRedefined()
        {
            int calledRedefined = 0;
            Feature gotFeature = null;
            bool gotTest = false;

            var fs = new FeatureSet();
            var oldTest = new UnaryFeature("test");
            var bf = new BinaryFeature("test");
            var bf2 = new BinaryFeature("test");

            Trace.OnFeatureRedefined += (old, newer) => 
            { 
                calledRedefined++; 
                gotFeature = newer; 
                if (old == oldTest)
                    gotTest = true;
            };

            fs.Add(oldTest);
            fs.Add(bf);

            Assert.AreEqual(1, calledRedefined);
            Assert.IsTrue(gotTest);
            Assert.AreSame(bf, gotFeature);

            // check that adding an identical feature doesn't result in overwrite
            fs.Add(bf2);

            Assert.AreEqual(1, calledRedefined);
            Assert.AreSame(fs.Get<Feature>(bf.Name), bf);
        }

        [Test]
        public void SymbolDefined()
        {
            int calledDefined = 0;
            Symbol gotSymbol = null;

            Trace.OnSymbolDefined += s => { calledDefined++; gotSymbol = s; };

            var ss = new SymbolSet();
            var ts = new Symbol("test", FeatureMatrixTest.MatrixA);
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

            Trace.OnSymbolRedefined += (old, newer) => { calledRedefined++; oldSymbol = old; newSymbol = newer; };

            var ss = new SymbolSet();
            var os = new Symbol("test", FeatureMatrixTest.MatrixA);
            var ns = new Symbol("test", FeatureMatrixTest.MatrixB);
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

            Trace.OnSymbolDuplicate += (old, newer) => { calledDuplicate++; oldSymbol = old; newSymbol = newer; };

            var ss = new SymbolSet();
            var os = new Symbol("y", FeatureMatrixTest.MatrixA);
            var ns = new Symbol("x", FeatureMatrixTest.MatrixA);
            ss.Add(os);
            ss.Add(ns);

            Assert.AreEqual(1, calledDuplicate);
            Assert.AreNotSame(oldSymbol, newSymbol);
            Assert.IsTrue(oldSymbol == os || oldSymbol == ns);
            Assert.IsTrue(newSymbol == os || newSymbol == ns);
        }

        [Test]
        public void RuleDefined()
        {
            int calledDefined = 0;
            Rule gotRule = null;

            Trace.OnRuleDefined += r => { calledDefined++; gotRule = r; };

            var rs = new RuleSet();
            rs.Add(RuleTest.TestRule);

            Assert.AreEqual(1, calledDefined);
            Assert.AreSame(RuleTest.TestRule, gotRule);
        }

        [Test]
        public void RuleRedefined()
        {
            int calledRedefined = 0;
            Rule newRule = null;
            Rule oldRule = null;

            Trace.OnRuleRedefined += (old, newer) => { calledRedefined++; oldRule = old; newRule = newer; };

            var rs = new RuleSet();
            var or = new Rule("test", new IRuleSegment[] {}, new IRuleSegment[] {});
            var nr = new Rule("test", new IRuleSegment[] {}, new IRuleSegment[] {});
            rs.Add(or);
            rs.Add(nr);

            Assert.AreEqual(1, calledRedefined);
            Assert.AreSame(or, oldRule);
            Assert.AreSame(nr, newRule);
        }

        [Test]
        public void RuleApplication()
        {
            int entered = 0;
            int exited = 0;
            int applied = 0;
            Rule ruleEntered = null;
            Rule ruleExited = null;
            Word wordEntered = null;
            Word wordExited = null;

            Trace.OnRuleEntered += (r, w) => { entered++; ruleEntered = r; wordEntered = w; };
            Trace.OnRuleExited += (r, w) => { exited++; ruleExited = r; wordExited = w; };
            Trace.OnRuleApplied += (r, w, s) => { applied++; };

            Rule rule = new Rule(
                    "test", 
                    new IRuleSegment[] { new FeatureMatrixSegment(MatrixMatcher.AlwaysMatches, MatrixCombiner.NullCombiner) },
                    new IRuleSegment[] { new FeatureMatrixSegment(MatrixMatcher.NeverMatches, MatrixCombiner.NullCombiner) }
                    );
            Word word = WordTest.GetTestWord();

            rule.Apply(word);

            Assert.AreEqual(1, entered);
            Assert.AreEqual(1, exited);
            Assert.AreEqual(3, applied);
            Assert.AreSame(rule, ruleEntered);
            Assert.AreSame(rule, ruleExited);
            Assert.AreSame(word, wordEntered);
            Assert.AreSame(word, wordExited);
        }
    }
}
