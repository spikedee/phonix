using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace Phonix.UnitTest
{
    using NUnit.Framework;

    public class MockSegment : IRuleSegment
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
            Assert.AreSame(segs, rule.Segments, "rule segments");

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
    }

    [TestFixture]
    public class RuleSetTest
    {
        // EMPTY for now
        
        public static RuleSet GetTestSet()
        {
            var rs = new RuleSet();
            rs.Add(RuleTest.TestRule);
            return rs;
        }
    }
}
