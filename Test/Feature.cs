using System;
using System.Collections.Generic;
using System.Linq;
using Phonix;

namespace Phonix.Test
{
    using NUnit.Framework;

    [TestFixture]
    public class FeatureTest
    {
        private const string TEST = "Test";

        public class TestFeature : Feature
        {
            public TestFeature(string name)
                : base(name)
            {
            }
        }

        [Test]
        public void Base()
        {
            Feature f = new TestFeature(TEST);

            Assert.AreEqual(TEST, f.Name);
            Assert.AreEqual(f.Name, f.ToString());

            Assert.IsNotNull(f.NullValue);
            Assert.AreSame(f, ((FeatureValue) f.NullValue).Feature);
            Assert.AreEqual("*" + TEST, f.NullValue.ToString());

            Assert.IsNotNull(f.VariableValue);
            Assert.AreEqual("$" + TEST, f.VariableValue.ToString());
        }

        [Test]
        public void Unary()
        {
            UnaryFeature f = new UnaryFeature(TEST);

            Assert.IsNotNull(f.Value);
            Assert.AreSame(f, f.Value.Feature);
            Assert.AreEqual(TEST, f.Value.ToString());
        }

        [Test]
        public void Binary()
        {
            BinaryFeature f = new BinaryFeature(TEST);

            Assert.IsNotNull(f.PlusValue);
            Assert.AreSame(f, f.PlusValue.Feature);
            Assert.AreEqual("+" + TEST, f.PlusValue.ToString());

            Assert.IsNotNull(f.MinusValue);
            Assert.AreSame(f, f.MinusValue.Feature);
            Assert.AreEqual("-" + TEST, f.MinusValue.ToString());
        }

        [Test]
        public void Scalar()
        {
            ScalarFeature f = new ScalarFeature(TEST);
            const int value = 3;

            var fv = f.Value(value);
            Assert.IsNotNull(fv);
            Assert.AreSame(f, fv.Feature);
            Assert.AreEqual(String.Format("{0}={1}", TEST, value), fv.ToString());

            var fv2 = f.Value(value);
            Assert.IsNotNull(fv2);
            Assert.AreSame(fv, fv2);
        }

        [Test]
        public void ScalarBounds()
        {
            int min = 1;
            int max = 3;
            ScalarFeature f = new ScalarFeature(TEST, min, max);

            Assert.AreEqual(min, f.Min.Value);
            Assert.AreEqual(max, f.Max.Value);

            for (int i = min - 1; i < max + 1; i++)
            {
                try
                {
                    var fv = f.Value(i);
                    if (i < min || i > max)
                    {
                        Assert.Fail("should have thrown exception, i={0}", i);
                    }
                    Assert.IsNotNull(fv);
                    Assert.AreSame(f, fv.Feature);
                    Assert.AreEqual(String.Format("{0}={1}", TEST, i), fv.ToString());
                }
                catch (ScalarValueRangeException ex)
                {
                    if (i >= min && i <= max)
                    {
                        Assert.Fail("should not have thrown exception: {0}", ex.Message);
                    }
                }
            }

            try
            {
                f = new ScalarFeature(TEST, max, min);
                Assert.Fail("should have thrown exception with " + f);
            }
            catch (ArgumentException)
            {
            }
        }

        [Test]
        public void Node()
        {
            var flist = new Feature[] 
            {
                new UnaryFeature(TEST + "Unary"),
                new BinaryFeature(TEST + "Binary"),
                new ScalarFeature(TEST + "Scalar")
            };
            NodeFeature node = new NodeFeature(TEST, flist);

            Assert.AreEqual(node.Name, TEST);

            var exists = node.ExistsValue;
            Assert.IsNotNull(exists);
        }
    }
}
