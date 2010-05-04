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
    }
}
