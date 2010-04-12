
using System;
using System.Collections.Generic;
using System.Linq;
using Phonix;

namespace Phonix.Test
{
    using NUnit.Framework;

    [TestFixture]
    public class MatrixMatcherTest
    {
        [Test]
        public void Ctor()
        {
            var test = new MatrixMatcher(FeatureMatrixTest.MatrixA);
            Assert.AreEqual(FeatureMatrixTest.MatrixA.Weight + 1, test.Count());

            bool hasNull = false;
            foreach (FeatureValue fv in test)
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
        public void Match()
        {
            var test = new MatrixMatcher(FeatureMatrixTest.MatrixA);
            var test2 = new MatrixMatcher(FeatureMatrixTest.MatrixB);

            Assert.IsTrue(test.Matches(null, FeatureMatrixTest.MatrixA), "test matches A");
            Assert.IsFalse(test.Matches(null, FeatureMatrixTest.MatrixB), "test matches B");
            Assert.IsFalse(test.Matches(null, FeatureMatrix.Empty), "test matches empty");

            Assert.IsFalse(test2.Matches(null, FeatureMatrixTest.MatrixA), "test2 matches A");
            Assert.IsTrue(test2.Matches(null, FeatureMatrixTest.MatrixB), "test2 matches B");
            Assert.IsFalse(test2.Matches(null, FeatureMatrix.Empty), "test2 matches empty");

            Assert.IsTrue(MatrixMatcher.AlwaysMatches.Matches(null, FeatureMatrixTest.MatrixA), "ALWAYS matches A");
            Assert.IsTrue(MatrixMatcher.AlwaysMatches.Matches(null, FeatureMatrixTest.MatrixB), "ALWAYS matches B");
            Assert.IsTrue(MatrixMatcher.AlwaysMatches.Matches(null, FeatureMatrix.Empty), "ALWAYS matches empty");
        }

        [Test]
        public void MatchVariable()
        {
            var fs = FeatureSetTest.GetTestSet();
            var un = fs.Get<UnaryFeature>("un");
            var sc = fs.Get<ScalarFeature>("sc");
            var test = new MatrixMatcher(new IMatchable[] { un.VariableValue, sc.VariableValue });
            var ctx = new RuleContext();

            Assert.IsTrue(test.Matches(ctx, FeatureMatrixTest.MatrixA), "test matches A");
            Assert.IsTrue(ctx.VariableFeatures.ContainsKey(un), "context has un value");
            Assert.IsTrue(ctx.VariableFeatures.ContainsKey(sc), "context has sc value");
            Assert.AreSame(FeatureMatrixTest.MatrixA[un], ctx.VariableFeatures[un], "context un equals A un");
            Assert.AreSame(FeatureMatrixTest.MatrixA[sc], ctx.VariableFeatures[sc], "context sc equals A sc");

            Assert.IsFalse(test.Matches(ctx, FeatureMatrixTest.MatrixB), "test matches B");
            Assert.IsFalse(test.Matches(ctx, FeatureMatrix.Empty), "test matches empty");
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException))]
        public void MatchVariableException()
        {
            var fs = FeatureSetTest.GetTestSet();
            var un = fs.Get<UnaryFeature>("un");
            var sc = fs.Get<ScalarFeature>("sc");
            var test = new MatrixMatcher(new IMatchable[] { un.VariableValue, sc.VariableValue });

            // this should throw InvalidOperationException since the context is
            // null and there are variables
            test.Matches(null, FeatureMatrixTest.MatrixA);
        }

        [Test]
        public void MatchNode()
        {
            var fs = FeatureSetTest.GetTestSet();
            var node = fs.Get<NodeFeature>("Node1");
            var test = new MatrixMatcher(new IMatchable[] { node.ExistsValue });

            Assert.IsTrue(test.Matches(null, FeatureMatrixTest.MatrixA));

            var node2 = fs.Get<NodeFeature>("Node2");
            var test2 = new MatrixMatcher(new IMatchable[] { node2.ExistsValue });

            Assert.IsFalse(test2.Matches(null, FeatureMatrixTest.MatrixB));
        }

        [Test]
        public void MatchNodeVariable()
        {
            var fs = FeatureSetTest.GetTestSet();
            var node = fs.Get<NodeFeature>("Node1");
            var test = new MatrixMatcher(new IMatchable[] { node.VariableValue });
            var ctx = new RuleContext();

            Assert.IsTrue(test.Matches(ctx, FeatureMatrixTest.MatrixA), "test matches A");
            Assert.IsTrue(ctx.VariableNodes.ContainsKey(node), "context has node value");
            Assert.AreEqual(FeatureMatrixTest.MatrixA[node], ctx.VariableNodes[node], "context node equals A node");

            Assert.IsFalse(test.Matches(ctx, FeatureMatrixTest.MatrixB), "test matches B");
            Assert.IsFalse(test.Matches(ctx, FeatureMatrix.Empty), "test matches empty");
        }

    }
}
