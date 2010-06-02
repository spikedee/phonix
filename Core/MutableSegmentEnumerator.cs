using System; using System.Collections.Generic;

namespace Phonix
{
    public sealed class MutableSegmentEnumerator : SegmentEnumerator, IEnumerator<MutableSegment>
    {
        internal MutableSegmentEnumerator(LinkedListNode<MutableSegment> node, IMatrixMatcher filter)
            : base(node, filter)
        {
        }

        new public MutableSegment Current
        {
            get
            {
                return CurrentMutable;
            }
            set
            {
                CurrentMutable = value;
            }
        }

        public void InsertBefore(MutableSegment seg)
        {
            if (_node == _startNode && _beforeFirst)
            {
                // can't insert before you've started iterating
                throw new InvalidOperationException();
            }
            else if (_node == _lastNode && _afterLast)
            {
                // we've gone past the end, but we can still add "before",
                // which means after the last node.
                _lastNode.List.AddAfter(_lastNode, seg);
            }
            else
            {
                _node.List.AddBefore(_node, seg);
            }
        }

        public void InsertAfter(MutableSegment seg)
        {
            if (_node == _lastNode &&  _afterLast)
            {
                throw new InvalidOperationException();
            }
            else if (_node == _startNode && _beforeFirst)
            {
                // we haven't started yet, so "after" actually means before
                // our start node. we also move the start node back
                _node.List.AddBefore(_node, seg);
                _node = _startNode = _node.Previous;
            }
            else
            {
                _node.List.AddAfter(_node, seg);
            }
        }

        public void Delete()
        {
            CheckValid();
            var deleted = _node;
            _node = _node.Next;
            deleted.List.Remove(deleted);
            _beforeFirst = true;
        }
    }
}
