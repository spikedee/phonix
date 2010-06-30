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

        protected internal Segment(Tier tier, FeatureMatrix fm, IEnumerable<Segment> children)
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

        public IEnumerable<Segment> FindAncestors(Tier tier)
        {
            var ancestors = new List<Segment>();
            foreach (var parent in Parents)
            {
                if (parent.Tier == tier)
                {
                    ancestors.Add(parent);
                }
                else
                {
                    ancestors.AddRange(parent.FindAncestors(tier));
                }
            }
            return ancestors.Distinct();
        }

        public Segment FirstAncestor(Tier tier)
        {
            try
            {
                return FindAncestors(tier).First();
            }
            catch (InvalidOperationException ex)
            {
                throw new InvalidOperationException(String.Format("no ancestors on {0} tier", tier), ex);
            }
        }

        public bool HasAncestor(Tier tier)
        {
            return FindAncestors(tier).Count() > 0;
        }

        public IEnumerable<Segment> FindDescendants(Tier tier)
        {
            var descendants = new List<Segment>();
            foreach (var child in Children)
            {
                if (child.Tier == tier)
                {
                    descendants.Add(child);
                }
                else
                {
                    descendants.AddRange(child.FindDescendants(tier));
                }
            }
            return descendants.Distinct();
        }

        public Segment FirstDescendant(Tier tier)
        {
            try
            {
                return FindDescendants(tier).First();
            }
            catch (InvalidOperationException ex)
            {
                throw new InvalidOperationException(String.Format("no descendants on {0} tier", tier), ex);
            }
        }

        public bool HasDescendant(Tier tier)
        {
            return FindDescendants(tier).Count() > 0;
        }

        protected void Detach(Tier detachedTier)
        {
            var removedParents = new List<Segment>();

            foreach (var parent in Parents)
            {
                if (parent.Tier == detachedTier || parent.Tier.HasAncestor(detachedTier))
                {
                    parent._children.Remove(this);
                    removedParents.Add(parent);
                }
            }
            foreach (var removed in removedParents)
            {
                _parents.Remove(removed);
            }
        }
    }
}
