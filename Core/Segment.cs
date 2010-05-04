using System;
using System.Collections.Generic;

namespace Phonix
{
    public class Segment
    {
        public readonly Tier Tier;

        // Segment.Matrix is assignable but cannot be null
        private FeatureMatrix _fm;
        public FeatureMatrix Matrix
        {
            get 
            { 
                return _fm; 
            }
            set 
            { 
                if (value == null)
                {
                    throw new ArgumentNullException("value for Segment.Matrix");
                }
                _fm = value;
            }
        }

        // Segment.Parents is an *unordered* list which cannot be assigned (all
        // parents are assigned when a parent has its children assigned)
        private readonly List<Segment> _parents = new List<Segment>();
        public IEnumerable<Segment> Parents
        {
            get
            {
                return _parents;
            }
        }

        // Segment.Children is an ordered list. The list is validated and all
        // bidirectional links are created during assignment.
        private readonly List<Segment> _children = new List<Segment>();
        public IEnumerable<Segment> Children
        {
            get 
            { 
                return _children; 
            }
            set 
            {
                // unlink all old children
                foreach (var child in _children)
                {
                    child._parents.Remove(this);
                }

                // empty our child list
                _children.RemoveRange(0, _children.Count);

                // add and validate all new children
                foreach (var child in value)
                {
                    // ensure that this child segment belongs to an appropriate tier
                    if (!this.Tier.HasChild(child.Tier))
                    {
                        throw new InvalidOperationException(
                                String.Format("invalid tier link: {0} does not govern {1}", this.Tier, child.Tier));
                    }

                    _children.Add(child);
                    child._parents.Add(this);
                }
            }
        }

        public Segment(Tier tier, FeatureMatrix fm, IEnumerable<Segment> children)
        {
            Matrix = fm;
            Tier = tier;
            Children = children;
        }
    }
}
