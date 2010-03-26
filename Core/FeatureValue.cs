using System;
using System.Collections;
using System.Collections.Generic;

namespace Phonix
{

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
    }

    public abstract class FeatureValue : AbstractFeatureValue, IMatchCombine
    {
        protected FeatureValue(Feature feature, string desc)
            : base(feature, desc)
        {
        }

        new public Feature Feature
        {
            get
            {
                return base.Feature;
            }
        }

        virtual public bool Matches(RuleContext ctx, FeatureMatrix matrix)
        {
            return this == matrix[this.Feature];
        }

        virtual public IEnumerable<FeatureValue> GetValues(RuleContext ctx, FeatureMatrix matrix)
        {
            return new FeatureValue[] { this };
        }
    }

}
