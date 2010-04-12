using System;
using System.Collections.Generic;
using System.Linq;
using Phonix;

namespace Phonix.Test
{
    using NUnit.Framework;

    [TestFixture]
    public class FeatureSetTest
    {
        private static Feature[] Features = new Feature[]
        {
            new UnaryFeature("un"),
            new UnaryFeature("un2"),
            new BinaryFeature("bn"),
            new BinaryFeature("bn2"),
            new ScalarFeature("sc"),
            new ScalarFeature("sc2")
        };

        public static FeatureSet GetTestSet()
        {
            var fs = new FeatureSet();
            foreach (Feature f in Features)
            {
                fs.Add(f);
            }
            var node1 = new NodeFeature("Node1", new Feature[] { Features[0], Features[2], Features[4] });
            var node2 = new NodeFeature("Node2", new Feature[] { Features[1], Features[3], Features[5] });
            var root = new NodeFeature("ROOT", new Feature[] { node1, node2 });
            fs.Add(node1);
            fs.Add(node2);
            fs.Add(root);

            return fs;
        }

        [Test]
        public void Add()
        {
            var fs = new FeatureSet();
            var f = new FeatureTest.TestFeature("test");
            fs.Add(f);

            Assert.IsTrue(fs.Has<FeatureTest.TestFeature>("test"));
        }

        [Test]
        public void AddOverwrite()
        {
            var fs = new FeatureSet();
            var f1 = new FeatureTest.TestFeature("foo");
            var f2 = new FeatureTest.TestFeature("foo");

            fs.Add(f1);
            fs.Add(f2); // shouldn't throw exception
        }

        [Test]
        public void Has()
        {
            FeatureSet fs = GetTestSet();

            foreach (var f in Features)
            {
                Assert.IsTrue(fs.Has<Feature>(f.Name));
            }

            Assert.IsTrue(fs.Has<UnaryFeature>("un"));
            Assert.IsFalse(fs.Has<BinaryFeature>("un"));
            Assert.IsFalse(fs.Has<ScalarFeature>("un"));

            Assert.IsFalse(fs.Has<UnaryFeature>("bn"));
            Assert.IsTrue(fs.Has<BinaryFeature>("bn"));
            Assert.IsFalse(fs.Has<ScalarFeature>("bn"));

            Assert.IsFalse(fs.Has<UnaryFeature>("sc"));
            Assert.IsFalse(fs.Has<BinaryFeature>("sc"));
            Assert.IsTrue(fs.Has<ScalarFeature>("sc"));
        }

        [Test]
        public void Get()
        {
            FeatureSet fs = GetTestSet();

            foreach (var f in Features)
            {
                Assert.AreSame(f, fs.Get<Feature>(f.Name));

                switch (f.Name[0])
                {
                    case 'u':
                        Assert.AreSame(f, fs.Get<UnaryFeature>(f.Name));
                        Util.AssertThrow<FeatureTypeException>(() => { fs.Get<BinaryFeature>(f.Name); });
                        Util.AssertThrow<FeatureTypeException>(() => { fs.Get<ScalarFeature>(f.Name); });
                        break;

                    case 'b':
                        Util.AssertThrow<FeatureTypeException>(() => { fs.Get<UnaryFeature>(f.Name); });
                        Assert.AreSame(f, fs.Get<BinaryFeature>(f.Name));
                        Util.AssertThrow<FeatureTypeException>(() => { fs.Get<ScalarFeature>(f.Name); });
                        break;

                    case 's':
                        Util.AssertThrow<FeatureTypeException>(() => { fs.Get<UnaryFeature>(f.Name); });
                        Util.AssertThrow<FeatureTypeException>(() => { fs.Get<BinaryFeature>(f.Name); });
                        Assert.AreSame(f, fs.Get<ScalarFeature>(f.Name));
                        break;

                    default:
                        Assert.Fail("Unexpected feature name");
                        break;
                }
            }
        }

        [Test]
        public void Enumerator()
        {
            var fs = GetTestSet();
            var f = new FeatureTest.TestFeature("test");
            fs.Add(f);

            int count = 0;
            var flist = new List<Feature>();
            foreach (Feature x in fs)
            {
                Assert.IsFalse(flist.Contains(x));
                flist.Add(x);
                count++;
            }

            // the expected length is the length of Features, plus the one we
            // added, plus the three nodes.
            Assert.AreEqual(Features.Length + 1 + 3, flist.Count);
        }

        [Test]
        public void FeatureDefined()
        {
            int calledDefined = 0;
            Feature gotFeature = null;
            var fs = new FeatureSet();

            fs.FeatureDefined += f => { calledDefined++; gotFeature = f; };

            var bf = new BinaryFeature("test");
            fs.Add(bf);

            Assert.AreEqual(1, calledDefined);
            Assert.AreSame(bf, gotFeature);
        }

        [Test]
        public void FeatureRedefined()
        {
            int calledRedefined = 0;
            Feature gotFeature = null;
            bool gotTest = false;

            var fs = new FeatureSet();
            var oldTest = new UnaryFeature("test");
            var bf = new BinaryFeature("test");
            var bf2 = new BinaryFeature("test");

            fs.FeatureRedefined += (old, newer) => 
            { 
                calledRedefined++; 
                gotFeature = newer; 
                if (old == oldTest)
                    gotTest = true;
            };

            fs.Add(oldTest);
            fs.Add(bf);

            Assert.AreEqual(1, calledRedefined);
            Assert.IsTrue(gotTest);
            Assert.AreSame(bf, gotFeature);

            // check that adding an identical feature doesn't result in overwrite
            fs.Add(bf2);

            Assert.AreEqual(1, calledRedefined);
            Assert.AreSame(fs.Get<Feature>(bf.Name), bf);
        }
    }
}
