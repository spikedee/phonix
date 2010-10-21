using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace Phonix.Test
{
    using NUnit.Framework;

    [TestFixture]
    public class RuleSetTest
    {
        public static RuleSet GetTestSet()
        {
            var rs = new RuleSet();
            rs.Add(RuleTest.TestRule);
            return rs;
        }

        [Test]
        public void Add()
        {
            var rs = new RuleSet();
            var rule1 = new Rule("rule1", new IRuleSegment[] { new MockSegment() }, new IRuleSegment[] { new MockSegment() });
            var rule2 = new Rule("rule2", new IRuleSegment[] { new MockSegment() }, new IRuleSegment[] { new MockSegment() });

            rs.Add(rule1);
            Assert.AreEqual(1, rs.OrderedRules.Count());
            Assert.IsTrue(rs.OrderedRules.Contains(rule1));
            Assert.IsFalse(rs.OrderedRules.Contains(rule2));

            rs.Add(rule2);
            Assert.AreEqual(2, rs.OrderedRules.Count());
            Assert.IsTrue(rs.OrderedRules.Contains(rule1));
            Assert.IsTrue(rs.OrderedRules.Contains(rule2));

            Assert.AreEqual(0, rs.PersistentRules.Count());
        }

        [Test]
        public void AddPersistent()
        {
            var rs = new RuleSet();
            var rule1 = new Rule("rule1", new IRuleSegment[] { new MockSegment() }, new IRuleSegment[] { new MockSegment() });
            var rule2 = new Rule("rule2", new IRuleSegment[] { new MockSegment() }, new IRuleSegment[] { new MockSegment() });

            rs.AddPersistent(rule1);
            Assert.AreEqual(1, rs.PersistentRules.Count());
            Assert.IsTrue(rs.PersistentRules.Contains(rule1));
            Assert.IsFalse(rs.PersistentRules.Contains(rule2));

            rs.AddPersistent(rule2);
            Assert.AreEqual(2, rs.PersistentRules.Count());
            Assert.IsTrue(rs.PersistentRules.Contains(rule1));
            Assert.IsTrue(rs.PersistentRules.Contains(rule2));

            Assert.AreEqual(0, rs.OrderedRules.Count());
        }

        [Test]
        public void RuleDefined()
        {
            int calledDefined = 0;
            AbstractRule gotRule = null;
            var rs = new RuleSet();

            rs.RuleDefined += r => { calledDefined++; gotRule = r; };

            rs.Add(RuleTest.TestRule);

            Assert.AreEqual(1, calledDefined);
            Assert.AreSame(RuleTest.TestRule, gotRule);
        }

        [Test]
        public void RuleRedefined()
        {
            int calledRedefined = 0;
            AbstractRule newRule = null;
            AbstractRule oldRule = null;
            var rs = new RuleSet();

            rs.RuleRedefined += (old, newer) => { calledRedefined++; oldRule = old; newRule = newer; };

            var or = new Rule("test", new IRuleSegment[] {}, new IRuleSegment[] {});
            var nr = new Rule("test", new IRuleSegment[] {}, new IRuleSegment[] {});
            rs.Add(or);
            rs.Add(nr);

            Assert.AreEqual(1, calledRedefined);
            Assert.AreSame(or, oldRule);
            Assert.AreSame(nr, newRule);
        }

        [Test]
        public void SyllableRuleRedefined()
        {
            int calledRedefined = 0;
            var rs = new RuleSet();
            var syll = new SyllableBuilder();
            syll.Nuclei.Add(new IMatrixMatcher[] { new MatrixMatcher(FeatureMatrixTest.MatrixA) });

            rs.RuleRedefined += (old, newer) => { calledRedefined++; };

            rs.Add(syll.GetSyllableRule());
            rs.Add(syll.GetSyllableRule());

            Assert.AreEqual(0, calledRedefined);
        }

        [Test]
        public void RuleApplied()
        {
            int entered = 0;
            int exited = 0;
            int applied = 0;
            AbstractRule ruleEntered = null;
            AbstractRule ruleExited = null;
            Word wordEntered = null;
            Word wordExited = null;
            Rule rule = new Rule(
                    "test", 
                    new IRuleSegment[] { new ActionSegment(MatrixMatcher.AlwaysMatches, MatrixCombiner.NullCombiner) },
                    new IRuleSegment[] { new ActionSegment(MatrixMatcher.NeverMatches, MatrixCombiner.NullCombiner) }
                    );
            Word word = WordTest.GetTestWord();
            RuleSet rs = new RuleSet();

            rs.RuleEntered += (r, w) => { entered++; ruleEntered = r; wordEntered = w; };
            rs.RuleExited += (r, w) => { exited++; ruleExited = r; wordExited = w; };
            rs.RuleApplied += (r, w, s) => { applied++; };

            rs.Add(rule);
            rs.ApplyAll(word);

            Assert.AreEqual(1, entered);
            Assert.AreEqual(1, exited);
            Assert.AreEqual(3, applied);
            Assert.AreSame(rule, ruleEntered);
            Assert.AreSame(rule, ruleExited);
            Assert.AreSame(word, wordEntered);
            Assert.AreSame(word, wordExited);
        }

        [Test]
        public void RuleAppliedUndefinedVariable()
        {
            int undefUsed = 0;
            var fs = FeatureSetTest.GetTestSet();
            Rule ruleInUndef = null;
            IFeatureValue varInUndef = null;
            var combo = new MatrixCombiner(new ICombinable[] { fs.Get<Feature>("un").VariableValue });

            Rule rule = new Rule(
                    "test", 
                    new IRuleSegment[] { new ActionSegment(MatrixMatcher.AlwaysMatches, combo) },
                    new IRuleSegment[] { new ActionSegment(MatrixMatcher.NeverMatches, MatrixCombiner.NullCombiner) }
                    );
            Word word = WordTest.GetTestWord();
            RuleSet rs = new RuleSet();

            rs.UndefinedVariableUsed += (r, v) => { undefUsed++; ruleInUndef = r; varInUndef = v; };

            rs.Add(rule);
            rs.ApplyAll(word);

            Assert.AreEqual(word.Count(), undefUsed);
            Assert.AreSame(rule, ruleInUndef);
            Assert.AreSame(fs.Get<Feature>("un").VariableValue, varInUndef);
        }

        [Test]
        public void PersistentRule()
        {
            int ruleCount = 0;
            int persistentCount = 0;

            Rule rule = new Rule(
                    "test", 
                    new IRuleSegment[] { new ActionSegment(MatrixMatcher.AlwaysMatches, MatrixCombiner.NullCombiner) },
                    new IRuleSegment[] { new ActionSegment(MatrixMatcher.NeverMatches, MatrixCombiner.NullCombiner) }
                    );
            Rule persistent = new Rule(
                    "test", 
                    new IRuleSegment[] { new ActionSegment(MatrixMatcher.AlwaysMatches, MatrixCombiner.NullCombiner) },
                    new IRuleSegment[] { new ActionSegment(MatrixMatcher.NeverMatches, MatrixCombiner.NullCombiner) }
                    );

            rule.Entered += (r, w) => { ruleCount++; };
            persistent.Entered += (r, w) => { persistentCount++; };

            Word word = WordTest.GetTestWord();
            RuleSet rs = new RuleSet();
            rs.Add(rule);
            rs.AddPersistent(persistent);

            rs.ApplyAll(word);

            Assert.AreEqual(4, persistentCount);
            Assert.AreEqual(1, ruleCount);
        }
    }
}
