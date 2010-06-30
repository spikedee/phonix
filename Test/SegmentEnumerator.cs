using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace Phonix.Test
{
    using NUnit.Framework;

    [TestFixture]
    public class SegmentEnumeratorTest
    {
        [Test]
        public void MoveNextPrev()
        {
            var word = WordTest.GetTestWord();
            var slice = word.GetSliceEnumerator(Direction.Rightward);
            slice.MoveNext();
            var iter = slice.Current.GetEnumerator();

            try
            {
                Assert.Fail("should not be able to access iter.Current" + iter.Current);
            }
            catch (InvalidOperationException)
            {
            }

            // should be able to MoveNext three times
            Assert.IsTrue(iter.MoveNext());
            Assert.AreSame(FeatureMatrixTest.MatrixA, iter.Current.Matrix);
            Assert.IsTrue(iter.MoveNext());
            Assert.AreSame(FeatureMatrixTest.MatrixB, iter.Current.Matrix);
            Assert.IsTrue(iter.MoveNext());
            Assert.AreSame(FeatureMatrixTest.MatrixC, iter.Current.Matrix);
            Assert.IsFalse(iter.MoveNext());
            Assert.IsFalse(iter.MoveNext()); // check that this MoveNext()=false repeats

            try
            {
                Assert.Fail("should not be able to access iter.Current" + iter.Current);
            }
            catch (InvalidOperationException)
            {
            }

            // should be able to MovePrev three times
            Assert.IsTrue(iter.MovePrev());
            Assert.AreSame(FeatureMatrixTest.MatrixC, iter.Current.Matrix);
            Assert.IsTrue(iter.MovePrev());
            Assert.AreSame(FeatureMatrixTest.MatrixB, iter.Current.Matrix);
            Assert.IsTrue(iter.MovePrev());
            Assert.AreSame(FeatureMatrixTest.MatrixA, iter.Current.Matrix);
            Assert.IsFalse(iter.MovePrev());
            Assert.IsFalse(iter.MovePrev()); // check that this MovePrev()=false repeats

            try
            {
                Assert.Fail("should not be able to access iter.Current" + iter.Current);
            }
            catch (InvalidOperationException)
            {
            }

            // should be able to MoveNext again after moving back
            Assert.IsTrue(iter.MoveNext());
            Assert.AreSame(FeatureMatrixTest.MatrixA, iter.Current.Matrix);
            Assert.IsTrue(iter.MoveNext());
            Assert.AreSame(FeatureMatrixTest.MatrixB, iter.Current.Matrix);
            Assert.IsTrue(iter.MoveNext());
            Assert.AreSame(FeatureMatrixTest.MatrixC, iter.Current.Matrix);
            Assert.IsFalse(iter.MoveNext());

        }

        [Test]
        public void Span()
        {
            var word = WordTest.GetTestWord();
            var iter = word.GetSliceEnumerator(Direction.Rightward);
            iter.MoveNext();

            var slice = iter.Current;
            var segmentIter = slice.GetEnumerator();

            // basic case: begin mark is before end mark
            segmentIter.MoveNext(); // should be on A
            var startMark = segmentIter.Mark();

            segmentIter.MoveNext(); // should be on B
            segmentIter.MoveNext(); // should be on C
            var endMark = segmentIter.Mark();

            Assert.AreNotSame(startMark, endMark);

            var matrixList = new List<Segment>(segmentIter.Span(startMark, endMark)).ConvertAll(seg => seg.Matrix);
            Assert.IsTrue(matrixList.SequenceEqual(new FeatureMatrix[] { FeatureMatrixTest.MatrixB, FeatureMatrixTest.MatrixC }));
        }

        [Test]
        public void SpanToEnd()
        {
            var word = WordTest.GetTestWord();
            var iter = word.GetSliceEnumerator(Direction.Rightward);
            iter.MoveNext();

            var slice = iter.Current;
            var segmentIter = slice.GetEnumerator();

            // end mark is at end of iteration
            segmentIter.MoveNext(); // should be on A
            var startMark = segmentIter.Mark();

            while (segmentIter.MoveNext()) {} // iterate to end
            var endMark = segmentIter.Mark();

            Assert.AreNotSame(startMark, endMark);

            var matrixList = new List<Segment>(segmentIter.Span(startMark, endMark)).ConvertAll(seg => seg.Matrix);
            Assert.IsTrue(matrixList.SequenceEqual(new FeatureMatrix[] { FeatureMatrixTest.MatrixB, FeatureMatrixTest.MatrixC }));
        }

        [Test]
        public void SpanFromStart()
        {
            var word = WordTest.GetTestWord();
            var iter = word.GetSliceEnumerator(Direction.Rightward);
            iter.MoveNext();

            var slice = iter.Current;
            var segmentIter = slice.GetEnumerator();

            // begin mark is at beginning of iteration
            var startMark = segmentIter.Mark();

            segmentIter.MoveNext(); // should be on A
            segmentIter.MoveNext(); // should be on B
            var endMark = segmentIter.Mark();

            Assert.AreNotSame(startMark, endMark);

            var matrixList = new List<Segment>(segmentIter.Span(startMark, endMark)).ConvertAll(seg => seg.Matrix);
            Assert.IsTrue(matrixList.SequenceEqual(new FeatureMatrix[] { FeatureMatrixTest.MatrixA, FeatureMatrixTest.MatrixB }));
        }

        [Test]
        public void SpanEmpty()
        {
            var word = WordTest.GetTestWord();
            var iter = word.GetSliceEnumerator(Direction.Rightward);
            iter.MoveNext();

            var slice = iter.Current;
            var segmentIter = slice.GetEnumerator();

            // begin mark and end mark are at same position
            segmentIter.MoveNext(); // should be on A
            var startMark = segmentIter.Mark();
            var endMark = segmentIter.Mark();

            Assert.AreNotSame(startMark, endMark);
            Assert.AreEqual(0, segmentIter.Span(startMark, endMark).Count());
        }

        [Test]
        public void SpanInvalid()
        {
            var word = WordTest.GetTestWord();
            var iter = word.GetSliceEnumerator(Direction.Rightward);
            iter.MoveNext();

            var slice = iter.Current;
            var segmentIter = slice.GetEnumerator();

            // end mark is before start mark
            segmentIter.MoveNext(); // should be on A
            var endMark = segmentIter.Mark();

            segmentIter.MoveNext(); // should be on B
            segmentIter.MoveNext(); // should be on C
            var startMark = segmentIter.Mark();

            Assert.AreNotSame(startMark, endMark);

            try
            {
                segmentIter.Span(startMark, endMark);
                Assert.Fail("Should have thrown exception");
            }
            catch (ArgumentOutOfRangeException)
            {
            }
        }
    }
}
