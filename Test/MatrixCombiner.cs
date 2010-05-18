using System;
using System.Collections.Generic;
using System.Linq;
using Phonix;

namespace Phonix.Test
{
    using NUnit.Framework;

    [TestFixture]
    public class MatrixCombinerTest
    {
        [Test]
        public void Ctor()
        {
            var combo = new MatrixCombiner(FeatureMatrixTest.MatrixA);
            Assert.AreEqual(FeatureMatrixTest.MatrixA.Weight + 1, combo.Count());

            // the combo enumerator should include the zero values
            bool hasNull = false;
            foreach (FeatureValue fv in combo)
            {
                Assert.AreSame(fv, FeatureMatrixTest.MatrixA[fv.Feature]);
                if (fv == fv.Feature.NullValue)
                {
                    hasNull = true;
                }
            }
            Assert.IsTrue(hasNull);
        }

        [Test]
        public void CombineWithPopulatedMatrix()
        {
            var combo = new MatrixCombiner(FeatureMatrixTest.MatrixA);

            // combining with a non-emtpy combiner should fill in new values
            var seg = new MutableSegment(FeatureMatrix.Empty);
            combo.Combine(null, seg);
            Assert.AreEqual(combo.Count() - 1, seg.Matrix.Weight);

            Assert.IsTrue(FeatureMatrixTest.MatrixA.Equals(seg.Matrix), "combo with populated matrix");
        }

        [Test]
        public void CombineWithNullCombiner()
        {
            // combining with the null combiner should actually yield the same
            // matrix
            var seg = new MutableSegment(FeatureMatrixTest.MatrixA);
            MatrixCombiner.NullCombiner.Combine(null, seg);
            Assert.AreSame(FeatureMatrixTest.MatrixA, seg.Matrix);
        }

        [Test]
        public void CombineWithEmptyMatrix()
        {
            // combining with an empty matrix (one not explicitly set to
            // nullify values) shouldn't change anything
            var empty = new MatrixCombiner(FeatureMatrix.Empty);
            var seg = new MutableSegment(FeatureMatrixTest.MatrixA);
            empty.Combine(null, seg);

            Assert.AreEqual(FeatureMatrixTest.MatrixA.Weight, seg.Matrix.Weight);
            Assert.IsTrue(FeatureMatrixTest.MatrixA.Equals(seg.Matrix), "combo with empty matrix");
        }

        [Test]
        public void CombineWithNullifyingMatrix()
        {
            // combining with a nullify matrix should clear the values
            var fs = FeatureSetTest.GetTestSet();
            var nullFm = new FeatureMatrix(new FeatureValue[] 
                    {
                        (FeatureValue) fs.Get<UnaryFeature>("un").NullValue,
                        (FeatureValue) fs.Get<UnaryFeature>("un2").NullValue,
                        (FeatureValue) fs.Get<BinaryFeature>("bn").NullValue,
                        (FeatureValue) fs.Get<BinaryFeature>("bn2").NullValue,
                        (FeatureValue) fs.Get<ScalarFeature>("sc").NullValue,
                        (FeatureValue) fs.Get<ScalarFeature>("sc2").NullValue
                    });
            var nullCombo = new MatrixCombiner(nullFm);
            var seg = new MutableSegment(FeatureMatrixTest.MatrixA);
            nullCombo.Combine(null, seg);

            Assert.AreEqual(0, seg.Matrix.Weight);
            foreach (var f in fs)
            {
                if (f is NodeFeature) continue;
                Assert.AreSame(f.NullValue, seg.Matrix[f]);
            }
        }

        [Test]
        public void CombineVariable()
        {
            var fs = FeatureSetTest.GetTestSet();
            var un = fs.Get<UnaryFeature>("un");
            var sc = fs.Get<ScalarFeature>("sc");
            var test = new MatrixCombiner(new ICombinable[] { un.VariableValue, sc.VariableValue });
            var ctx = new RuleContext();
            ctx.VariableFeatures[un] = un.Value;
            ctx.VariableFeatures[sc] = sc.Value(1);

            var seg = new MutableSegment(FeatureMatrixTest.MatrixB);
            test.Combine(ctx, seg);

            Assert.AreSame(ctx.VariableFeatures[un], seg.Matrix[un]);
            Assert.AreSame(ctx.VariableFeatures[sc], seg.Matrix[sc]);
            foreach (var fv in FeatureMatrixTest.MatrixB)
            {
                if (fv.Feature != un && fv.Feature != sc)
                {
                    Assert.AreSame(fv, seg.Matrix[fv.Feature]);
                }
            }
        }

        [Test]
        [ExpectedException(typeof(UndefinedFeatureVariableException))]
        public void CombineVariableUndefined()
        {
            var fs = FeatureSetTest.GetTestSet();
            var un = fs.Get<UnaryFeature>("un");
            var sc = fs.Get<ScalarFeature>("sc");
            var test = new MatrixCombiner(new ICombinable[] { un.VariableValue, sc.VariableValue });
            var ctx = new RuleContext();

            test.Combine(ctx, new MutableSegment(FeatureMatrixTest.MatrixB));
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException))]
        public void CombineVariableNullContext()
        {
            var fs = FeatureSetTest.GetTestSet();
            var un = fs.Get<UnaryFeature>("un");
            var sc = fs.Get<ScalarFeature>("sc");
            var test = new MatrixCombiner(new ICombinable[] { un.VariableValue, sc.VariableValue });

            // this should throw InvalidOperationException because the context
            // is null.

            test.Combine(null, new MutableSegment(FeatureMatrixTest.MatrixB));
        }
    }
}
