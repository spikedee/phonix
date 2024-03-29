using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace Phonix.Test
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
            var slice = word.Slice(Direction.Rightward).GetEnumerator();
            slice.MoveNext();

            SegmentEnumerator iter = slice.Current.GetEnumerator();
            RuleContext ctx = new RuleContext();

            Assert.AreEqual(firstMatch, seg.Matches(ctx, iter), "first match");
            if (firstPos != null)
                Assert.AreSame(firstPos, iter.Current.Matrix, "position after first match");

            Assert.AreEqual(secondMatch, seg.Matches(ctx, iter), "second match");
            if (secondPos != null)
                Assert.AreSame(secondPos, iter.Current.Matrix, "position after second match");

            Assert.AreEqual(thirdMatch, seg.Matches(ctx, iter), "third match");
            if (thirdPos != null)
                Assert.AreSame(thirdPos, iter.Current.Matrix, "position after third match");

            Assert.AreEqual(fourthMatch, seg.Matches(ctx, iter), "fourth match");
        }

        private void VerifyCombine(
                IRuleSegment seg, 
                FeatureMatrix firstPos,
                FeatureMatrix secondPos,
                FeatureMatrix thirdPos,
                string finalSlice
            )
        {
            VerifyCombine(new RuleContext(), seg, firstPos, secondPos, thirdPos, finalSlice);
        }

        private void VerifyCombine(
                RuleContext ctx,
                IRuleSegment seg, 
                FeatureMatrix firstPos,
                FeatureMatrix secondPos,
                FeatureMatrix thirdPos,
                string finalSlice
            )
        {
            var word = WordTest.GetTestWord();
            var slice = word.Slice(Direction.Rightward).GetEnumerator();
            slice.MoveNext();

            MutableSegmentEnumerator iter = slice.Current.GetMutableEnumerator();

            seg.Combine(ctx, iter);
            if (firstPos != null)
            {
                Assert.IsTrue(firstPos.Equals(iter.Current.Matrix), "position after first combo");
            }

            seg.Combine(ctx, iter);
            if (secondPos != null)
            {
                Assert.IsTrue(secondPos.Equals(iter.Current.Matrix), "position after second combo");
            }

            seg.Combine(ctx, iter);
            if (thirdPos != null)
            {
                Assert.IsTrue(thirdPos.Equals(iter.Current.Matrix), "position after third combo");
            }

            // get a new slice rather than reusing the old one
            slice = word.Slice(Direction.Rightward).GetEnumerator();
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
        public void ActionMatches()
        {
            var match = new MatrixMatcher(FeatureMatrixTest.MatrixA);
            var combo = SymbolTest.SymbolC;
            var seg = new ActionSegment(match, combo);

            VerifyMatches(
                    seg, 
                    true, FeatureMatrixTest.MatrixA, 
                    false, FeatureMatrixTest.MatrixB, 
                    false, FeatureMatrixTest.MatrixC,
                    false
                    );
        }

        [Test]
        public void ActionCombine()
        {
            var match = new MatrixMatcher(FeatureMatrixTest.MatrixA);
            var combo = SymbolTest.SymbolC;
            var seg = new ActionSegment(match, combo);

            VerifyCombine(
                    seg, 
                    SymbolTest.SymbolC.FeatureMatrix,
                    SymbolTest.SymbolC.FeatureMatrix,
                    SymbolTest.SymbolC.FeatureMatrix,
                    "ccc"
                    );
        }

        [Test]
        public void VariableMatrixMatches()
        {
            var fs = FeatureSetTest.GetTestSet();
            var match = new MatrixMatcher(new IMatchable[] { 
                    fs.Get<Feature>("un").VariableValue,
                    fs.Get<Feature>("sc").VariableValue
                    });
            var seg = new ContextSegment(match);

            VerifyMatches(
                    seg, 
                    true, FeatureMatrixTest.MatrixA, 
                    false, FeatureMatrixTest.MatrixB, 
                    true, FeatureMatrixTest.MatrixC,
                    false
                    );
        }

        [Test]
        public void VariableMatrixCombine()
        {
            var fs = FeatureSetTest.GetTestSet();
            var un = fs.Get<UnaryFeature>("un");
            var sc = fs.Get<ScalarFeature>("sc");
            var combo = new MatrixCombiner(new ICombinable[] { un.VariableValue, sc.VariableValue });
            var seg = new ActionSegment(MatrixMatcher.AlwaysMatches, combo);

            var expectedSecond = new FeatureMatrix(new FeatureValue[] {
                        un.Value,
                        fs.Get<BinaryFeature>("bn").PlusValue,
                        sc.Value(1),
                        });

            var ctx = new RuleContext();
            ctx.VariableFeatures.Add(un, un.Value);
            ctx.VariableFeatures.Add(sc, sc.Value(1));

            VerifyCombine(
                    ctx,
                    seg, 
                    FeatureMatrixTest.MatrixA,
                    expectedSecond,
                    FeatureMatrixTest.MatrixC,
                    "a[+bn sc=1 un]c"
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

        [Test]
        public void StepSegmentMatch()
        {
            var step = new StepSegment();

            VerifyMatches(
                    step,
                    true, FeatureMatrixTest.MatrixA,
                    true, FeatureMatrixTest.MatrixB,
                    true, FeatureMatrixTest.MatrixC,
                    false 
                    );
        }

        [Test]
        public void BackstepSegmentMatch()
        {
            var step = new BackstepSegment();
            var word = WordTest.GetTestWord();
            var slice = word.Slice(Direction.Rightward).GetEnumerator();

            Assert.IsTrue(slice.MoveNext());
            Assert.IsTrue(slice.MoveNext());
            Assert.IsTrue(slice.MoveNext());
            var iter = slice.Current.GetEnumerator();
            var ctx = new RuleContext();

            Assert.IsTrue(step.Matches(ctx, iter));
            Assert.AreSame(FeatureMatrixTest.MatrixA, iter.Current.Matrix);
            Assert.IsTrue(step.Matches(ctx, iter));

            try
            {
                Assert.Fail("should throw an exception accessing " + iter.Current);
            }
            catch (InvalidOperationException)
            {
            }

            Assert.IsFalse(step.Matches(ctx, iter));

            try
            {
                Assert.Fail("should throw an exception accessing " + iter.Current);
            }
            catch (InvalidOperationException)
            {
            }
        }

        [Test]
        public void MultiSegmentMatchZeroOrOne()
        {
            var match = MatrixMatcher.AlwaysMatches;
            var seg = new MultiSegment(new IRuleSegment[] { new ContextSegment(match) }, 0, 1);

            VerifyMatches(
                    seg, 
                    true, FeatureMatrixTest.MatrixA, 
                    true, FeatureMatrixTest.MatrixB, 
                    true, FeatureMatrixTest.MatrixC,
                    true
                    );
        }

        [Test]
        public void MultiSegmentMatchZeroOrMore()
        {
            var match = MatrixMatcher.AlwaysMatches;
            var seg = new MultiSegment(new IRuleSegment[] { new ContextSegment(match) }, 0, null);

            VerifyMatches(
                    seg, 
                    true, null,
                    true, null,
                    true, null,
                    true
                    );
        }

        [Test]
        public void MultiSegmentMatchOneOrMore()
        {
            var match = MatrixMatcher.AlwaysMatches;
            var seg = new MultiSegment(new IRuleSegment[] { new ContextSegment(match) }, 1, null);

            VerifyMatches(
                    seg, 
                    true, null,
                    false, null,
                    false, null,
                    false
                    );
        }

        [Test]
        public void MultiSegmentMatchOneOrTwo()
        {
            var match = MatrixMatcher.AlwaysMatches;
            var seg = new MultiSegment(new IRuleSegment[] { new ContextSegment(match) }, 1, 2);

            VerifyMatches(
                    seg, 
                    true, FeatureMatrixTest.MatrixB,
                    true, FeatureMatrixTest.MatrixC,
                    false, null,
                    false
                    );
        }
    }
}
