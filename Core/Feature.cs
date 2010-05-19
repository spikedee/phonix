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

        protected virtual IFeatureValue GetVariableValue()
        {
            return new VariableFeatureValue(this);
        }

        protected virtual IFeatureValue GetNullValue()
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
        private class VariableFeatureValue : AbstractFeatureValue, IFeatureValue
        {
            public VariableFeatureValue(Feature f)
                : base(f, "$" + f.Name)
            {
            }

            public bool Matches(RuleContext ctx, Segment segment)
            {
                if (ctx == null)
                {
                    throw new InvalidOperationException("context cannot be null for match with variables");
                }
                if (!ctx.VariableFeatures.ContainsKey(Feature))
                {
                    ctx.VariableFeatures[Feature] = segment.Matrix[Feature];
                }
                return segment.Matrix[Feature] == ctx.VariableFeatures[Feature];
            }

            public IEnumerable<FeatureValue> CombineValues(RuleContext ctx, MutableSegment segment)
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

            public FeatureValue ToFeatureValue()
            {
                throw new NotImplementedException();
            }
        }

        public readonly IFeatureValue NullValue;

        public readonly IFeatureValue VariableValue;

        public static string FriendlyName<T>() where T : Feature
        {
            StringBuilder str = new StringBuilder(typeof(T).Name);
            str.Replace("Feature", "");
            return str.ToString().ToLowerInvariant();
        }
    }
}
