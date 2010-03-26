using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Phonix
{
    public abstract class Feature
    {
        private static int _instanceCount = 0;
        internal static int InstanceCount
        {
            get { return _instanceCount; }
        }

        public readonly int Index;

        public readonly string Name;

        protected Feature(string name)
        {
            Name = name;
            NullValue = GetNullValue();
            VariableValue = GetVariableValue();
            Index = Interlocked.Increment(ref _instanceCount);
        }

        protected virtual IMatchCombine GetVariableValue()
        {
            return new VariableFeatureValue(this);
        }

        protected virtual IMatchCombine GetNullValue()
        {
            return new NullFeatureValue(this);
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

            public IEnumerable<FeatureValue> GetValues(RuleContext ctx, FeatureMatrix matrix)
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
                    // the user tried to use a variable that hasn't been
                    // defined. Warn them and leave the variable alone.
                    throw new UndefinedFeatureVariableException(this);
                }
            }
        }

        public readonly IMatchCombine NullValue;

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

        public BinaryFeature(string name) 
            : base(name)
        {
            PlusValue = new BinaryFeatureValue(this, "+");
            MinusValue = new BinaryFeatureValue(this, "-");
        }
    }

    public class ScalarFeature : Feature
    {
        private readonly List<ScalarFeatureValue> _list = new List<ScalarFeatureValue>();
        private readonly int? _min;
        private readonly int? _max;

        public ScalarFeature(string name) 
            : base(name)
        {
        }

        public ScalarFeature(string name, int min, int max)
            : base(name)
        {
            _min = min;
            _max = max;
        }

        private class ScalarFeatureValue : FeatureValue
        {
            public readonly int Value;

            public ScalarFeatureValue(ScalarFeature f, int val)
                : base(f, String.Format("{0}={1}", f.Name, val))
            {
                Value = val;
            }
        }

        public FeatureValue Value(int val)
        {
            if (_min != null && _max != null)
            {
                if (val < _min || val > _max)
                {
                    throw new ScalarValueRangeException(this.Name, _min.Value, _max.Value, val);
                }
            }

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

        private bool GetIntValue(FeatureMatrix fm, out int val)
        {
            var fv = fm[this];
            if (fv != this.NullValue)
            {
                val = (fv as ScalarFeatureValue).Value;
                return true;
            }
            else
            {
                val = -1;
                return false;
            }
        }

        public IMatchable NotEqual(int cmp)
        {
            // Note: !GetIntValue is true when the value is null. A null value
            // should evaluated to TRUE under the NotEqual operator.
            return new DelegateMatcher((ctx, fm) => { int val; return !GetIntValue(fm, out val) || val != cmp; });
        }

        public IMatchable GreaterThan(int cmp)
        {
            return new DelegateMatcher((ctx, fm) => { int val; return GetIntValue(fm, out val) && val > cmp; });
        }

        public IMatchable LessThan(int cmp)
        {
            return new DelegateMatcher((ctx, fm) => { int val; return GetIntValue(fm, out val) && val < cmp; });
        }

        public IMatchable GreaterThanOrEqual(int cmp)
        {
            return new DelegateMatcher((ctx, fm) => { int val; return GetIntValue(fm, out val) && val >= cmp; });
        }

        public IMatchable LessThanOrEqual(int cmp)
        {
            return new DelegateMatcher((ctx, fm) => { int val; return GetIntValue(fm, out val) && val <= cmp; });
        }

        public ICombinable Add(int addend)
        {
            DelegateCombiner.ComboFunc func = (ctx, fm) =>
            {
                int val;
                if (!GetIntValue(fm, out val))
                {
                    throw new InvalidScalarOpException("cannot add to a null scalar value");
                }
                return new FeatureValue[] { this.Value(val + addend) };
            };
            return new DelegateCombiner(func);
        }

        public ICombinable Subtract(int diminuend)
        {
            DelegateCombiner.ComboFunc func = (ctx, fm) =>
            {
                int val;
                if (!GetIntValue(fm, out val))
                {
                    throw new InvalidScalarOpException("cannot subtract from a null scalar value");
                }
                return new FeatureValue[] { this.Value(val - diminuend) };
            };
            return new DelegateCombiner(func);
        }
    }

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

    public class FeatureSet : IEnumerable<Feature>
    {
        private Dictionary<string, Feature> _dict = new Dictionary<string, Feature>();

        public event Action<Feature> FeatureDefined;
        public event Action<Feature, Feature> FeatureRedefined;

        public FeatureSet()
        {
            // add null event handlers
            FeatureDefined += (f) => {};
            FeatureRedefined += (f1, f2) => {};
        }

        private void AddImpl(string name, Feature f)
        {
            if (_dict.ContainsKey(name))
            {
                var dup = _dict[name];
                if (dup.GetType() != f.GetType())
                {
                    FeatureRedefined(dup, f);
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
            FeatureDefined(f);
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
