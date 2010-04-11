using System;
using System.Collections.Generic;

namespace Phonix
{
    public class ScalarFeature : Feature
    {
        private readonly List<ScalarFeatureValue> _list = new List<ScalarFeatureValue>();
        public readonly int? Min;
        public readonly int? Max;

        public ScalarFeature(string name) 
            : base(name)
        {
        }

        public ScalarFeature(string name, int min, int max)
            : base(name)
        {
            if (min > max)
            {
                throw new ArgumentException("min must be less than or equal to max");
            }
            Min = min;
            Max = max;
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
            if (Min != null && Max != null)
            {
                if (val < Min || val > Max)
                {
                    throw new ScalarValueRangeException(this, val);
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
            return new DelegateMatcher((ctx, fm) => { int val; return !GetIntValue(fm, out val) || val != cmp; }, 
                    String.Format("{0}<>{1}", Name, cmp));
        }

        public IMatchable GreaterThan(int cmp)
        {
            return new DelegateMatcher((ctx, fm) => { int val; return GetIntValue(fm, out val) && val > cmp; },
                    String.Format("{0}>{1}", Name, cmp));
        }

        public IMatchable LessThan(int cmp)
        {
            return new DelegateMatcher((ctx, fm) => { int val; return GetIntValue(fm, out val) && val < cmp; },
                    String.Format("{0}<{1}", Name, cmp));
        }

        public IMatchable GreaterThanOrEqual(int cmp)
        {
            return new DelegateMatcher((ctx, fm) => { int val; return GetIntValue(fm, out val) && val >= cmp; },
                    String.Format("{0}>={1}", Name, cmp));
        }

        public IMatchable LessThanOrEqual(int cmp)
        {
            return new DelegateMatcher((ctx, fm) => { int val; return GetIntValue(fm, out val) && val <= cmp; },
                    String.Format("{0}<={1}", Name, cmp));
        }

        public ICombinable Add(int addend)
        {
            DelegateCombiner.ComboFunc func = (ctx, fm) =>
            {
                int val;
                if (!GetIntValue(fm, out val))
                {
                    throw new InvalidScalarOpException(this, "cannot add to a null scalar value");
                }
                return new FeatureValue[] { this.Value(val + addend) };
            };
            return new DelegateCombiner(func, String.Format("{0}=+{1}", Name, addend));
        }

        public ICombinable Subtract(int diminuend)
        {
            DelegateCombiner.ComboFunc func = (ctx, fm) =>
            {
                int val;
                if (!GetIntValue(fm, out val))
                {
                    throw new InvalidScalarOpException(this, "cannot subtract from a null scalar value");
                }
                return new FeatureValue[] { this.Value(val - diminuend) };
            };
            return new DelegateCombiner(func, String.Format("{0}=-{1}", Name, diminuend));
        }
    }
}
