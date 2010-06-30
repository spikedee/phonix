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

            var test2 = new MutableSegment(Tier.Onset, FeatureMatrixTest.MatrixB, new Segment[] { test });

            Assert.AreSame(Tier.Onset, test2.Tier);
            Assert.AreSame(FeatureMatrixTest.MatrixB, test2.Matrix);
            Assert.AreEqual(1, test2.Children.Count());
            Assert.AreEqual(1, test.Parents.Count());
            Assert.AreEqual(0, test2.Parents.Count());
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

        [Test]
        public void Parents()
        {
            var bottom = new MutableSegment(TierTest.Bottom, FeatureMatrixTest.MatrixA, new Segment[] {});
            var midA = new MutableSegment(TierTest.MidA, FeatureMatrixTest.MatrixA, new Segment[] {});
            var midB = new MutableSegment(TierTest.MidB, FeatureMatrixTest.MatrixA, new Segment[] {});
            var top = new MutableSegment(TierTest.Top, FeatureMatrixTest.MatrixA, new Segment[] {});

            top.Children = new Segment[] { midA, midB };
            midA.Children = new Segment[] { bottom };
            midB.Children = new Segment[] { bottom };

            Assert.AreEqual(2, bottom.Parents.Count());
            Assert.AreEqual(1, midA.Parents.Count());
            Assert.AreEqual(1, midB.Parents.Count());
            Assert.AreEqual(0, top.Parents.Count());

            Assert.IsTrue(bottom.Parents.Contains(midA));
            Assert.IsTrue(bottom.Parents.Contains(midB));
            Assert.IsTrue(midA.Parents.Contains(top));
            Assert.IsTrue(midB.Parents.Contains(top));

            top.Children = new Segment[] { midA };

            Assert.AreEqual(2, bottom.Parents.Count());
            Assert.AreEqual(1, midA.Parents.Count());
            Assert.AreEqual(0, midB.Parents.Count());

            Assert.IsTrue(bottom.Parents.Contains(midA));
            Assert.IsTrue(bottom.Parents.Contains(midB));
            Assert.IsTrue(midA.Parents.Contains(top));
            Assert.IsFalse(midB.Parents.Contains(top));
        }

        [Test]
        public void Descendants()
        {
            var bottom = new MutableSegment(TierTest.Bottom, FeatureMatrixTest.MatrixA, new Segment[] {});
            var midA = new MutableSegment(TierTest.MidA, FeatureMatrixTest.MatrixA, new Segment[] { bottom });
            var midB = new MutableSegment(TierTest.MidB, FeatureMatrixTest.MatrixA, new Segment[] { bottom });
            var top = new MutableSegment(TierTest.Top, FeatureMatrixTest.MatrixA, new Segment[] { midA, midB });

            Assert.IsFalse(top.HasDescendant(TierTest.Top));
            Assert.IsTrue(top.HasDescendant(TierTest.MidA));
            Assert.IsTrue(top.HasDescendant(TierTest.MidB));
            Assert.IsTrue(top.HasDescendant(TierTest.Bottom));
            Assert.AreSame(top.FirstDescendant(TierTest.MidA), midA);
            Assert.AreSame(top.FirstDescendant(TierTest.MidB), midB);
            Assert.AreSame(top.FirstDescendant(TierTest.Bottom), bottom);

            Assert.IsFalse(midA.HasDescendant(TierTest.Top));
            Assert.IsFalse(midA.HasDescendant(TierTest.MidA));
            Assert.IsFalse(midA.HasDescendant(TierTest.MidB));
            Assert.IsTrue(midA.HasDescendant(TierTest.Bottom));
            Assert.AreSame(top.FirstDescendant(TierTest.Bottom), bottom);

            Assert.IsFalse(midB.HasDescendant(TierTest.Top));
            Assert.IsFalse(midB.HasDescendant(TierTest.MidA));
            Assert.IsFalse(midB.HasDescendant(TierTest.MidB));
            Assert.IsTrue(midB.HasDescendant(TierTest.Bottom));
            Assert.AreSame(top.FirstDescendant(TierTest.Bottom), bottom);

            Assert.IsFalse(bottom.HasDescendant(TierTest.Top));
            Assert.IsFalse(bottom.HasDescendant(TierTest.MidA));
            Assert.IsFalse(bottom.HasDescendant(TierTest.MidB));
            Assert.IsFalse(bottom.HasDescendant(TierTest.Bottom));
        }

        [Test]
        public void Ancestors()
        {
            var bottom = new MutableSegment(TierTest.Bottom, FeatureMatrixTest.MatrixA, new Segment[] {});
            var midA = new MutableSegment(TierTest.MidA, FeatureMatrixTest.MatrixA, new Segment[] { bottom });
            var midB = new MutableSegment(TierTest.MidB, FeatureMatrixTest.MatrixA, new Segment[] { bottom });
            var top = new MutableSegment(TierTest.Top, FeatureMatrixTest.MatrixA, new Segment[] { midA, midB });

            Assert.IsTrue(bottom.HasAncestor(TierTest.Top));
            Assert.IsTrue(bottom.HasAncestor(TierTest.MidA));
            Assert.IsTrue(bottom.HasAncestor(TierTest.MidB));
            Assert.IsFalse(bottom.HasAncestor(TierTest.Bottom));
            Assert.AreSame(bottom.FirstAncestor(TierTest.Top), top);
            Assert.AreSame(bottom.FirstAncestor(TierTest.MidA), midA);
            Assert.AreSame(bottom.FirstAncestor(TierTest.MidB), midB);

            Assert.IsTrue(midA.HasAncestor(TierTest.Top));
            Assert.IsFalse(midA.HasAncestor(TierTest.MidA));
            Assert.IsFalse(midA.HasAncestor(TierTest.MidB));
            Assert.IsFalse(midA.HasAncestor(TierTest.Bottom));
            Assert.AreSame(midA.FirstAncestor(TierTest.Top), top);

            Assert.IsTrue(midB.HasAncestor(TierTest.Top));
            Assert.IsFalse(midB.HasAncestor(TierTest.MidA));
            Assert.IsFalse(midB.HasAncestor(TierTest.MidB));
            Assert.IsFalse(midB.HasAncestor(TierTest.Bottom));
            Assert.AreSame(midB.FirstAncestor(TierTest.Top), top);

            Assert.IsFalse(top.HasAncestor(TierTest.Top));
            Assert.IsFalse(top.HasAncestor(TierTest.MidA));
            Assert.IsFalse(top.HasAncestor(TierTest.MidB));
            Assert.IsFalse(top.HasAncestor(TierTest.Bottom));
        }

        [Test]
        public void Detach()
        {
            var bottom = new MutableSegment(TierTest.Bottom, FeatureMatrixTest.MatrixA, new Segment[] {});
            var midA = new MutableSegment(TierTest.MidA, FeatureMatrixTest.MatrixA, new Segment[] { bottom });
            var midA2 = new MutableSegment(TierTest.MidA, FeatureMatrixTest.MatrixA, new Segment[] { bottom });
            var midB = new MutableSegment(TierTest.MidB, FeatureMatrixTest.MatrixA, new Segment[] { bottom });
            var top = new MutableSegment(TierTest.Top, FeatureMatrixTest.MatrixA, new Segment[] { midA, midA2, midB });

            bottom.Detach(TierTest.Top);

            // bottom should now be unattached
            Assert.IsFalse(bottom.HasAncestor(TierTest.MidA));
            Assert.IsFalse(bottom.HasAncestor(TierTest.MidB));
            Assert.IsFalse(bottom.HasAncestor(TierTest.Top));

            // midA and midB should be unaffected
            Assert.IsTrue(midA.HasAncestor(TierTest.Top));
            Assert.AreSame(top, midA.FirstAncestor(TierTest.Top));
            Assert.IsTrue(midA2.HasAncestor(TierTest.Top));
            Assert.AreSame(top, midA2.FirstAncestor(TierTest.Top));
            Assert.IsTrue(midB.HasAncestor(TierTest.Top));
            Assert.AreSame(top, midB.FirstAncestor(TierTest.Top));
        }
    }
}
