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
                _children.Clear();

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
            Children = children;
        }

        public bool TryFindAncestor(Tier ancestorTier, out Segment ancestor)
        {
            ancestor = null;
            if (!this.Tier.HasAncestor(ancestorTier))
            {
                return false;
            }

            foreach (var parent in Parents)
            {
                if (parent.Tier == ancestorTier)
                {
                    ancestor = parent;
                    return true;
                }
                else if (parent.Tier.HasAncestor(ancestorTier) && parent.TryFindAncestor(ancestorTier, out ancestor))
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
            Segment ignored;
            return TryFindAncestor(tier, out ignored);
        }

        public bool TryFindDescendant(Tier descendantTier, out Segment descendant)
        {
            descendant = null;
            if (!this.Tier.HasDescendant(descendantTier))
            {
                return false;
            }

            foreach (var child in Children)
            {
                if (child.Tier == descendantTier)
                {
                    descendant = child;
                    return true;
                }
                else if (child.Tier.HasDescendant(descendantTier) && child.TryFindDescendant(descendantTier, out descendant))
                {
                    return true;
                }
            }
            return false;
        }

        public Segment FindDescendant(Tier tier)
        {
            Segment descendant;
            if (TryFindDescendant(tier, out descendant))
            {
                return descendant;
            }
            throw new InvalidOperationException(String.Format("no ancestor found on {0} tier", tier));
        }

        public bool HasDescendant(Tier tier)
        {
            Segment ignored;
            return TryFindDescendant(tier, out ignored);
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
