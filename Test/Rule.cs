using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace Phonix.Test
{
    using NUnit.Framework;

    internal class MockSegment : IRuleSegment
    {
        private bool _isTrue = true;

        public MockSegment()
            : this(true)
        {
        }

        public MockSegment(bool isTrue)
        {
            _isTrue = isTrue;
        }

        public int MatchesCalled
        {
            get; private set;
        }

        public int CombineCalled
        {
            get; private set;
        }

        public bool Matches(RuleContext ctx, SegmentEnumerator segment)
        {
            MatchesCalled++;
            return segment.MoveNext() && _isTrue;
        }

        public void Combine(RuleContext ctx, MutableSegmentEnumerator segment)
        {
            CombineCalled++;
            segment.MoveNext();
        }

        public bool IsMatchOnlySegment { get { return false; } }
        public string MatchString { get { return "~"; } }
        public string CombineString { get { return "~"; } }
    }

    [TestFixture]
    public class RuleTest
    {
        public static Rule TestRule = 
            new Rule("Test", new IRuleSegment[] { new MockSegment() }, new IRuleSegment[] { new MockSegment() });

        [Test]
        public void Ctor()
        {
            var segs = new IRuleSegment[] { new MockSegment() };
            var rule = new Rule("Test", segs, segs);
            
            Assert.AreEqual("Test", rule.Name, "rule name");
            Assert.AreSame(segs[0], rule.Segments.First(), "rule segments");

            try
            {
                rule = new Rule(null, segs, segs);
                Assert.Fail("should have thrown exception");
            }
            catch (ArgumentNullException)
            {
            }

            try
            {
                rule = new Rule("Test", null, segs);
                Assert.Fail("should have thrown exception");
            }
            catch (ArgumentNullException)
            {
            }

            try
            {
                rule = new Rule("Test", segs, null);
                Assert.Fail("should have thrown exception");
            }
            catch (ArgumentNullException)
            {
            }
        }

        [Test]
        public void Apply()
        {
            var segs = new MockSegment[] { new MockSegment(), new MockSegment(), new MockSegment() };
            var exclude = new MockSegment[] { new MockSegment(), new MockSegment(), new MockSegment(false) };
            var rule = new Rule("Test", segs, exclude);

            rule.Apply(WordTest.GetTestWord());

            Assert.AreEqual(3, segs[0].MatchesCalled);
            Assert.AreEqual(3, segs[1].MatchesCalled);
            Assert.AreEqual(2, segs[2].MatchesCalled);

            Assert.AreEqual(1, exclude[0].MatchesCalled);
            Assert.AreEqual(1, exclude[1].MatchesCalled);
            Assert.AreEqual(1, exclude[2].MatchesCalled);

            Assert.AreEqual(1, segs[0].CombineCalled);
            Assert.AreEqual(1, segs[1].CombineCalled);
            Assert.AreEqual(1, segs[2].CombineCalled);

            Assert.AreEqual(0, exclude[0].CombineCalled);
            Assert.AreEqual(0, exclude[1].CombineCalled);
            Assert.AreEqual(0, exclude[2].CombineCalled);
        }

        [Test]
        public void RuleApplication()
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

            rule.Entered += (r, w) => { entered++; ruleEntered = r; wordEntered = w; };
            rule.Exited += (r, w) => { exited++; ruleExited = r; wordExited = w; };
            rule.Applied += (r, w, s) => { applied++; };

            rule.Apply(word);

            Assert.AreEqual(1, entered);
            Assert.AreEqual(1, exited);
            Assert.AreEqual(3, applied);
            Assert.AreSame(rule, ruleEntered);
            Assert.AreSame(rule, ruleExited);
            Assert.AreSame(word, wordEntered);
            Assert.AreSame(word, wordExited);
        }

        [Test]
        public void ApplicationRate()
        {
            Rule rule = new Rule(
                    "test", 
                    new IRuleSegment[] { new ActionSegment(MatrixMatcher.AlwaysMatches, MatrixCombiner.NullCombiner) },
                    new IRuleSegment[] { new ActionSegment(MatrixMatcher.NeverMatches, MatrixCombiner.NullCombiner) }
                    );
            rule.ApplicationRate = 0.5;

            Assert.AreEqual(0.5, rule.ApplicationRate);

            int appliedCount = 0;
            int callCount = 10000;
            rule.Applied += (r, w, s) => appliedCount++;

            var word = WordTest.GetTestWord();
            for (int i = 0; i < callCount; i++)
            {
                rule.Apply(word);
            }

            // assert that the rule actually applied between 49% and 51% of the
            // time that it could have applied
            Assert.IsTrue(appliedCount > (callCount * word.Count() * 0.49), "appliedCount: " + appliedCount);
            Assert.IsTrue(appliedCount < (callCount * word.Count() * 0.51), "appliedCount: " + appliedCount);
        }

        [Test]
        public void Description()
        {
            Rule rule = new Rule(
                    "test", 
                    new IRuleSegment[] { new ActionSegment(MatrixMatcher.AlwaysMatches, MatrixCombiner.NullCombiner) },
                    new IRuleSegment[] { new ActionSegment(MatrixMatcher.NeverMatches, MatrixCombiner.NullCombiner) }
                );

            Console.WriteLine(rule.Description);
            Assert.IsNotNull(rule.Description);
            Assert.AreSame(rule.Description, rule.Description);
        }
    }

}
