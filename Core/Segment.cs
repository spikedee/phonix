using System;
using System.Collections.Generic;
using System.Linq;

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
            protected set 
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
            protected set 
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

        protected Segment(Tier tier, FeatureMatrix fm, IEnumerable<Segment> children)
        {
            if (fm == null)
            {
                throw new ArgumentNullException("fm");
            }
            if (tier == null)
            {
                throw new ArgumentNullException("tier");
            }
            if (children == null)
            {
                throw new ArgumentNullException("children");
            }
            Matrix = fm;
            Tier = tier;
            _children.AddRange(children);
        }

        public bool TryFindAncestor(Tier tier, out Segment ancestor)
        {
            if (!this.Tier.HasAncestor(tier))
            {
                throw new ArgumentException(String.Format("tier {0} is not an ancestor if {1}", tier, this.Tier));
            }

            ancestor = null;
            foreach (var parent in Parents)
            {
                if (parent.Tier == tier)
                {
                    ancestor = parent;
                    return true;
                }
                else if (parent.TryFindAncestor(tier, out ancestor))
                {
                    return true;
                }
            }
            return false;
        }

        public Segment FindAncestor(Tier tier)
        {
            Segment ancestor;
            if (TryFindAncestor(tier, out ancestor))
            {
                return ancestor;
            }
            throw new InvalidOperationException(String.Format("no ancestor found on {0} tier", tier));
        }

        public bool HasAncestor(Tier tier)
        {
            return Parents.Any(p => p.Tier == tier) || Parents.Any(p => p.HasAncestor(tier));
        }

        protected void Detach()
        {
            foreach (var parent in Parents)
            {
                parent._children.Remove(this);
            }
            _parents.Clear();
        }

    }
}
