using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Phonix
{

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

        public FeatureValue this[Feature f]
        {
            get
            {
                if (f is NodeFeature)
                {
                    throw new InvalidOperationException("Can't directly access node values");
                }
                else if (_values.ContainsKey(f))
                {
                    return _values[f];
                }
                else
                {
                    return f.NullValue;
                }
            }
        }

        public IEnumerable<FeatureValue> this[NodeFeature node]
        {
            get
            {
                var list = new List<FeatureValue>();
                foreach (var child in node.Children)
                {
                    if (child is NodeFeature)
                    {
                        list.AddRange(this[child as NodeFeature]);
                    }
                    else
                    {
                        list.Add(this[child]);
                    }
                }
                return list;
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
