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

        override protected IFeatureValue GetVariableValue()
        {
            return new NodeVariableValue(this);
        }

        override protected IFeatureValue GetNullValue()
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

            public bool Matches(RuleContext ctx, Segment segment)
            {
                return !_node.NullValue.Matches(ctx, segment);
            }
        }

        private class NodeNullValue : AbstractFeatureValue, IFeatureValue
        {
            private readonly NodeFeature _node;

            public NodeNullValue(NodeFeature f)
                : base(f, "*" + f.Name)
            {
                _node = f;
            }

            public bool Matches(RuleContext ctx, Segment segment)
            {
                foreach (var feature in _node.Children)
                {
                    if (!feature.NullValue.Matches(ctx, segment))
                    {
                        return false;
                    }
                }
                return true;
            }

            public IEnumerable<FeatureValue> CombineValues(RuleContext ctx, MutableSegment segment)
            {
                var list = new List<FeatureValue>();
                foreach (var feature in _node.Children)
                {
                    list.AddRange(feature.NullValue.CombineValues(ctx, segment));
                }
                return list;
            }

            public FeatureValue ToFeatureValue()
            {
                throw new NotImplementedException();
            }
        }

        private class NodeVariableValue : AbstractFeatureValue, IFeatureValue
        {
            private readonly NodeFeature _node;

            public NodeVariableValue(NodeFeature feature)
                : base(feature, "$" + feature.Name)
            {
                _node = feature;
            }

            public bool Matches(RuleContext ctx, Segment segment)
            {
                if (ctx.VariableNodes.ContainsKey(_node))
                {
                    foreach (var fv in ctx.VariableNodes[_node])
                    {
                        if (fv != segment.Matrix[fv.Feature])
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
                        list.Add(segment.Matrix[feature]);
                    }
                    ctx.VariableNodes[_node] = list;
                }
                return true;
            }

            public IEnumerable<FeatureValue> CombineValues(RuleContext ctx, MutableSegment segment)
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

            public FeatureValue ToFeatureValue()
            {
                throw new NotImplementedException();
            }
        }
    }
}
