using System;
using System.Collections.Generic;

namespace Phonix
{
    public class NodeFeature : Feature
    {
        public readonly IEnumerable<Feature> Children;
        public readonly IMatchable ExistsValue;

        public IEnumerable<Feature> NonNodeDescendants
        {
            get
            {
                var flist = new List<Feature>();
                foreach (var f in Children)
                {
                    if (f is NodeFeature)
                    {
                        flist.AddRange((f as NodeFeature).NonNodeDescendants);
                    }
                    else
                    {
                        flist.Add(f);
                    }
                }
                return flist;
            }
        }

        public NodeFeature(string name, IEnumerable<Feature> children)
            : base(name)
        {
            Children = children;
            ExistsValue = new NodeExistsValue(this);
        }

        override protected IMatchCombine GetVariableValue()
        {
            return new NodeVariableValue(this);
        }

        override protected IMatchCombine GetNullValue()
        {
            return new NodeNullValue(this);
        }

        private class NodeExistsValue : AbstractFeatureValue, IMatchable
        {
            private readonly NodeFeature _node;

            public NodeExistsValue(NodeFeature feature)
                : base(feature, feature.Name)
            {
                _node = feature;
            }

            public bool Matches(RuleContext ctx, FeatureMatrix matrix)
            {
                return !_node.NullValue.Matches(ctx, matrix);
            }
        }

        private class NodeNullValue : AbstractFeatureValue, IMatchCombine
        {
            private readonly NodeFeature _node;

            public NodeNullValue(NodeFeature f)
                : base(f, "*" + f.Name)
            {
                _node = f;
            }

            public bool Matches(RuleContext ctx, FeatureMatrix matrix)
            {
                foreach (var feature in _node.Children)
                {
                    if (!feature.NullValue.Matches(ctx, matrix))
                    {
                        return false;
                    }
                }
                return true;
            }

            public IEnumerable<FeatureValue> GetValues(RuleContext ctx, FeatureMatrix matrix)
            {
                var list = new List<FeatureValue>();
                foreach (var feature in _node.Children)
                {
                    list.AddRange(feature.NullValue.GetValues(ctx, matrix));
                }
                return list;
            }
        }

        private class NodeVariableValue : AbstractFeatureValue, IMatchCombine
        {
            private readonly NodeFeature _node;

            public NodeVariableValue(NodeFeature feature)
                : base(feature, "$" + feature.Name)
            {
                _node = feature;
            }

            public bool Matches(RuleContext ctx, FeatureMatrix matrix)
            {
                if (ctx.VariableNodes.ContainsKey(_node))
                {
                    foreach (var fv in ctx.VariableNodes[_node])
                    {
                        if (fv != matrix[fv.Feature])
                        {
                            return false;
                        }
                    }
                }
                else
                {
                    var list = new List<FeatureValue>();
                    foreach (var feature in _node.NonNodeDescendants)
                    {
                        list.Add(matrix[feature]);
                    }
                    ctx.VariableNodes[_node] = list;
                }
                return true;
            }

            public IEnumerable<FeatureValue> GetValues(RuleContext ctx, FeatureMatrix matrix)
            {
                if (ctx.VariableNodes.ContainsKey(_node))
                {
                    return ctx.VariableNodes[_node];
                }
                else
                {
                    throw new InvalidOperationException("undefined variable used");
                }
            }
        }
    }
}
