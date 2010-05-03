using System;
using System.Collections.Generic;

namespace Phonix
{
    public class Tier
    {
        public readonly string Name;

        private readonly List<Tier> _children = new List<Tier>();
        private readonly List<Tier> _parents = new List<Tier>();

        public IEnumerable<Tier> Children { get { return _children; } }
        public IEnumerable<Tier> Parents { get { return _parents; } }

        static public readonly Tier Segment = new Tier("segment", new Tier[] {});

        public Tier(string name, IEnumerable<Tier> children)
        {
            Name = name;

            // set up parent-child relationships
            foreach (var child in children)
            {
                if (child._parents.Contains(this))
                {
                    continue;
                }
                this._children.Add(child);
                child._parents.Add(this);
            }

            CheckCircularRelationships(this);
        }

        private void CheckCircularRelationships(Tier tier)
        {
            foreach (var child in tier.Children)
            {
                if (child == this)
                {
                    throw new InvalidOperationException("circular relationship detected");
                }
                CheckCircularRelationships(child);
            }
        }

        public bool HasChild(Tier tier)
        {
            return _children.Contains(tier);
        }

        public bool HasParent(Tier tier)
        {
            return _parents.Contains(tier);
        }

        override public string ToString()
        {
            return Name;
        }
    }
}
