using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
            VariableValue = new VariableFeatureValue(this);
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
        private class VariableFeatureValue : AbstractFeatureValue
        {
            public VariableFeatureValue(Feature f)
                : base(f, "$" + f.Name)
            {
            }
        }

        public readonly FeatureValue NullValue;

        public readonly AbstractFeatureValue VariableValue;

        public static string FriendlyName<T>() where T : Feature
        {
            StringBuilder str = new StringBuilder(typeof(T).Name);
            str.Replace("Feature", "");
            return str.ToString().ToLowerInvariant();
        }

    }

    // AbstractFeatureValue defines all of the functionality for feature values.
    // It's separated from Feature Value only to allow us to enforce type
    // strictness with variable and non-variable FeatureValue.
    public abstract class AbstractFeatureValue : IComparable
    {
        public readonly Feature Feature;

        private readonly string _desc;

        protected AbstractFeatureValue(Feature feature, string desc)
        {
            Feature = feature;
            _desc = desc;
        }

        public int CompareTo(object obj)
        {
           var fv = obj as FeatureValue;
           if (fv != null)
           {
               if (Feature == fv.Feature)
               {
                   return _desc.CompareTo(fv._desc);
               }
               else
               {
                   return Feature.Name.CompareTo(fv.Feature.Name);
               }
           }
           else 
           {
               throw new ArgumentException();
           }
        }

        public override string ToString()
        {
            return _desc;
        }

        public static bool operator == (AbstractFeatureValue fvA, AbstractFeatureValue fvB)
        {
            if (object.ReferenceEquals(fvA, null))
            {
                return object.ReferenceEquals(fvA, fvB);
            }
            else
            {
                return fvA.Equals(fvB);
            }
        }

        public static bool operator != (AbstractFeatureValue fvA, AbstractFeatureValue fvB)
        {
            return !(fvA == fvB);
        }

        override public bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        override public int GetHashCode()
        {
            return base.GetHashCode();
        }
    }

    // FeatureValue is the class from which all non-variable feature value
    // types derive.
    public abstract class StaticFeatureValue : AbstractFeatureValue
    {
        protected StaticFeatureValue(Feature feature, string desc)
            : base(feature, desc)
        {
        }

        virtual public IEnumerable<FeatureValue> ToValueList()
        {
            return new FeatureValue[] { this as FeatureValue };
        }
    }

    public abstract class FeatureValue : StaticFeatureValue
    {
        protected FeatureValue(Feature feature, string desc)
            : base(feature, desc)
        {
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
        private readonly IEnumerable<Feature> _children;

        public NodeFeature(string name, IEnumerable<Feature> children)
            : base(name)
        {
            _children = children;
        }

        public StaticFeatureValue Value(FeatureMatrix fm)
        {
            // Return a real feature value if there are eny non-null children,
            // otherwise return the null value.
            foreach (var f in _children)
            {
                if (fm[f] != f.NullValue)
                {
                    return new NodeFeatureValue(this, fm);
                }
            }
            return this.NullValue;
        }

        private class NodeFeatureValue : StaticFeatureValue
        {
            private readonly FeatureMatrix _fm;
            private readonly NodeFeature _node;

            public NodeFeatureValue(NodeFeature f, IEnumerable<FeatureValue> fvList)
                : base(f, f.Name)
            {
                _fm = new FeatureMatrix(fvList);
                _node = Feature as NodeFeature;
            }

            override public IEnumerable<FeatureValue> ToValueList()
            {
                var list = new List<FeatureValue>();
                foreach (var f in _node._children)
                {
                    list.AddRange(_fm[f].ToValueList());
                }
                return list;
            }

            override public bool Equals(object obj)
            {
                // short-cut for one common case
                if (object.ReferenceEquals(this, obj))
                {
                    return true;
                }

                var fv = obj as NodeFeatureValue;
                if (fv == null)
                {
                    return base.Equals(obj);
                }
                else
                {
                    foreach (var feature in _node._children)
                    {
                        if (_fm[feature] != fv._fm[feature])
                        {
                            return false;
                        }
                    }
                    return true;
                }
            }

            override public int GetHashCode()
            {
                int rv = 0;
                foreach (var feature in _node._children)
                {
                    rv = unchecked(rv + _fm[feature].GetHashCode());
                }
                return rv;
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

    public class FeatureMatrix : IEnumerable<FeatureValue>
    {
        private readonly Dictionary<Feature, FeatureValue> _values = new Dictionary<Feature, FeatureValue>();

        public FeatureMatrix(IEnumerable<FeatureValue> values)
        {
            if (values == null)
            {
                throw new ArgumentNullException();
            }

            foreach (var val in values)
            {
                _values[val.Feature] = val;
            }
        }

        public FeatureMatrix(FeatureMatrix matrix)
            : this(matrix._values.Values)
        {
        }

        public readonly static FeatureMatrix Empty = new FeatureMatrix(new FeatureValue[] {});

#region IEnumerable(FeatureValue) members

        public IEnumerator<FeatureValue> GetEnumerator()
        {
            return GetEnumerator(false);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

#endregion

        public IEnumerator<FeatureValue> GetEnumerator(bool enumerateNullValues)
        {
            foreach (FeatureValue fv in _values.Values)
            {
                if (enumerateNullValues || fv != fv.Feature.NullValue)
                {
                    yield return fv;
                }
            }
            yield break;
        }

        public StaticFeatureValue this[Feature f]
        {
            get
            {
                if (f is NodeFeature)
                {
                    return (f as NodeFeature).Value(this);
                }
                else if (_values.ContainsKey(f))
                {
                    return _values[f];
                }
                return f.NullValue;
            }
        }

        public int Weight
        {
            get
            {
                return this.Count();
            }
        }

        public bool Equals(FeatureMatrix fm)
        {
            if (this.Weight != fm.Weight)
                return false;
            
            foreach (var fv in this)
            {
                if (fm[fv.Feature] != fv)
                    return false;
            }

            return true;
        }

        public override bool Equals(object obj)
        {
            if (obj is FeatureMatrix)
            {
                return Equals((FeatureMatrix)obj);
            }
            else
            {
                return false;
            }
        }

        public override int GetHashCode()
        {
            int hash = 0;
            foreach (var fv in this)
            {
                hash += fv.GetHashCode();
            }
            return hash;
        }

        public override string ToString()
        {
            StringBuilder str = new StringBuilder();
            str.Append("[");

            var list = new List<FeatureValue>(this);
            list.Sort();

            str.Append(String.Join(" ", list.ConvertAll(fv => fv.ToString()).ToArray()));

            str.Append("]");
            return str.ToString();
        }

    }
}
