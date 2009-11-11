using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace Phonix.UnitTest
{
    using NUnit.Framework;

    [TestFixture]
    public class WordTest
    {
        public static Word GetTestWord()
        {
            var fms = new FeatureMatrix[] 
            { 
                FeatureMatrixTest.MatrixA, 
                FeatureMatrixTest.MatrixB, 
                FeatureMatrixTest.MatrixC
            };

            return new Word(fms);
        }

        public static string SpellSlice(IWordSlice slice)
        {
            SymbolSet ss = SymbolSetTest.GetTestSet();
            StringBuilder str = new StringBuilder();
            foreach (FeatureMatrix seg in slice)
            {
                try
                {
                    str.Append(ss.Spell(seg));
                }
                catch (SpellingException)
                {
                    str.Append(seg.ToString());
                }
            }
            return str.ToString();
        }

        [Test]
        public void Ctor()
        {
            var word = GetTestWord();
            Assert.IsNotNull(word);
        }

        [Test]
        public void GetSliceEnumeratorRightward()
        {
            var word = GetTestWord();

            var iter = word.GetSliceEnumerator(Direction.Rightward);
            Assert.IsTrue(iter.MoveNext());
            Assert.AreEqual("abc", SpellSlice(iter.Current));
            
            Assert.IsTrue(iter.MoveNext());
            Assert.AreEqual("bc", SpellSlice(iter.Current));
            
            Assert.IsTrue(iter.MoveNext());
            Assert.AreEqual("c", SpellSlice(iter.Current));

            Assert.IsFalse(iter.MoveNext());
        }

        [Test]
        public void GetSliceEnumeratorLeftward()
        {
            var word = GetTestWord();

            var iter = word.GetSliceEnumerator(Direction.Leftward);
            Assert.IsTrue(iter.MoveNext());
            Assert.AreEqual("c", SpellSlice(iter.Current));
            
            Assert.IsTrue(iter.MoveNext());
            Assert.AreEqual("bc", SpellSlice(iter.Current));
            
            Assert.IsTrue(iter.MoveNext());
            Assert.AreEqual("abc", SpellSlice(iter.Current));

            Assert.IsFalse(iter.MoveNext());
        }

        [Test]
        public void GetFilteredSliceEnumeratorRightward()
        {
            var word = GetTestWord();
            var fs = FeatureSetTest.GetTestSet();
            var fm = new FeatureMatrix(new FeatureValue[] { 
                    fs.Get<ScalarFeature>("sc").Value(1)
            });
            var filter = new MatrixMatcher(fm);

            var iter = word.GetSliceEnumerator(Direction.Rightward, filter);
            Assert.IsTrue(iter.MoveNext());
            Assert.AreEqual("ac", SpellSlice(iter.Current));
            
            Assert.IsTrue(iter.MoveNext());
            Assert.AreEqual("c", SpellSlice(iter.Current));
            
            Assert.IsFalse(iter.MoveNext());
        }

        [Test]
        public void GetFilteredSliceEnumeratorLeftward()
        {
            var word = GetTestWord();
            var fs = FeatureSetTest.GetTestSet();
            var fm = new FeatureMatrix(new FeatureValue[] { 
                    fs.Get<ScalarFeature>("sc").Value(1)
            });
            var filter = new MatrixMatcher(fm);

            var iter = word.GetSliceEnumerator(Direction.Leftward, filter);
            Assert.IsTrue(iter.MoveNext());
            Assert.AreEqual("c", SpellSlice(iter.Current));
            
            Assert.IsTrue(iter.MoveNext());
            Assert.AreEqual("ac", SpellSlice(iter.Current));
            
            Assert.IsFalse(iter.MoveNext());
        }

        [Test]
        public void SegmentIsFirst()
        {
            var word = GetTestWord();
            var iter = word.GetSliceEnumerator(Direction.Rightward);
            iter.MoveNext();

            var slice = iter.Current;
            var sliceIter = slice.GetEnumerator();

            Assert.IsTrue(sliceIter.MoveNext(), "first MoveNext()");
            Assert.IsTrue(sliceIter.IsFirst, "first sliceIter.IsFirst");

            Assert.IsTrue(sliceIter.MoveNext(), "second MoveNext()");
            Assert.IsFalse(sliceIter.IsFirst, "second sliceIter.IsFirst");

            Assert.IsTrue(sliceIter.MoveNext(), "third MoveNext()");
            Assert.IsFalse(sliceIter.IsFirst, "third sliceIter.IsFirst");
        }

        [Test]
        public void SegmentIsLast()
        {
            var word = GetTestWord();
            var iter = word.GetSliceEnumerator(Direction.Rightward);
            iter.MoveNext();

            var slice = iter.Current;
            var sliceIter = slice.GetEnumerator();

            Assert.IsTrue(sliceIter.MoveNext(), "first MoveNext()");
            Assert.IsFalse(sliceIter.IsLast, "first sliceIter.IsLast");

            Assert.IsTrue(sliceIter.MoveNext(), "second MoveNext()");
            Assert.IsFalse(sliceIter.IsLast, "second sliceIter.IsLast");

            Assert.IsTrue(sliceIter.MoveNext(), "third MoveNext()");
            Assert.IsTrue(sliceIter.IsLast, "third sliceIter.IsLast");

            Assert.IsFalse(sliceIter.MoveNext(), "last MoveNext()");
        }

        [Test]
        public void SegmentInsertBefore()
        {
            var word = GetTestWord();
            var iter = word.GetSliceEnumerator(Direction.Rightward);
            iter.MoveNext();

            var slice = iter.Current;
            var sliceIter = slice.GetMutableEnumerator();

            try
            {
                sliceIter.InsertBefore(FeatureMatrixTest.MatrixC);
                Assert.Fail("should have thrown exception");
            }
            catch (InvalidOperationException)
            {
            }

            Assert.IsTrue(sliceIter.MoveNext(), "first MoveNext()");
            Assert.AreSame(FeatureMatrixTest.MatrixA, sliceIter.Current);
            sliceIter.InsertBefore(FeatureMatrixTest.MatrixC);

            Assert.IsTrue(sliceIter.MoveNext(), "second MoveNext()");
            Assert.AreSame(FeatureMatrixTest.MatrixB, sliceIter.Current);
            sliceIter.InsertBefore(FeatureMatrixTest.MatrixC);

            Assert.IsTrue(sliceIter.MoveNext(), "third MoveNext()");
            Assert.AreSame(FeatureMatrixTest.MatrixC, sliceIter.Current);
            sliceIter.InsertBefore(FeatureMatrixTest.MatrixC);

            Assert.IsFalse(sliceIter.MoveNext(), "last MoveNext()");
            sliceIter.InsertBefore(FeatureMatrixTest.MatrixC);

            iter = word.GetSliceEnumerator(Direction.Rightward);
            iter.MoveNext();
            Assert.AreEqual("cacbccc", SpellSlice(iter.Current));
        }

        [Test]
        public void SegmentInsertAfter()
        {
            var word = GetTestWord();
            var iter = word.GetSliceEnumerator(Direction.Rightward);
            iter.MoveNext();

            var slice = iter.Current;
            var sliceIter = slice.GetMutableEnumerator();

            sliceIter.InsertAfter(FeatureMatrixTest.MatrixC);
            Assert.IsTrue(sliceIter.MoveNext(), "zeroeth MoveNext() (inserted before starting)");
            Assert.AreSame(FeatureMatrixTest.MatrixC, sliceIter.Current);

            Assert.IsTrue(sliceIter.MoveNext(), "first MoveNext()");
            Assert.AreSame(FeatureMatrixTest.MatrixA, sliceIter.Current);
            sliceIter.InsertAfter(FeatureMatrixTest.MatrixC);
            Assert.IsTrue(sliceIter.MoveNext(), "second MoveNext()");
            Assert.AreSame(FeatureMatrixTest.MatrixC, sliceIter.Current);

            Assert.IsTrue(sliceIter.MoveNext(), "third MoveNext()");
            Assert.AreSame(FeatureMatrixTest.MatrixB, sliceIter.Current);
            sliceIter.InsertAfter(FeatureMatrixTest.MatrixC);
            Assert.IsTrue(sliceIter.MoveNext(), "fourth MoveNext()");
            Assert.AreSame(FeatureMatrixTest.MatrixC, sliceIter.Current);

            Assert.IsTrue(sliceIter.MoveNext(), "fifth MoveNext()");
            Assert.AreSame(FeatureMatrixTest.MatrixC, sliceIter.Current);
            sliceIter.InsertAfter(FeatureMatrixTest.MatrixC);
            Assert.IsTrue(sliceIter.MoveNext(), "sixth MoveNext()");
            Assert.AreSame(FeatureMatrixTest.MatrixC, sliceIter.Current);

            Assert.IsFalse(sliceIter.MoveNext(), "last MoveNext()");

            try
            {
                sliceIter.InsertAfter(FeatureMatrixTest.MatrixC);
                Assert.Fail("should have thrown exception");
            }
            catch (InvalidOperationException)
            {
            }

            iter = word.GetSliceEnumerator(Direction.Rightward);
            iter.MoveNext();
            Assert.AreEqual("cacbccc", SpellSlice(iter.Current));
        }

        [Test]
        public void SegmentDelete()
        {
            var word = GetTestWord();
            var iter = word.GetSliceEnumerator(Direction.Rightward);
            iter.MoveNext();

            var slice = iter.Current;
            var sliceIter = slice.GetMutableEnumerator();

            Assert.IsTrue(sliceIter.MoveNext(), "first MoveNext()");
            Assert.AreSame(FeatureMatrixTest.MatrixA, sliceIter.Current);
            sliceIter.Delete();

            Assert.IsTrue(sliceIter.MoveNext(), "second MoveNext()");
            Assert.AreSame(FeatureMatrixTest.MatrixB, sliceIter.Current);
            sliceIter.Delete();

            Assert.IsTrue(sliceIter.MoveNext(), "third MoveNext()");
            Assert.AreSame(FeatureMatrixTest.MatrixC, sliceIter.Current);
            sliceIter.Delete();

            Assert.IsFalse(sliceIter.MoveNext(), "last MoveNext()");

            iter = word.GetSliceEnumerator(Direction.Rightward);
            Assert.IsFalse(iter.MoveNext());
        }

        [Test]
        public void LeftBoundary()
        {
            var word = GetTestWord();
            var iter = word.GetSliceEnumerator(Direction.Rightward);
            iter.MoveNext();

            var slice = iter.Current;
            var sliceIter = slice.GetEnumerator();

            Assert.IsTrue(Word.LeftBoundary.Matches(null, sliceIter), "left boundary matches at first segment");
            Assert.IsTrue(sliceIter.MoveNext(), "first MoveNext()");
            Assert.AreSame(FeatureMatrixTest.MatrixA, sliceIter.Current, "next iter is A");
            Assert.IsFalse(Word.LeftBoundary.Matches(null, sliceIter), "left boundary doesn't match after first segment");
        }

        [Test]
        public void RightBoundary()
        {
            var word = GetTestWord();
            var iter = word.GetSliceEnumerator(Direction.Leftward);
            iter.MoveNext();
            iter.MoveNext(); // get two segments in the slice

            var slice = iter.Current;
            var sliceIter = slice.GetEnumerator();
            sliceIter.MoveNext();

            Assert.IsFalse(Word.RightBoundary.Matches(null, sliceIter), "right boundary doesn't match before last segment");
            Assert.IsTrue(sliceIter.MoveNext(), "first MoveNext()");
            Assert.AreSame(FeatureMatrixTest.MatrixC, sliceIter.Current, "next iter is C");
            Assert.IsTrue(Word.RightBoundary.Matches(null, sliceIter), "right boundary matches at last segment");
        }
    }
}
