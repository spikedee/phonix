using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Phonix
{
    public partial class Word : IEnumerable<Segment>
    {
        public Word(IEnumerable<FeatureMatrix> fms)
        {
            using (var iter = fms.GetEnumerator())
            {
                if (iter.MoveNext())
                {
                    var node = new Word.Node(this, new MutableSegment(Tier.Segment, iter.Current, new Segment[] {}));
                    Last = First = node;
                }

                while (iter.MoveNext())
                {
                    Last.InsertAfter(new MutableSegment(Tier.Segment, iter.Current, new Segment[] {}));
                }
            }
        }

        internal Node First
        {
            get;
            private set;
        }

        internal Node Last
        {
            get;
            private set;
        }

        public IEnumerable<WordSlice> Slice(Direction dir)
        {
            return Slice(dir, MatrixMatcher.AlwaysMatches);
        }

        public IEnumerable<WordSlice> Slice(Direction dir, IMatrixMatcher filter)
        {
            for (var currNode = (dir == Direction.Rightward ? First : Last);
                     currNode != null;
                     currNode = (dir == Direction.Rightward ? currNode.Next : currNode.Prev))
            {
                // skip over deleted segments
                if (currNode.Deleted)
                {
                    continue;
                }

                if (filter == null || filter.Matches(null, currNode.Segment))
                {
                    yield return new WordSlice(currNode, filter);
                }
            }

            yield break;
        }

#region IEnumerable(T) members

        public IEnumerator<Segment> GetEnumerator()
        {
            for (var node = First; node != null; node = node.Next)
            {
                yield return node.Segment;
            }
            yield break;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

#endregion

        public override string ToString()
        {
            var str = new StringBuilder();
            foreach (var seg in this)
            {
                str.Append(seg.Matrix.ToString());
            }
            return str.ToString();
        }
    }
}
