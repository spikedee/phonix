using System;
using System.Collections.Generic;
using System.Linq;
using Phonix;

namespace Phonix.Test
{
    using NUnit.Framework;

    [TestFixture]
    public class TierTest
    {
        public static Tier Bottom = new Tier("Bottom", new Tier[] {});
        public static Tier MidA = new Tier("MidA", new Tier[] { Bottom });
        public static Tier MidB = new Tier("MidB", new Tier[] { Bottom });
        public static Tier Top = new Tier("Top", new Tier[] { MidA, MidB });

        [Test]
        public void Ctor()
        {
            var tier = new Tier("test", new Tier[] {});

            Assert.AreEqual("test", tier.Name);
            Assert.AreEqual(0, tier.Parents.Count());
            Assert.AreEqual(0, tier.Children.Count());

            var tier2 = new Tier("test2", new Tier[] { tier });
            Assert.AreEqual("test2", tier2.Name);
            Assert.AreEqual(0, tier2.Parents.Count());
            Assert.AreEqual(1, tier2.Children.Count());

            // the original tier should now have a parent
            Assert.AreEqual(1, tier.Parents.Count());
        }

        [Test]
        public void HasParent()
        {
            Assert.IsTrue(Bottom.HasParent(MidA));
            Assert.IsTrue(Bottom.HasParent(MidB));
            Assert.IsTrue(MidA.HasParent(Top));
            Assert.IsTrue(MidB.HasParent(Top));

            Assert.IsFalse(Bottom.HasParent(Top));
            Assert.IsFalse(Top.HasParent(Bottom));
            Assert.IsFalse(Top.HasParent(MidA));
            Assert.IsFalse(Top.HasParent(MidB));
            Assert.IsFalse(Top.HasParent(null));
        }

        [Test]
        public void HasChild()
        {
            Assert.IsTrue(Top.HasChild(MidA));
            Assert.IsTrue(Top.HasChild(MidB));
            Assert.IsTrue(MidA.HasChild(Bottom));
            Assert.IsTrue(MidB.HasChild(Bottom));

            Assert.IsFalse(Top.HasChild(Bottom));
            Assert.IsFalse(Bottom.HasChild(Top));
            Assert.IsFalse(Bottom.HasChild(MidA));
            Assert.IsFalse(Bottom.HasChild(MidB));
            Assert.IsFalse(Bottom.HasChild(null));
        }

        [Test]
        public void AncestorMatcher()
        {
            var bottom = new Segment(Bottom, FeatureMatrix.Empty, new Segment[] {});
            var midA = new Segment(MidA, FeatureMatrix.Empty, new Segment[] { bottom });
            var midB = new Segment(MidB, FeatureMatrix.Empty, new Segment[] { bottom });
            var top = new Segment(Top, FeatureMatrix.Empty, new Segment[] { midA, midB });

            Assert.IsFalse(Bottom.AncestorMatcher.Matches(null, bottom));
            Assert.IsTrue(MidA.AncestorMatcher.Matches(null, bottom));
            Assert.IsTrue(MidB.AncestorMatcher.Matches(null, bottom));
            Assert.IsTrue(Top.AncestorMatcher.Matches(null, bottom));

            Assert.IsFalse(Bottom.AncestorMatcher.Matches(null, midA));
            Assert.IsFalse(MidA.AncestorMatcher.Matches(null, midA));
            Assert.IsFalse(MidB.AncestorMatcher.Matches(null, midA));
            Assert.IsTrue(Top.AncestorMatcher.Matches(null, midA));

            Assert.IsFalse(Bottom.AncestorMatcher.Matches(null, midB));
            Assert.IsFalse(MidA.AncestorMatcher.Matches(null, midB));
            Assert.IsFalse(MidB.AncestorMatcher.Matches(null, midB));
            Assert.IsTrue(Top.AncestorMatcher.Matches(null, midB));

            Assert.IsFalse(Bottom.AncestorMatcher.Matches(null, top));
            Assert.IsFalse(MidA.AncestorMatcher.Matches(null, top));
            Assert.IsFalse(MidB.AncestorMatcher.Matches(null, top));
            Assert.IsFalse(Top.AncestorMatcher.Matches(null, top));
        }
    }
}
