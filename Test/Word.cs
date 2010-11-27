using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace Phonix.Test
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

        public static string SpellSlice(WordSlice slice)
        {
            SymbolSet ss = SymbolSetTest.GetTestSet();
            StringBuilder str = new StringBuilder();
            foreach (Segment seg in slice)
            {
                try
                {
                    str.Append(ss.Spell(seg.Matrix));
                }
                catch (SpellingException)
                {
                    str.Append(seg.Matrix.ToString());
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
        public void SliceRightward()
        {
            var word = GetTestWord();

            var iter = word.Slice(Direction.Rightward).GetEnumerator();
            Assert.IsTrue(iter.MoveNext());
            Assert.AreEqual("abc", SpellSlice(iter.Current));
            
            Assert.IsTrue(iter.MoveNext());
            Assert.AreEqual("bc", SpellSlice(iter.Current));
            
            Assert.IsTrue(iter.MoveNext());
            Assert.AreEqual("c", SpellSlice(iter.Current));

            Assert.IsFalse(iter.MoveNext());
        }

        [Test]
        public void SliceLeftward()
        {
            var word = GetTestWord();

            var iter = word.Slice(Direction.Leftward).GetEnumerator();
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

            var iter = word.Slice(Direction.Rightward, filter).GetEnumerator();
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

            var iter = word.Slice(Direction.Leftward, filter).GetEnumerator();
            Assert.IsTrue(iter.MoveNext());
            Assert.AreEqual("c", SpellSlice(iter.Current));
            
            Assert.IsTrue(iter.MoveNext());
            Assert.AreEqual("ac", SpellSlice(iter.Current));
            
            Assert.IsFalse(iter.MoveNext());
        }

        [Test]
        public void LeftBoundary()
        {
            var word = GetTestWord();
            var iter = word.Slice(Direction.Rightward).GetEnumerator();
            iter.MoveNext();

            var slice = iter.Current;
            var sliceIter = slice.GetEnumerator();

            Assert.IsTrue(WordBoundary.Left.Matches(null, sliceIter), "left boundary matches at first segment");
            Assert.IsTrue(sliceIter.MoveNext(), "first MoveNext()");
            Assert.AreSame(FeatureMatrixTest.MatrixA, sliceIter.Current.Matrix, "next iter is A");
            Assert.IsFalse(WordBoundary.Left.Matches(null, sliceIter), "left boundary doesn't match after first segment");
        }

        [Test]
        public void RightBoundary()
        {
            var word = GetTestWord();
            var iter = word.Slice(Direction.Leftward).GetEnumerator();
            iter.MoveNext();
            iter.MoveNext(); // get two segments in the slice

            var slice = iter.Current;
            var sliceIter = slice.GetEnumerator();
            sliceIter.MoveNext();

            Assert.IsFalse(WordBoundary.Right.Matches(null, sliceIter), "right boundary doesn't match before last segment");
            Assert.IsTrue(sliceIter.MoveNext(), "first MoveNext()");
            Assert.AreSame(FeatureMatrixTest.MatrixC, sliceIter.Current.Matrix, "next iter is C");
            Assert.IsTrue(WordBoundary.Right.Matches(null, sliceIter), "right boundary matches at last segment");
        }

        [Test]
        public void LeftBoundaryFiltered()
        {
            var word = GetTestWord();
            var fs = FeatureSetTest.GetTestSet();
            var filter = new MatrixMatcher(new IMatchable[] { fs.Get<Feature>("bn2").NullValue });

            var iter = word.Slice(Direction.Rightward, filter).GetEnumerator();
            iter.MoveNext();

            var slice = iter.Current;
            var sliceIter = slice.GetEnumerator();

            Assert.IsTrue(WordBoundary.Left.Matches(null, sliceIter), "left boundary matches at first segment");
            Assert.IsTrue(sliceIter.MoveNext(), "first MoveNext()");
            Assert.AreSame(FeatureMatrixTest.MatrixB, sliceIter.Current.Matrix, "next iter is B");
            Assert.IsFalse(WordBoundary.Left.Matches(null, sliceIter), "left boundary doesn't match after first segment");
        }

        [Test]
        public void RightBoundaryFiltered()
        {
            var word = GetTestWord();
            var fs = FeatureSetTest.GetTestSet();
            var filter = new MatrixMatcher(new IMatchable[] { fs.Get<BinaryFeature>("bn").PlusValue });

            var iter = word.Slice(Direction.Rightward, filter).GetEnumerator();
            iter.MoveNext(); // get one segment into the slice

            var slice = iter.Current;
            var sliceIter = slice.GetEnumerator();
            sliceIter.MoveNext();

            Assert.IsFalse(WordBoundary.Right.Matches(null, sliceIter), "right boundary doesn't match before last segment");
            Assert.IsTrue(sliceIter.MoveNext(), "first MoveNext()");
            Assert.AreSame(FeatureMatrixTest.MatrixB, sliceIter.Current.Matrix, "next iter is B");
            Assert.IsTrue(WordBoundary.Right.Matches(null, sliceIter), "right boundary matches at last segment");
        }
    }
}
