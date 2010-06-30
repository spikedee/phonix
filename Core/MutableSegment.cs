using System;
using System.Collections.Generic;

namespace Phonix
{
    public class MutableSegment : Segment
    {
        public MutableSegment(Tier tier, FeatureMatrix fm, IEnumerable<Segment> children)
            : base(tier, fm, children)
        {
        }

        public MutableSegment(FeatureMatrix fm)
            : this(Tier.Segment, fm, new Segment[] {})
        {
        }
        
        public MutableSegment(Tier tier)
            : this(tier, FeatureMatrix.Empty, new Segment[] {})
        {
        }

        new public FeatureMatrix Matrix
        {
            get 
            { 
                return base.Matrix;
            }
            set 
            { 
                if (value == null)
                {
                    throw new ArgumentNullException("value for Segment.Matrix");
                }
                base.Matrix = value;
            }
        }

        new public IEnumerable<Segment> Children
        {
            get 
            { 
                return base.Children;
            }
            set 
            {
                base.Children = value;
            }
        }

        new public void Detach(Tier detachedTier)
        {
            base.Detach(detachedTier);
        }
    }
}
