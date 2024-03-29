using System;
using System.Collections;
using System.Collections.Generic;

namespace Phonix
{
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

        public bool Has<T>(string name) where T: Feature
        {
            return _dict.ContainsKey(name) && _dict[name] is T;
        }

        public T Get<T>(string name) where T: Feature
        {
            if (!_dict.ContainsKey(name))
            {
                throw new FeatureNotFoundException(name);
            }

            T f = _dict[name] as T;
            if (f == null)
            {
                throw new FeatureTypeException(name, typeof(T).Name);
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
