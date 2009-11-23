using System;
using System.Collections.Generic;
using System.Linq;
using Phonix;

namespace Phonix.UnitTest
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
            Assert.IsNotNull(f.NullValue);
            Assert.AreEqual(f.Name, f.ToString());

            Assert.AreSame(f, f.NullValue.Feature);
            Assert.AreEqual("*" + TEST, f.NullValue.ToString());
        }

        [Test]
        public void Unary()
        {
            UnaryFeature f = new UnaryFeature(TEST);

            Assert.AreSame(f, f.Value.Feature);
            Assert.AreEqual(TEST, f.Value.ToString());
        }

        [Test]
        public void Binary()
        {
            BinaryFeature f = new BinaryFeature(TEST);

            Assert.AreSame(f, f.PlusValue.Feature);
            Assert.AreEqual("+" + TEST, f.PlusValue.ToString());

            Assert.AreSame(f, f.MinusValue.Feature);
            Assert.AreEqual("-" + TEST, f.MinusValue.ToString());
        }

        [Test]
        public void Scalar()
        {
            ScalarFeature f = new ScalarFeature(TEST);
            const int value = 3;

            var fv = f.Value(value);
            Assert.AreSame(f, fv.Feature);
            Assert.AreEqual(String.Format("{0}={1}", TEST, value), fv.ToString());

            var fv2 = f.Value(value);
            Assert.AreSame(fv, fv2);
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
        }

        [Test]
        public void NodeFeatureValue()
        {
            var fs = FeatureSetTest.GetTestSet();
            var flist = new Feature[] 
            {
                fs.Get<UnaryFeature>("un"),
                fs.Get<BinaryFeature>("bn"),
                fs.Get<ScalarFeature>("sc")
            };

            var node = new NodeFeature(TEST, flist);

            var fvA = node.Value(FeatureMatrixTest.MatrixA);
            var fvB = node.Value(FeatureMatrixTest.MatrixA);

            Assert.AreNotSame(fvA, fvB);
            Assert.AreEqual(fvA, fvB);
            Assert.IsTrue(fvA == fvB);
        }

    }

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
    }

    [TestFixture]
    public class FeatureMatrixTest
    {
        private static FeatureMatrix _matrixA;
        public static FeatureMatrix MatrixA
        {
            get
            {
                if (_matrixA == null)
                {
                    var fs = FeatureSetTest.GetTestSet();

                    FeatureValue[] fvs = new FeatureValue[] 
                    {
                        fs.Get<UnaryFeature>("un").Value,
                        fs.Get<UnaryFeature>("un2").NullValue,
                        fs.Get<BinaryFeature>("bn").PlusValue,
                        fs.Get<BinaryFeature>("bn2").MinusValue,
                        fs.Get<ScalarFeature>("sc").Value(1),
                        fs.Get<ScalarFeature>("sc2").Value(2)
                    };

                    _matrixA = new FeatureMatrix(fvs);
                }

                return _matrixA;
            }
        }

        private static FeatureMatrix _matrixB;
        public static FeatureMatrix MatrixB
        {
            get
            {
                if (_matrixB == null)
                {
                    var fs = FeatureSetTest.GetTestSet();

                    FeatureValue[] fvs = new FeatureValue[] 
                    {
                        fs.Get<BinaryFeature>("bn").PlusValue,
                        fs.Get<ScalarFeature>("sc").Value(2),
                    };

                    _matrixB = new FeatureMatrix(fvs);
                }

                return _matrixB;
            }
        }

        private static FeatureMatrix _matrixC;
        public static FeatureMatrix MatrixC
        {
            get
            {
                if (_matrixC == null)
                {
                    var fs = FeatureSetTest.GetTestSet();

                    FeatureValue[] fvs = new FeatureValue[] 
                    {
                        fs.Get<UnaryFeature>("un").Value,
                        fs.Get<BinaryFeature>("bn").MinusValue,
                        fs.Get<ScalarFeature>("sc").Value(1),
                    };

                    _matrixC = new FeatureMatrix(fvs);
                }

                return _matrixC;
            }
        }

        [Test]
        public void Ctor()
        {
            // successfully accessing MatrixA implies successful
            // construction (see definition of MatrixA above).
            var fm = MatrixA;
            var fm2 = new FeatureMatrix(MatrixA);

            foreach (var fv in fm)
            {
                Assert.AreSame(fv, fm2[fv.Feature]);

                // assert that we don't enumerate the zero values
                Assert.AreNotSame(fv, fv.Feature.NullValue);
            }
        }

        [Test]
        public void Weight()
        {
            var fm = MatrixA;
            Assert.AreEqual(5, fm.Weight);
            Assert.AreEqual(0, FeatureMatrix.Empty.Weight);
        }

        [Test]
        public void String()
        {

            // this is hard to verify without just reimplementing
            // FeatureMatrix.ToString(). Furthermore, if it's broken it'll be
            // clear pretty quickly, so we'll just do some obvious identity
            // tests.

            var fm = MatrixA;
            var fm2 = new FeatureMatrix(MatrixA);

            Assert.AreEqual(fm.ToString(), fm2.ToString());
            Console.Out.WriteLine("Manual feature matrix string check: " + fm.ToString());

            Assert.AreEqual("[]", FeatureMatrix.Empty.ToString());
        }

        [Test]
        public void Index()
        {
            var fs = FeatureSetTest.GetTestSet();
            var fm = MatrixB;

            var un = fm[fs.Get<Feature>("un")];
            Assert.IsNotNull(un);
            Assert.AreSame(un, un.Feature.NullValue);

            var bn = fm[fs.Get<Feature>("bn")];
            Assert.IsNotNull(bn);
            Assert.AreSame(bn, (bn.Feature as BinaryFeature).PlusValue);

            var sc = fm[fs.Get<Feature>("sc")];
            Assert.IsNotNull(sc);
            Assert.AreSame(sc, (sc.Feature as ScalarFeature).Value(2));

            var node = fm[fs.Get<Feature>("Node1")];
            Assert.IsNotNull(sc);
            Assert.AreEqual(node, (node.Feature as NodeFeature).Value(fm));
        }
    }

}
