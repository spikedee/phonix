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

        public readonly FeatureValue NullValue;

        public static string FriendlyName<T>() where T : Feature
        {
            StringBuilder str = new StringBuilder(typeof(T).Name);
            str.Replace("Feature", "");
            return str.ToString().ToLowerInvariant();
        }

    }

    public abstract class FeatureValue : IComparable
    {
        public readonly Feature Feature;

        private readonly string _desc;

        protected FeatureValue(Feature feature, string desc)
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

        public readonly static FeatureMatrix Empty = 
            new FeatureMatrix(new FeatureValue[] {});

#region IEnumerable(T) members

        public virtual IEnumerator<FeatureValue> GetEnumerator()
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

        public FeatureValue this[Feature f]
        {
            get
            {
                if (_values.ContainsKey(f))
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

    public interface IMatrixMatcher : IEnumerable<FeatureValue>
    {
        bool Matches(FeatureMatrix matrix);
    }

    public interface IMatrixCombiner : IEnumerable<FeatureValue>
    {
        FeatureMatrix Combine(FeatureMatrix matrix);
    }

    public class MatrixMatcher : IMatrixMatcher
    {
        public static MatrixMatcher AlwaysMatches = new MatrixMatcher(FeatureMatrix.Empty);

        private readonly FeatureMatrix _matrix;

        public MatrixMatcher(FeatureMatrix fm)
        {
            _matrix = fm;
        }

#region IEnumerable<FeatureValue> members

        public IEnumerator<FeatureValue> GetEnumerator()
        {
            return _matrix.GetEnumerator(true);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

#endregion

        public bool Matches(FeatureMatrix matrix)
        {
            if (matrix == null)
            {
                return false;
            }

            foreach (FeatureValue fv in this)
            {
                if (matrix[fv.Feature] != fv)
                {
                    return false;
                }
            }
            return true;
        }
    }

    public class MatrixCombiner : IMatrixCombiner
    {
        public static MatrixCombiner NullCombiner = new MatrixCombiner(FeatureMatrix.Empty);

        private readonly FeatureMatrix _matrix;

        public MatrixCombiner(FeatureMatrix fm)
        {
            _matrix = fm;
        }

#region IEnumerable<FeatureValue> members

        public IEnumerator<FeatureValue> GetEnumerator()
        {
            return _matrix.GetEnumerator(true);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

#endregion

        public FeatureMatrix Combine(FeatureMatrix matrix)
        {
            if (this == NullCombiner)
            {
                return matrix;
            }

            List<FeatureValue> comboValues = new List<FeatureValue>();
            comboValues.AddRange(matrix);
            comboValues.AddRange(this);

            return new FeatureMatrix(comboValues);
        }

    }
}
