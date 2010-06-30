using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace Phonix.Test
{
    using NUnit.Framework;

    [TestFixture]
    public class MutableSegmentEnumeratorTest
    {
        [Test]
        public void InsertBefore()
        {
            var word = WordTest.GetTestWord();
            var iter = word.GetSliceEnumerator(Direction.Rightward);
            iter.MoveNext();

            var slice = iter.Current;
            var sliceIter = slice.GetMutableEnumerator();

            try
            {
                sliceIter.InsertBefore(new MutableSegment(FeatureMatrixTest.MatrixC));
                Assert.Fail("should have thrown exception");
            }
            catch (InvalidOperationException)
            {
            }

            Assert.IsTrue(sliceIter.MoveNext(), "first MoveNext()");
            Assert.AreSame(FeatureMatrixTest.MatrixA, sliceIter.Current.Matrix);
            sliceIter.InsertBefore(new MutableSegment(FeatureMatrixTest.MatrixC));

            Assert.IsTrue(sliceIter.MoveNext(), "second MoveNext()");
            Assert.AreSame(FeatureMatrixTest.MatrixB, sliceIter.Current.Matrix);
            sliceIter.InsertBefore(new MutableSegment(FeatureMatrixTest.MatrixC));

            Assert.IsTrue(sliceIter.MoveNext(), "third MoveNext()");
            Assert.AreSame(FeatureMatrixTest.MatrixC, sliceIter.Current.Matrix);
            sliceIter.InsertBefore(new MutableSegment(FeatureMatrixTest.MatrixC));

            Assert.IsFalse(sliceIter.MoveNext(), "last MoveNext()");
            sliceIter.InsertBefore(new MutableSegment(FeatureMatrixTest.MatrixC));

            iter = word.GetSliceEnumerator(Direction.Rightward);
            iter.MoveNext();
            Assert.AreEqual("cacbccc", WordTest.SpellSlice(iter.Current));
        }

        [Test]
        public void InsertAfter()
        {
            var word = WordTest.GetTestWord();
            var iter = word.GetSliceEnumerator(Direction.Rightward);
            iter.MoveNext();

            var slice = iter.Current;
            var sliceIter = slice.GetMutableEnumerator();

            sliceIter.InsertAfter(new MutableSegment(FeatureMatrixTest.MatrixC));
            Assert.IsTrue(sliceIter.MoveNext(), "zeroeth MoveNext() (inserted before starting)");
            Assert.AreSame(FeatureMatrixTest.MatrixC, sliceIter.Current.Matrix);

            Assert.IsTrue(sliceIter.MoveNext(), "first MoveNext()");
            Assert.AreSame(FeatureMatrixTest.MatrixA, sliceIter.Current.Matrix);
            sliceIter.InsertAfter(new MutableSegment(FeatureMatrixTest.MatrixC));
            Assert.IsTrue(sliceIter.MoveNext(), "second MoveNext()");
            Assert.AreSame(FeatureMatrixTest.MatrixC, sliceIter.Current.Matrix);

            Assert.IsTrue(sliceIter.MoveNext(), "third MoveNext()");
            Assert.AreSame(FeatureMatrixTest.MatrixB, sliceIter.Current.Matrix);
            sliceIter.InsertAfter(new MutableSegment(FeatureMatrixTest.MatrixC));
            Assert.IsTrue(sliceIter.MoveNext(), "fourth MoveNext()");
            Assert.AreSame(FeatureMatrixTest.MatrixC, sliceIter.Current.Matrix);

            Assert.IsTrue(sliceIter.MoveNext(), "fifth MoveNext()");
            Assert.AreSame(FeatureMatrixTest.MatrixC, sliceIter.Current.Matrix);
            sliceIter.InsertAfter(new MutableSegment(FeatureMatrixTest.MatrixC));
            Assert.IsTrue(sliceIter.MoveNext(), "sixth MoveNext()");
            Assert.AreSame(FeatureMatrixTest.MatrixC, sliceIter.Current.Matrix);

            Assert.IsFalse(sliceIter.MoveNext(), "last MoveNext()");

            try
            {
                sliceIter.InsertAfter(new MutableSegment(FeatureMatrixTest.MatrixC));
                Assert.Fail("should have thrown exception");
            }
            catch (InvalidOperationException)
            {
            }

            iter = word.GetSliceEnumerator(Direction.Rightward);
            iter.MoveNext();
            Assert.AreEqual("cacbccc", WordTest.SpellSlice(iter.Current));
        }

        [Test]
        public void Delete()
        {
            var word = WordTest.GetTestWord();
            var iter = word.GetSliceEnumerator(Direction.Rightward);
            iter.MoveNext();

            var slice = iter.Current;
            var sliceIter = slice.GetMutableEnumerator();

            Assert.IsTrue(sliceIter.MoveNext(), "first MoveNext()");
            Assert.AreSame(FeatureMatrixTest.MatrixA, sliceIter.Current.Matrix);
            sliceIter.Delete();

            Assert.IsTrue(sliceIter.MoveNext(), "second MoveNext()");
            Assert.AreSame(FeatureMatrixTest.MatrixB, sliceIter.Current.Matrix);
            sliceIter.Delete();

            Assert.IsTrue(sliceIter.MoveNext(), "third MoveNext()");
            Assert.AreSame(FeatureMatrixTest.MatrixC, sliceIter.Current.Matrix);
            sliceIter.Delete();

            Assert.IsFalse(sliceIter.MoveNext(), "last MoveNext()");

            iter = word.GetSliceEnumerator(Direction.Rightward);
            Assert.IsFalse(iter.MoveNext());
        }
    }
}

