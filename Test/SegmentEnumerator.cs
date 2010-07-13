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
    }
}
