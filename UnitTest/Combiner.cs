using System;
using System.Collections.Generic;
using System.Linq;
using Phonix;

namespace Phonix.UnitTest
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
            foreach (var fv in combo)
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
            var fm = combo.Combine(null, FeatureMatrix.Empty);
            Assert.AreEqual(combo.Count() - 1, fm.Weight);

            Assert.IsTrue(FeatureMatrixTest.MatrixA.Equals(fm), "combo with populated matrix");
        }

        [Test]
        public void CombineWithNullCombiner()
        {
            // combining with the null combiner should actually yield the same
            // matrix
            var fm = FeatureMatrixTest.MatrixA;
            Assert.AreSame(fm, MatrixCombiner.NullCombiner.Combine(null, fm));
        }

        [Test]
        public void CombineWithEmptyMatrix()
        {
            // combining with an empty matrix (one not explicitly set to
            // nullify values) shouldn't change anything
            var empty = new MatrixCombiner(FeatureMatrix.Empty);
            var fm = empty.Combine(null, FeatureMatrixTest.MatrixA);

            Assert.AreEqual(FeatureMatrixTest.MatrixA.Weight, fm.Weight);
            Assert.IsTrue(FeatureMatrixTest.MatrixA.Equals(fm), "combo with empty matrix");
        }

        [Test]
        public void CombineWithNullifyingMatrix()
        {
            // combining with a nullify matrix should clear the values
            var fs = FeatureSetTest.GetTestSet();
            var nullFm = new FeatureMatrix(new FeatureValue[] 
                    {
                        fs.Get<UnaryFeature>("un").NullValue,
                        fs.Get<UnaryFeature>("un2").NullValue,
                        fs.Get<BinaryFeature>("bn").NullValue,
                        fs.Get<BinaryFeature>("bn2").NullValue,
                        fs.Get<ScalarFeature>("sc").NullValue,
                        fs.Get<ScalarFeature>("sc2").NullValue
                    });
            var nullCombo = new MatrixCombiner(nullFm);
            var fm = nullCombo.Combine(null, FeatureMatrixTest.MatrixA);

            Assert.AreEqual(0, fm.Weight);
            foreach (var f in fs)
            {
                Assert.AreSame(f.NullValue, fm[f]);
            }
        }

        [Test]
        public void CombineVariable()
        {
            var fs = FeatureSetTest.GetTestSet();
            var un = fs.Get<UnaryFeature>("un");
            var sc = fs.Get<ScalarFeature>("sc");
            var test = new MatrixCombiner(new FeatureValueBase[] { un.VariableValue, sc.VariableValue });
            var ctx = new RuleContext();
            ctx.VariableFeatures[un] = un.Value;
            ctx.VariableFeatures[sc] = sc.Value(1);

            var fm = test.Combine(ctx, FeatureMatrixTest.MatrixB);
            Assert.AreSame(ctx.VariableFeatures[un], fm[un]);
            Assert.AreSame(ctx.VariableFeatures[sc], fm[sc]);
            foreach (var fv in FeatureMatrixTest.MatrixB)
            {
                if (fv.Feature != un && fv.Feature != sc)
                {
                    Assert.AreSame(fv, fm[fv.Feature]);
                }
            }
        }

        [Test]
        public void CombineVariableUndefined()
        {
            var fs = FeatureSetTest.GetTestSet();
            var un = fs.Get<UnaryFeature>("un");
            var sc = fs.Get<ScalarFeature>("sc");
            var test = new MatrixCombiner(new FeatureValueBase[] { un.VariableValue, sc.VariableValue });
            var ctx = new RuleContext();

            uint gotTrace = 0;
            var undef = new List<Feature>();
            Action<FeatureValueBase> tracer = (fv) => 
            {
                gotTrace++;
                undef.Add(fv.Feature);
            };
            Trace.OnUndefinedVariableUsed += tracer;

            var fm = test.Combine(ctx, FeatureMatrixTest.MatrixB);

            Assert.AreEqual(FeatureMatrixTest.MatrixB, fm);
            Assert.AreEqual(2, gotTrace);
            Assert.IsTrue(undef.Contains(un));
            Assert.IsTrue(undef.Contains(sc));

            Trace.OnUndefinedVariableUsed -= tracer;
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException))]
        public void CombineVariableNullContext()
        {
            var fs = FeatureSetTest.GetTestSet();
            var un = fs.Get<UnaryFeature>("un");
            var sc = fs.Get<ScalarFeature>("sc");
            var test = new MatrixCombiner(new FeatureValueBase[] { un.VariableValue, sc.VariableValue });

            // this should throw InvalidOperationException because the context
            // is null.

            test.Combine(null, FeatureMatrixTest.MatrixB);
        }
    }
}
