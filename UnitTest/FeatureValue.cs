using System;
using System.Collections.Generic;
using Phonix;

namespace Phonix.UnitTest
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

            Assert.IsTrue(un.Value.Matches(null, FeatureMatrixTest.MatrixA));
            Assert.IsFalse(un.Value.Matches(null, FeatureMatrixTest.MatrixB));
        }

        [Test]
        public void Binary()
        {
            var fs = FeatureSetTest.GetTestSet();
            var bn = fs.Get<BinaryFeature>("bn");

            Assert.IsTrue(bn.PlusValue.Matches(null, FeatureMatrixTest.MatrixA));
            Assert.IsFalse(bn.PlusValue.Matches(null, FeatureMatrixTest.MatrixC));

            Assert.IsTrue(bn.MinusValue.Matches(null, FeatureMatrixTest.MatrixC));
            Assert.IsFalse(bn.MinusValue.Matches(null, FeatureMatrixTest.MatrixA));
        }

        [Test]
        public void Scalar()
        {
            var fs = FeatureSetTest.GetTestSet();
            var sc = fs.Get<ScalarFeature>("sc");

            // check that scalar feature values are the same every invocation
            Assert.AreSame(sc.Value(2), sc.Value(2));
            Assert.AreNotEqual(sc.Value(1), sc.Value(2));

            Assert.IsTrue(sc.Value(1).Matches(null, FeatureMatrixTest.MatrixA));
            Assert.IsFalse(sc.Value(1).Matches(null, FeatureMatrixTest.MatrixB));
        }

        [Test]
        public void Variable()
        {
            var fs = FeatureSetTest.GetTestSet();
            var bn = fs.Get<BinaryFeature>("bn");
            var ctx = new RuleContext();

            Assert.IsTrue(bn.VariableValue.Matches(ctx, FeatureMatrixTest.MatrixA));
            Assert.IsTrue(ctx.VariableFeatures.ContainsKey(bn));
            Assert.AreSame(ctx.VariableFeatures[bn], FeatureMatrixTest.MatrixA[bn]);

            Assert.IsTrue(bn.VariableValue.Matches(ctx, FeatureMatrixTest.MatrixB));
            Assert.IsFalse(bn.VariableValue.Matches(ctx, FeatureMatrixTest.MatrixC));
        }

        [Test]
        public void NodeExists()
        {
            var fs = FeatureSetTest.GetTestSet();
            var node = fs.Get<NodeFeature>("Node2");

            Assert.IsTrue(node.ExistsValue.Matches(null, FeatureMatrixTest.MatrixA));
            Assert.IsFalse(node.ExistsValue.Matches(null, FeatureMatrixTest.MatrixB));
        }

        [Test]
        public void NodeVariable()
        {
            var fs = FeatureSetTest.GetTestSet();
            var node = fs.Get<NodeFeature>("Node2");
            var ctx = new RuleContext();

            Assert.IsTrue(node.VariableValue.Matches(ctx, FeatureMatrixTest.MatrixA));
            Assert.IsTrue(ctx.VariableNodes.ContainsKey(node));

            Assert.IsTrue(node.VariableValue.Matches(ctx, FeatureMatrixTest.MatrixA));
            Assert.IsFalse(node.VariableValue.Matches(ctx, FeatureMatrixTest.MatrixB));
            Assert.IsFalse(node.VariableValue.Matches(ctx, FeatureMatrixTest.MatrixC));
        }
    }
}
