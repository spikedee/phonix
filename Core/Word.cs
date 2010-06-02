using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Phonix
{
    public class Word : IEnumerable<Segment>
    {
        private readonly LinkedList<MutableSegment> _list = new LinkedList<MutableSegment>();

        public Word(IEnumerable<FeatureMatrix> fms)
        {
            foreach (var fm in fms)
            {
                _list.AddLast(new MutableSegment(Tier.Segment, fm, new Segment[] {}));
            }
        }

        private class WordSlice : IWordSlice
        {
            private LinkedListNode<MutableSegment> _node;
            private readonly IMatrixMatcher _filter;

            public WordSlice(LinkedListNode<MutableSegment> node, IMatrixMatcher filter)
            {
                if (node == null)
                {
                    throw new ArgumentNullException();
                }
                _node = node;
                _filter = filter;
            }

            public MutableSegmentEnumerator GetMutableEnumerator()
            {
                return new MutableSegmentEnumerator(_node, _filter);
            }

            public SegmentEnumerator GetEnumerator()
            {
                return GetMutableEnumerator();
            }

            public override string ToString()
            {
                StringBuilder str = new StringBuilder();
                var currNode = _node;
                while (currNode != null)
                {
                    str.Append(currNode.Value.ToString());
                    currNode = currNode.Next;
                }
                return str.ToString();
            }
        }

        private class BoundarySegment : IRuleSegment
        {

            // note that the boundary segments are tricksy. since there are
            // only ever two instances, we reference compare against those
            // instances to determine our behavior.

            public bool Matches(RuleContext ctx, SegmentEnumerator segment)
            {
                if (this == Word.LeftBoundary)
                {
                    segment.MoveNext();
                    return !segment.MovePrev();
                }
                else if (this == Word.RightBoundary)
                {
                    bool match = !segment.MoveNext();
                    segment.MovePrev();
                    return match;
                }
                return false;
            }

             public void Combine(RuleContext ctx, MutableSegmentEnumerator segment)
             {
                 // nothing to do
             }

            public bool IsMatchOnlySegment { get { return true; } }
            public string MatchString { get { return "$"; } }
            public string CombineString { get { return ""; } }
        }

        public static IRuleSegment LeftBoundary = new BoundarySegment();

        public static IRuleSegment RightBoundary = new BoundarySegment();

        public IEnumerator<IWordSlice> GetSliceEnumerator(Direction dir)
        {
            return GetSliceEnumerator(dir, MatrixMatcher.AlwaysMatches);
        }

        public IEnumerator<IWordSlice> GetSliceEnumerator(Direction dir, IMatrixMatcher filter)
        {
            LinkedListNode<MutableSegment> currNode;
            if (dir == Direction.Rightward)
            {
                currNode = _list.First;
            }
            else
            {
                currNode = _list.Last;
            }

            while (currNode != null)
            {
                var nextNodePre = (dir == Direction.Rightward ? currNode.Next : currNode.Previous);
                RuleContext ctx = new RuleContext();

                if (filter == null || filter.Matches(ctx, currNode.Value))
                {
                    yield return new WordSlice(currNode, filter);
                }

                var nextNodePost = (dir == Direction.Rightward ? currNode.Next : currNode.Previous);

                if (currNode.List == null)
                {
                    // if currNode was detached, then we have to use the
                    // nextNode that was saved before we yielded.
                    currNode = nextNodePre;
                }
                else
                {
                    currNode = nextNodePost;
                }
            }

            yield break;
        }

#region IEnumerable(T) members

        public IEnumerator<Segment> GetEnumerator()
        {
            foreach (var seg in _list)
            {
                yield return seg;
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
            System.Text.StringBuilder str = new System.Text.StringBuilder();
            foreach (var fm in _list)
            {
                str.Append(fm.ToString());
            }
            return str.ToString();
        }
    }
}
