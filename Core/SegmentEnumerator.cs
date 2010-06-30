using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Phonix
{
    public abstract class SegmentEnumerator : IEnumerator<Segment>
    {
        private readonly IMatrixMatcher _filter;

        protected internal LinkedListNode<MutableSegment> _node;
        protected internal LinkedListNode<MutableSegment> _startNode;
        protected internal LinkedListNode<MutableSegment> _lastNode;
        protected internal bool _beforeFirst = false;
        protected internal bool _afterLast = false;

        public abstract class Marker
        {
            protected internal Marker() {}
            internal abstract void Restore(SegmentEnumerator seg);
        }

        private sealed class MarkerImpl : Marker
        {
            private SegmentEnumerator _seg;
            private LinkedListNode<MutableSegment> _node;
            private bool _beforeFirst = false;
            private bool _afterLast = false;

            public MarkerImpl(SegmentEnumerator seg)
                : base()
            {
                _seg = seg;
                _node = seg._node;
                _beforeFirst = seg._beforeFirst;
                _afterLast = seg._afterLast;
            }

            internal override void Restore(SegmentEnumerator seg)
            {
                if (seg != _seg)
                {
                    throw new InvalidOperationException("cannot restore a different enumerator");
                }
                seg._node = _node;
                seg._beforeFirst = _beforeFirst;
                seg._afterLast = _afterLast;
            }
            
            public override bool Equals(object other)
            {
                var otherMark = other as MarkerImpl;
                if (otherMark == null)
                {
                    return false;
                }
                return _beforeFirst == otherMark._beforeFirst
                    && _afterLast == otherMark._afterLast
                    && _node == otherMark._node;
            }

            public override int GetHashCode()
            {
                return _beforeFirst.GetHashCode() ^ _afterLast.GetHashCode() ^ _node.GetHashCode();
            }
        }

        protected SegmentEnumerator(LinkedListNode<MutableSegment> node, IMatrixMatcher filter)
        {
            if (node == null)
            {
                throw new ArgumentNullException("node");
            }
            if (node.List == null)
            {
                throw new SegmentDeletedException();
            }

            _startNode = node;
            _lastNode = node.List.Last;
            _filter = filter;
            Reset();
        }

        public bool MoveNext()
        {
            if (!_beforeFirst)
            {
                _node = _node.Next;
            }
            while (_node != null && _filter != null && !_filter.Matches(null, _node.Value))
            {
                _node = _node.Next;
            }
            _beforeFirst = false;

            if (_node == null)
            {
                _afterLast = true;
                _node = _lastNode;
            }
            return !_afterLast;
        }

        public bool MovePrev()
        {
            if (!_afterLast)
            {
                _node = _node.Previous;
            }
            while (_node != null && _filter != null && !_filter.Matches(null, _node.Value))
            {
                _node = _node.Previous;
            }
            _afterLast = false;

            if (_node == null)
            {
                _beforeFirst = true;
                _node = _startNode.List.First;
            }

            return !_beforeFirst;
        }

        protected MutableSegment CurrentMutable
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

        public Segment Current
        {
            get
            {
                return CurrentMutable;
            }
        }

        object IEnumerator.Current
        {
            get
            {
                return Current;
            }
        }

        public void Reset()
        {
            _node = _startNode;
            _beforeFirst = true;
            _afterLast = false;
        }

        public void Dispose()
        {
        }

        public Marker Mark()
        {
            return new MarkerImpl(this);
        }

        public void Revert(Marker mark)
        {
            mark.Restore(this);
        }

        public IEnumerable<Segment> Span(Marker start, Marker end)
        {
            // this method should not change our state, so we save our current
            // state and restore it at the end
            var current = Mark();

            try
            {
                var list = new List<Segment>();

                // return an empty enumeration in case of equality
                if (start.Equals(end))
                {
                    return Enumerable.Empty<Segment>();
                }

                Revert(end);

                // get the expected end state
                bool endAfterLast = _afterLast;
                Segment endSeg = endAfterLast ? null : Current;

                Revert(start);

                bool hasCurrent;
                while ((hasCurrent = MoveNext()) && Current != endSeg)
                {
                    list.Add(Current);
                }
                if (hasCurrent)
                {
                    list.Add(Current);
                }
                if (!hasCurrent && !endAfterLast)
                {
                    // this should only happen if start is after end
                    throw new ArgumentOutOfRangeException("the start and end markers do not represent a valid range");
                }

                return list;
            }
            catch (InvalidOperationException ex)
            {
                throw new ArgumentOutOfRangeException("the start and end markers do not represent a valid range", ex);
            }
            finally
            {
                Revert(current);
            }
        }

        protected void CheckValid()
        {
            if (_beforeFirst)
            {
                throw new InvalidOperationException("The segment enumeration has passed the beginning of the set");
            }
            if (_afterLast)
            {
                throw new InvalidOperationException("The segment enumeration has passed the end of the set");
            }
        }
    }
}
