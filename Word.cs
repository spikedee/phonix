using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Phonix
{
    public interface SegmentEnumerator : IEnumerator<FeatureMatrix>
    {
        bool IsFirst { get; }
        bool IsLast { get; }
        new FeatureMatrix Current { get; }
    }

    public interface MutableSegmentEnumerator : SegmentEnumerator
    {
        void InsertBefore(FeatureMatrix fm);
        void InsertAfter(FeatureMatrix fm);
        void Delete();
        new FeatureMatrix Current { get; set; }
    }

    public interface IWordSlice
    {
        SegmentEnumerator GetEnumerator();
        MutableSegmentEnumerator GetMutableEnumerator();
    }

    public enum Direction
    {
        Rightward,
        Leftward
    }

    public class Word : IEnumerable<FeatureMatrix>
    {
        private readonly LinkedList<FeatureMatrix> _list;

        public Word(IEnumerable<FeatureMatrix> fms)
        {
            _list = new LinkedList<FeatureMatrix>( fms );
        }

        private class WordSegment : MutableSegmentEnumerator
        {
            private LinkedListNode<FeatureMatrix> _node;
            private LinkedListNode<FeatureMatrix> _startNode;
            private LinkedListNode<FeatureMatrix> _lastNode;
            private readonly IMatrixMatcher _filter;

            private bool _valid = false;

            public bool NoAdvance = false;

            public WordSegment(LinkedListNode<FeatureMatrix> node, IMatrixMatcher filter)
            {
                _node = _startNode = node;
                _filter = filter;
            }

#region IEnumerator<FeatureMatrix> members

            public bool MoveNext()
            {
                var currNode = _node;
                if (_valid && !NoAdvance)
                {
                    _node = _node.Next;
                }
                while (_node != null && !_filter.Matches(_node.Value))
                {
                    _node = _node.Next;
                }

                _valid = true;
                if (_node == null)
                {
                    _lastNode = currNode;
                    _valid = false;
                }

                NoAdvance = false;
                return _valid;
            }

            public FeatureMatrix Current
            {
                get 
                { 
                    CheckValid();
                    return _node.Value; 
                }
                set 
                {
                    CheckValid();
                    _node.Value = value; 
                }
            }

            public void Reset()
            {
                _node = _startNode;
                _valid = false;
            }

            object IEnumerator.Current
            {
                get { return Current; }
            }

            public void Dispose()
            {
            }

#endregion

#region SegmentEnumerator members

            public bool IsFirst
            {
                get 
                { 
                    CheckValid();
                    return _node.List.First == _node; 
                }
            }

            public bool IsLast
            {
                get 
                { 
                    CheckValid();
                    return _node.List.Last == _node; 
                }
            }

#endregion

#region MutableSegmentEnumerator members

            public void InsertBefore(FeatureMatrix fm)
            {
                if (_node == _startNode && !_valid)
                {
                    // can't insert before you've started iterating
                    throw new InvalidOperationException();
                }
                if (_node == null && _lastNode != null)
                {
                    // we've gone past the end, but we can still add "before",
                    // which means after the last node.
                    _lastNode.List.AddAfter(_lastNode, fm);
                }
                else
                {
                    _node.List.AddBefore(_node, fm);
                }
            }

            public void InsertAfter(FeatureMatrix fm)
            {
                if (_node == null)
                {
                    throw new InvalidOperationException();
                }
                if (_node == _startNode && !_valid)
                {
                    // we haven't started yet, so "after" actually means before
                    // our start node. we also move the start node back
                    _node.List.AddBefore(_node, fm);
                    _node = _startNode = _node.Previous;
                }
                else
                {
                    _node.List.AddAfter(_node, fm);
                }
            }

            public void Delete()
            {
                CheckValid();
                var deleted = _node;
                _node = _node.Next;
                deleted.List.Remove(deleted);
                _valid = false;
            }

#endregion

            private void CheckValid()
            {
                if (!_valid)
                {
                    throw new InvalidOperationException();
                }
            }

        }

        private class WordSlice : IWordSlice
        {
            private LinkedListNode<FeatureMatrix> _node;
            private readonly IMatrixMatcher _filter;

            public WordSlice(LinkedListNode<FeatureMatrix> node, IMatrixMatcher filter)
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
                return new WordSegment(_node, _filter);
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

            public bool Matches(SegmentEnumerator segment)
            {
                if (this == Word.LeftBoundary)
                {
                    if (segment.MoveNext() && segment.IsFirst)
                    {
                        // here we cheat and do evil wicked things. Set
                        // NoAdvance to true, which makes the next iteration
                        // return the same segment again. This is a hack to
                        // avoid setting up a real lookahead.
                        WordSegment wordSegment = (WordSegment)segment;
                        wordSegment.NoAdvance = true;
                        return true;
                    }
                    return false;
                }
                else if (this == Word.RightBoundary)
                {
                    return segment.IsLast && !segment.MoveNext();
                }
                return false;
            }

             public void Combine(MutableSegmentEnumerator segment)
             {
                 // nothing to do
             }
        }

        public static IRuleSegment LeftBoundary = new BoundarySegment();

        public static IRuleSegment RightBoundary = new BoundarySegment();

        public IEnumerator<IWordSlice> GetSliceEnumerator(Direction dir)
        {
            return GetSliceEnumerator(dir, MatrixMatcher.AlwaysMatches);
        }

        public IEnumerator<IWordSlice> GetSliceEnumerator(Direction dir, IMatrixMatcher filter)
        {
            LinkedListNode<FeatureMatrix> currNode;
            if (dir == Direction.Rightward)
            {
                currNode = _list.First;
            }
            else
            {
                currNode = _list.Last;
            }
            if (filter == null)
            {
                filter = MatrixMatcher.AlwaysMatches;
            }

            while (currNode != null)
            {
                var nextNodePre = (dir == Direction.Rightward ? currNode.Next : currNode.Previous);

                if (filter.Matches(currNode.Value))
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

        public IEnumerator<FeatureMatrix> GetEnumerator()
        {
            return _list.GetEnumerator();
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
