using Phonix;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Phonix.Test
{
    using NUnit.Framework;

    [TestFixture]
    public class FeatureValueTest
    {
        [Test]
        public void Unary()
        {
            var fs = FeatureSetTest.GetTestSet();
            var un = fs.Get<UnaryFeature>("un");

            Assert.IsTrue(un.Value.Matches(null, new MutableSegment(FeatureMatrixTest.MatrixA)));
            Assert.IsFalse(un.Value.Matches(null, new MutableSegment(FeatureMatrixTest.MatrixB)));
        }

        [Test]
        public void Binary()
        {
            var fs = FeatureSetTest.GetTestSet();
            var bn = fs.Get<BinaryFeature>("bn");

            Assert.IsTrue(bn.PlusValue.Matches(null, new MutableSegment(FeatureMatrixTest.MatrixA)));
            Assert.IsFalse(bn.PlusValue.Matches(null, new MutableSegment(FeatureMatrixTest.MatrixC)));

            Assert.IsTrue(bn.MinusValue.Matches(null, new MutableSegment(FeatureMatrixTest.MatrixC)));
            Assert.IsFalse(bn.MinusValue.Matches(null, new MutableSegment(FeatureMatrixTest.MatrixA)));
        }

        [Test]
        public void Scalar()
        {
            var fs = FeatureSetTest.GetTestSet();
            var sc = fs.Get<ScalarFeature>("sc");

            // check that scalar feature values are the same every invocation
            Assert.AreSame(sc.Value(2), sc.Value(2));
            Assert.AreNotEqual(sc.Value(1), sc.Value(2));

            Assert.IsTrue(sc.Value(1).Matches(null, new MutableSegment(FeatureMatrixTest.MatrixA)));
            Assert.IsFalse(sc.Value(1).Matches(null, new MutableSegment(FeatureMatrixTest.MatrixB)));
        }

        [Test]
        public void Variable()
        {
            var fs = FeatureSetTest.GetTestSet();
            var bn = fs.Get<BinaryFeature>("bn");
            var ctx = new RuleContext();

            Assert.IsTrue(bn.VariableValue.Matches(ctx, new MutableSegment(FeatureMatrixTest.MatrixA)));
            Assert.IsTrue(ctx.VariableFeatures.ContainsKey(bn));
            Assert.AreSame(ctx.VariableFeatures[bn], FeatureMatrixTest.MatrixA[bn]);

            Assert.IsTrue(bn.VariableValue.Matches(ctx, new MutableSegment(FeatureMatrixTest.MatrixB)));
            Assert.IsFalse(bn.VariableValue.Matches(ctx, new MutableSegment(FeatureMatrixTest.MatrixC)));
        }

        [Test]
        public void NodeExists()
        {
            var fs = FeatureSetTest.GetTestSet();
            var node = fs.Get<NodeFeature>("Node2");

            Assert.IsTrue(node.ExistsValue.Matches(null, new MutableSegment(FeatureMatrixTest.MatrixA)));
            Assert.IsFalse(node.ExistsValue.Matches(null, new MutableSegment(FeatureMatrixTest.MatrixB)));

            var root = fs.Get<NodeFeature>("ROOT");
            Assert.IsTrue(root.ExistsValue.Matches(null, new MutableSegment(FeatureMatrixTest.MatrixA)));
            Assert.IsTrue(root.ExistsValue.Matches(null, new MutableSegment(FeatureMatrixTest.MatrixB)));
        }

        [Test]
        public void NodeVariable()
        {
            var fs = FeatureSetTest.GetTestSet();
            var node = fs.Get<NodeFeature>("Node2");
            var ctx = new RuleContext();

            Assert.IsTrue(node.VariableValue.Matches(ctx, new MutableSegment(FeatureMatrixTest.MatrixA)));
            Assert.IsTrue(ctx.VariableNodes.ContainsKey(node));

            Assert.IsTrue(node.VariableValue.Matches(ctx, new MutableSegment(FeatureMatrixTest.MatrixA)));
            Assert.IsFalse(node.VariableValue.Matches(ctx, new MutableSegment(FeatureMatrixTest.MatrixB)));
            Assert.IsFalse(node.VariableValue.Matches(ctx, new MutableSegment(FeatureMatrixTest.MatrixC)));

            foreach (var fv in node.VariableValue.CombineValues(ctx, null))
            {
                Assert.AreSame(FeatureMatrixTest.MatrixA[fv.Feature], fv);
            }

            var root = fs.Get<NodeFeature>("ROOT");
            Assert.IsTrue(root.VariableValue.Matches(ctx, new MutableSegment(FeatureMatrixTest.MatrixA)));
            Assert.IsTrue(ctx.VariableNodes.ContainsKey(root));

            Assert.IsTrue(root.VariableValue.Matches(ctx, new MutableSegment(FeatureMatrixTest.MatrixA)));
            Assert.IsFalse(root.VariableValue.Matches(ctx, new MutableSegment(FeatureMatrixTest.MatrixB)));
            Assert.IsFalse(root.VariableValue.Matches(ctx, new MutableSegment(FeatureMatrixTest.MatrixC)));

            foreach (var fv in root.VariableValue.CombineValues(ctx, null))
            {
                Assert.AreSame(FeatureMatrixTest.MatrixA[fv.Feature], fv);
            }
        }

        [Test]
        public void NodeNull()
        {
            var fs = FeatureSetTest.GetTestSet();
            var node = fs.Get<NodeFeature>("Node2");

            var values = node.NullValue.CombineValues(null, null);
            Assert.AreEqual(node.Children.Count(), values.Count());
            foreach (var child in values)
            {
                Assert.AreEqual(child.Feature.NullValue, child);
            }
        }

        [Test]
        public void ScalarNotEqual()
        {
            var fs = FeatureSetTest.GetTestSet();
            var sc = fs.Get<ScalarFeature>("sc");
            var scNe = sc.NotEqual(1);
            FeatureMatrix fm;

            fm = new FeatureMatrix(new FeatureValue[] { sc.Value(0) });
            Assert.IsTrue(scNe.Matches(null, new MutableSegment(fm)));

            fm = new FeatureMatrix(new FeatureValue[] { sc.Value(1) });
            Assert.IsFalse(scNe.Matches(null, new MutableSegment(fm)));

            fm = new FeatureMatrix(new FeatureValue[] { sc.Value(2) });
            Assert.IsTrue(scNe.Matches(null, new MutableSegment(fm)));

            fm = new FeatureMatrix(new FeatureValue[] {});
            Assert.IsTrue(scNe.Matches(null, new MutableSegment(fm)));
        }

        [Test]
        public void ScalarGreaterThan()
        {
            var fs = FeatureSetTest.GetTestSet();
            var sc = fs.Get<ScalarFeature>("sc");
            var scGT = sc.GreaterThan(1);
            FeatureMatrix fm;

            fm = new FeatureMatrix(new FeatureValue[] { sc.Value(0) });
            Assert.IsFalse(scGT.Matches(null, new MutableSegment(fm)));

            fm = new FeatureMatrix(new FeatureValue[] { sc.Value(1) });
            Assert.IsFalse(scGT.Matches(null, new MutableSegment(fm)));

            fm = new FeatureMatrix(new FeatureValue[] { sc.Value(2) });
            Assert.IsTrue(scGT.Matches(null, new MutableSegment(fm)));

            fm = new FeatureMatrix(new FeatureValue[] {});
            Assert.IsFalse(scGT.Matches(null, new MutableSegment(fm)));
        }

        [Test]
        public void ScalarLessThan()
        {
            var fs = FeatureSetTest.GetTestSet();
            var sc = fs.Get<ScalarFeature>("sc");
            var scLT = sc.LessThan(1);
            FeatureMatrix fm;

            fm = new FeatureMatrix(new FeatureValue[] { sc.Value(0) });
            Assert.IsTrue(scLT.Matches(null, new MutableSegment(fm)));

            fm = new FeatureMatrix(new FeatureValue[] { sc.Value(1) });
            Assert.IsFalse(scLT.Matches(null, new MutableSegment(fm)));

            fm = new FeatureMatrix(new FeatureValue[] { sc.Value(2) });
            Assert.IsFalse(scLT.Matches(null, new MutableSegment(fm)));

            fm = new FeatureMatrix(new FeatureValue[] {});
            Assert.IsFalse(scLT.Matches(null, new MutableSegment(fm)));
        }

        [Test]
        public void ScalarGreaterThanOrEqual()
        {
            var fs = FeatureSetTest.GetTestSet();
            var sc = fs.Get<ScalarFeature>("sc");
            var scGTE = sc.GreaterThanOrEqual(1);
            FeatureMatrix fm;

            fm = new FeatureMatrix(new FeatureValue[] { sc.Value(0) });
            Assert.IsFalse(scGTE.Matches(null, new MutableSegment(fm)));

            fm = new FeatureMatrix(new FeatureValue[] { sc.Value(1) });
            Assert.IsTrue(scGTE.Matches(null, new MutableSegment(fm)));

            fm = new FeatureMatrix(new FeatureValue[] { sc.Value(2) });
            Assert.IsTrue(scGTE.Matches(null, new MutableSegment(fm)));

            fm = new FeatureMatrix(new FeatureValue[] {});
            Assert.IsFalse(scGTE.Matches(null, new MutableSegment(fm)));
        }

        [Test]
        public void ScalarLessThanOrEqual()
        {
            var fs = FeatureSetTest.GetTestSet();
            var sc = fs.Get<ScalarFeature>("sc");
            var scLTE = sc.LessThanOrEqual(1);
            FeatureMatrix fm;
            MutableSegment seg;

            fm = new FeatureMatrix(new FeatureValue[] { sc.Value(0) });
            seg = new MutableSegment(fm);
            Assert.IsTrue(scLTE.Matches(null, seg));

            fm = new FeatureMatrix(new FeatureValue[] { sc.Value(1) });
            seg = new MutableSegment(fm);
            Assert.IsTrue(scLTE.Matches(null, seg));

            fm = new FeatureMatrix(new FeatureValue[] { sc.Value(2) });
            seg = new MutableSegment(fm);
            Assert.IsFalse(scLTE.Matches(null, seg));

            fm = new FeatureMatrix(new FeatureValue[] {});
            seg = new MutableSegment(fm);
            Assert.IsFalse(scLTE.Matches(null, seg));
        }

        [Test]
        public void ScalarAdd()
        {
            var sc = new ScalarFeature("sc", 0, 2);
            var scAdd = sc.Add(1);
            FeatureMatrix fm;
            MutableSegment seg;

            fm = new FeatureMatrix(new FeatureValue[] { sc.Value(0) });
            seg = new MutableSegment(fm);
            Assert.AreSame(scAdd.CombineValues(null, seg).First(), sc.Value(1));

            fm = new FeatureMatrix(new FeatureValue[] { sc.Value(1) });
            seg = new MutableSegment(fm);
            Assert.AreSame(scAdd.CombineValues(null, seg).First(), sc.Value(2));

            try
            {
                seg = new MutableSegment(FeatureMatrix.Empty);
                scAdd.CombineValues(null, seg).First();
                Assert.Fail("should have thrown exception combining with null value");
            }
            catch (InvalidScalarOpException)
            {
            }

            try
            {
                seg = new MutableSegment(new FeatureMatrix(new FeatureValue[] { sc.Value(2) }));
                scAdd.CombineValues(null, seg);
                Assert.Fail("should have thrown exception going out of range");
            }
            catch (ScalarValueRangeException)
            {
            }
        }

        [Test]
        public void ScalarSubtract()
        {
            var sc = new ScalarFeature("sc", 0, 2);
            var scSubtract = sc.Subtract(1);
            FeatureMatrix fm;
            MutableSegment seg;

            fm = new FeatureMatrix(new FeatureValue[] { sc.Value(1) });
            seg = new MutableSegment(fm);
            Assert.AreSame(scSubtract.CombineValues(null, seg).First(), sc.Value(0));

            fm = new FeatureMatrix(new FeatureValue[] { sc.Value(2) });
            seg = new MutableSegment(fm);
            Assert.AreSame(scSubtract.CombineValues(null, seg).First(), sc.Value(1));

            try
            {
                seg = new MutableSegment(FeatureMatrix.Empty);
                scSubtract.CombineValues(null, seg).First();
                Assert.Fail("should have thrown exception combining with null value");
            }
            catch (InvalidScalarOpException)
            {
            }

            try
            {
                seg = new MutableSegment(new FeatureMatrix(new FeatureValue[] { sc.Value(0) }));
                scSubtract.CombineValues(null, seg);
                Assert.Fail("should have thrown exception going out of range");
            }
            catch (ScalarValueRangeException)
            {
            }
        }
    }
}
