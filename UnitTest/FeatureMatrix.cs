using System;
using System.Collections.Generic;
using System.Linq;
using Phonix;

namespace Phonix.UnitTest
{
    using NUnit.Framework;
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
                        fs.Get<UnaryFeature>("un2").NullValue.GetValues(null).First(),
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
        }
    }
}
