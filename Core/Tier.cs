using System;
using System.Collections.Generic;
using System.Linq;

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
        static public readonly Tier Onset = new Tier("onset", new Tier[] { Segment });
        static public readonly Tier Nucleus = new Tier("nucleus", new Tier[] { Segment });
        static public readonly Tier Coda = new Tier("coda", new Tier[] { Segment });
        static public readonly Tier Rime = new Tier("rime", new Tier[] { Nucleus, Coda });
        static public readonly Tier Syllable = new Tier("syllable", new Tier[] { Onset, Rime });

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
        }

        public bool HasChild(Tier tier)
        {
            return _children.Contains(tier);
        }

        public bool HasParent(Tier tier)
        {
            return _parents.Contains(tier);
        }

        public bool HasAncestor(Tier tier)
        {
            return HasParent(tier) || Parents.Any(p => p.HasAncestor(tier));
        }

        public bool HasDescendant(Tier tier)
        {
            return HasChild(tier) || Children.Any(c => c.HasDescendant(tier));
        }

        override public string ToString()
        {
            return Name;
        }
    }
}
