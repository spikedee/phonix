using System;
using System.Collections.Generic;
using System.Linq;
using Phonix;

namespace Phonix.Test
{
    using NUnit.Framework;

    [TestFixture]
    public class SegmentTest
    {
        [Test]
        public void Ctor()
        {
            var test = new MutableSegment(Tier.Segment, FeatureMatrixTest.MatrixA, new Segment[] {});

            Assert.AreSame(Tier.Segment, test.Tier);
            Assert.AreSame(FeatureMatrixTest.MatrixA, test.Matrix);
            Assert.AreEqual(0, test.Children.Count());
            Assert.AreEqual(0, test.Parents.Count());
        }

        [Test]
        public void Matrix()
        {
            var test = new MutableSegment(Tier.Segment, FeatureMatrixTest.MatrixA, new Segment[] {});

            test.Matrix = FeatureMatrixTest.MatrixB;
            Assert.AreSame(FeatureMatrixTest.MatrixB, test.Matrix);

            try
            {
                test.Matrix = null;
                Assert.Fail("should have thrown exception");
            }
            catch (ArgumentNullException)
            {
            }
        }

        [Test]
        public void Children()
        {
            var midA = new MutableSegment(TierTest.MidA, FeatureMatrixTest.MatrixA, new Segment[] {});
            var midB = new MutableSegment(TierTest.MidB, FeatureMatrixTest.MatrixA, new Segment[] {});
            var top = new MutableSegment(TierTest.Top, FeatureMatrixTest.MatrixA, new Segment[] {});

            top.Children = new Segment[] { midA, midB };

            Assert.AreEqual(2, top.Children.Count());
            Assert.AreEqual(1, midA.Parents.Count());
            Assert.AreEqual(1, midB.Parents.Count());

            Assert.AreSame(top, midA.Parents.First());
            Assert.AreSame(top, midB.Parents.First());
            Assert.AreSame(midA, top.Children.First());
            Assert.AreSame(midB, top.Children.Last());

            top.Children = new Segment[] { midA };

            Assert.AreEqual(1, top.Children.Count());
            Assert.AreEqual(1, midA.Parents.Count());
            Assert.AreEqual(0, midB.Parents.Count());

            Assert.AreSame(top, midA.Parents.First());
            Assert.AreSame(midA, top.Children.Last());
        }
    }
}
