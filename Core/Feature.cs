using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Phonix
{
    public abstract class Feature
    {
        public readonly string Name;

        protected Feature(string name)
        {
            Name = name;
            NullValue = new NullFeatureValue(this);
            VariableValue = GetVariable();
        }

        protected virtual IMatchCombine GetVariable()
        {
            return new VariableFeatureValue(this);
        }

        public override string ToString()
        {
            return Name;
        }

        private class NullFeatureValue : FeatureValue
        {
            public NullFeatureValue(Feature f)
                : base(f, "*" + f.Name)
            {
            }
        }

        // VariableFeatureValue is derived from AbstractFeatureValue to keep it
        // from being put into a FeatureMatrix or other containers that should
        // only contain non-variable values.
        private class VariableFeatureValue : AbstractFeatureValue, IMatchCombine
        {
            public VariableFeatureValue(Feature f)
                : base(f, "$" + f.Name)
            {
            }

            public bool Matches(RuleContext ctx, FeatureMatrix matrix)
            {
                if (ctx == null)
                {
                    throw new InvalidOperationException("context cannot be null for match with variables");
                }
                if (!ctx.VariableFeatures.ContainsKey(Feature))
                {
                    ctx.VariableFeatures[Feature] = matrix[Feature];
                }
                return matrix[Feature] == ctx.VariableFeatures[Feature];
            }

            public IEnumerable<FeatureValue> GetValues(RuleContext ctx)
            {
                if (ctx == null)
                {
                    throw new InvalidOperationException("context cannot be null for combine with variables");
                }
                if (ctx.VariableFeatures.ContainsKey(Feature))
                {
                    return new FeatureValue[] { ctx.VariableFeatures[Feature] };
                }
                else
                {
                    // the user tried to set a variable that hasn't been
                    // defined. Warn them and leave the variable alone.
                    Trace.UndefinedVariableUsed(this);
                    return new FeatureValue[] {};
                }
            }
        }

        public readonly FeatureValue NullValue;

        public readonly IMatchCombine VariableValue;

        public static string FriendlyName<T>() where T : Feature
        {
            StringBuilder str = new StringBuilder(typeof(T).Name);
            str.Replace("Feature", "");
            return str.ToString().ToLowerInvariant();
        }
    }

    public class UnaryFeature : Feature
    {
        public UnaryFeature(string name)
            : base(name)
        {
            Value = new UnaryFeatureValue(this);
        }

        private class UnaryFeatureValue : FeatureValue
        {
            public UnaryFeatureValue(UnaryFeature f)
                : base(f, f.Name)
            {}
        }

        public readonly FeatureValue Value;
    }

    public class BinaryFeature : Feature
    {
        private class BinaryFeatureValue : FeatureValue
        {
            public BinaryFeatureValue(BinaryFeature f, string prefix)
                : base(f, prefix + f.Name)
            {
            }
        }

        public readonly FeatureValue PlusValue;
        public readonly FeatureValue MinusValue;

        public BinaryFeature(string name, params string[] altNames) 
            : base(name)
        {
            PlusValue = new BinaryFeatureValue(this, "+");
            MinusValue = new BinaryFeatureValue(this, "-");
        }
    }

    public class ScalarFeature : Feature
    {
        public ScalarFeature(string name, params string[] altNames) 
            : base(name)
        {
        }

        private class ScalarFeatureValue : FeatureValue
        {
            public ScalarFeatureValue(ScalarFeature f, int val)
                : base(f, String.Format("{0}={1}", f.Name, val))
            {
                Value = val;
            }

            public readonly int Value;
        }

        private readonly List<ScalarFeatureValue> _list = new List<ScalarFeatureValue>();

        public FeatureValue Value(int val)
        {
            foreach (var fv in _list)
            {
                if (fv.Value == val)
                {
                    return fv;
                }
            }

            var scalarVal = new ScalarFeatureValue(this, val);
            _list.Add(scalarVal);

            return scalarVal;
        }

    }

    public class NodeFeature : Feature
    {
        public readonly IEnumerable<Feature> Children;

        public readonly IMatchable ExistsValue;

        public NodeFeature(string name, IEnumerable<Feature> children)
            : base(name)
        {
            Children = children;
            ExistsValue = new NodeExistsValue(this);
        }

        override protected IMatchCombine GetVariable()
        {
            return new NodeVariableValue(this);
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
                foreach (var feature in _node.Children)
                {
                    if (matrix[feature] != feature.NullValue)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        // this class is only intended to be used as the VariableValue for
        // NodeFeature objects
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
                    foreach (var feature in _node.Children)
                    {
                        list.Add(matrix[feature]);
                    }
                    ctx.VariableNodes[_node] = list;
                }
                return true;
            }

            public IEnumerable<FeatureValue> GetValues(RuleContext ctx)
            {
                if (ctx.VariableNodes.ContainsKey(_node))
                {
                    return ctx.VariableNodes[_node];
                }
                else
                {
                    Trace.UndefinedVariableUsed(this);
                    return new FeatureValue[] {};
                }
            }
        }
    }

    public class FeatureSet : IEnumerable<Feature>
    {
        private Dictionary<string, Feature> _dict = new Dictionary<string, Feature>();

        private void AddImpl(string name, Feature f)
        {
            if (_dict.ContainsKey(name))
            {
                var dup = _dict[name];
                if (dup.GetType() != f.GetType())
                {
                    Trace.FeatureRedefined(dup, f);
                    _dict[f.Name] = f;
                }
            }
            else
            {
                _dict.Add(name, f);
            }
        }

        public void Add(Feature f)
        {
            if (f == null)
            {
                throw new ArgumentNullException("f");
            }
            Trace.FeatureDefined(f);
            AddImpl(f.Name, f);
        }

        public bool Has<TFeature>(string name) where TFeature: Feature
        {
            return _dict.ContainsKey(name) && _dict[name] is TFeature;
        }

        public TFeature Get<TFeature>(string name) where TFeature : Feature
        {
            if (!_dict.ContainsKey(name))
            {
                throw new FeatureNotFoundException(name);
            }

            TFeature f = _dict[name] as TFeature;
            if (f == null)
            {
                throw new FeatureTypeException(name, Feature.FriendlyName<TFeature>());
            }

            return f;
        }

#region IEnumerable<Feature> methods

        public IEnumerator<Feature> GetEnumerator()
        {
            foreach (var pair in _dict)
            {
                if (pair.Key.Equals(pair.Value.Name))
                {
                    yield return pair.Value;
                }
            }
            yield break;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

#endregion

    }
}