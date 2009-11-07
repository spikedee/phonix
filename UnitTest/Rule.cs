using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace Phonix.UnitTest
{
    using NUnit.Framework;

    [TestFixture]
    public class RuleSegmentTest
    {
        private void VerifyMatches(
                IRuleSegment seg, 
                bool firstMatch, FeatureMatrix firstPos,
                bool secondMatch, FeatureMatrix secondPos,
                bool thirdMatch, FeatureMatrix thirdPos,
                bool fourthMatch
            )
        {
            var word = WordTest.GetTestWord();
            var slice = word.GetSliceEnumerator(Direction.Rightward);
            slice.MoveNext();

            SegmentEnumerator iter = slice.Current.GetEnumerator();

            Assert.AreEqual(firstMatch, seg.Matches(iter), "first match");
            if (firstPos != null)
                Assert.AreSame(firstPos, iter.Current, "position after first match");

            Assert.AreEqual(secondMatch, seg.Matches(iter), "second match");
            if (secondPos != null)
                Assert.AreSame(secondPos, iter.Current, "position after second match");

            Assert.AreEqual(thirdMatch, seg.Matches(iter), "third match");
            if (thirdPos != null)
                Assert.AreSame(thirdPos, iter.Current, "position after third match");

            Assert.AreEqual(fourthMatch, seg.Matches(iter), "fourth match");
        }

        private void VerifyCombine(
                IRuleSegment seg, 
                FeatureMatrix firstPos,
                FeatureMatrix secondPos,
                FeatureMatrix thirdPos,
                string finalSlice
            )
        {
            var word = WordTest.GetTestWord();
            var slice = word.GetSliceEnumerator(Direction.Rightward);
            slice.MoveNext();

            MutableSegmentEnumerator iter = slice.Current.GetMutableEnumerator();

            seg.Combine(iter);
            if (firstPos != null)
                Assert.AreSame(firstPos, iter.Current, "position after first combo");

            seg.Combine(iter);
            if (secondPos != null)
                Assert.AreSame(secondPos, iter.Current, "position after second combo");

            seg.Combine(iter);
            if (thirdPos != null)
                Assert.AreSame(thirdPos, iter.Current, "position after third combo");

            // get a new slice rather than reusing the old one
            slice = word.GetSliceEnumerator(Direction.Rightward);
            if (slice.MoveNext())
            {
                Assert.AreEqual(finalSlice, WordTest.SpellSlice(slice.Current), "resulting string");
            }
            else
            {
                Assert.AreEqual("", finalSlice);
            }
        }

        [Test]
        public void FeatureMatrixMatches()
        {
            var match = new MatrixMatcher(FeatureMatrixTest.MatrixA);
            var combo = SymbolTest.SymbolC;
            var seg = new FeatureMatrixSegment(match, combo);

            VerifyMatches(
                    seg, 
                    true, FeatureMatrixTest.MatrixA, 
                    false, FeatureMatrixTest.MatrixB, 
                    false, FeatureMatrixTest.MatrixC,
                    false
                    );
        }

        [Test]
        public void FeatureMatrixCombine()
        {
            var match = new MatrixMatcher(FeatureMatrixTest.MatrixA);
            var combo = SymbolTest.SymbolC;
            var seg = new FeatureMatrixSegment(match, combo);

            VerifyCombine(
                    seg, 
                    SymbolTest.SymbolC.FeatureMatrix,
                    SymbolTest.SymbolC.FeatureMatrix,
                    SymbolTest.SymbolC.FeatureMatrix,
                    "ccc"
                    );
        }

        [Test]
        public void DeletingSegmentMatches()
        {
            var match = new MatrixMatcher(FeatureMatrixTest.MatrixA);
            var seg = new DeletingSegment(match);

            VerifyMatches(
                    seg, 
                    true, FeatureMatrixTest.MatrixA, 
                    false, FeatureMatrixTest.MatrixB, 
                    false, FeatureMatrixTest.MatrixC,
                    false
                    );
        }

        [Test]
        public void DeletingSegmentCombine()
        {
            var match = new MatrixMatcher(FeatureMatrixTest.MatrixA);
            var seg = new DeletingSegment(match);

            VerifyCombine(
                    seg, 
                    null,
                    null,
                    null,
                    ""
                    );
        }

        [Test]
        public void InsertingSegmentMatches()
        {
            var combo = SymbolTest.SymbolC;
            var seg = new InsertingSegment(combo);

            VerifyMatches(
                    seg, 
                    true, null,
                    true, null,
                    true, null,
                    true
                    );
        }

        [Test]
        public void InsertingSegmentCombine()
        {
            var combo = SymbolTest.SymbolC;
            var seg = new InsertingSegment(combo);

            VerifyCombine(
                    seg, 
                    FeatureMatrixTest.MatrixC, 
                    FeatureMatrixTest.MatrixC, 
                    FeatureMatrixTest.MatrixC, 
                    "cccabc"
                    );
        }

    }

    public class MockSegment : IRuleSegment
    {
        public int MatchesCalled
        {
            get; private set;
        }

        public int CombineCalled
        {
            get; private set;
        }

        public bool Matches(SegmentEnumerator segment)
        {
            MatchesCalled++;
            return segment.MoveNext();
        }

        public void Combine(MutableSegmentEnumerator segment)
        {
            CombineCalled++;
            segment.MoveNext();
        }
    }

    [TestFixture]
    public class RuleTest
    {
        public static Rule TestRule = new Rule("Test", new IRuleSegment[] { new MockSegment() });

        [Test]
        public void Ctor()
        {
            var segs = new IRuleSegment[] { new MockSegment() };
            var rule = new Rule("Test", segs);
            
            Assert.AreEqual("Test", rule.Name, "rule name");
            Assert.AreSame(segs, rule.Segments, "rule segments");

            try
            {
                rule = new Rule(null, segs);
                Assert.Fail("should have thrown exception");
            }
            catch (ArgumentNullException)
            {
            }

            try
            {
                rule = new Rule("Test", null);
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
            var rule = new Rule("Test", segs);

            rule.Apply(WordTest.GetTestWord());

            Assert.AreEqual(3, segs[0].MatchesCalled);
            Assert.AreEqual(3, segs[1].MatchesCalled);
            Assert.AreEqual(2, segs[2].MatchesCalled);

            Assert.AreEqual(1, segs[0].CombineCalled);
            Assert.AreEqual(1, segs[1].CombineCalled);
            Assert.AreEqual(1, segs[2].CombineCalled);
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
