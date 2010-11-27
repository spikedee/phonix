using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Phonix
{
    public abstract class SegmentEnumerator : IEnumerator<Segment>
    {
        internal SegmentEnumerator(Word.Node node, IMatrixMatcher filter)
        {
            if (node == null)
            {
                throw new ArgumentNullException("node");
            }
            if (node.Deleted)
            {
                throw new SegmentDeletedException();
            }

            _startNode = node;
            _lastNode = node.Word.Last;
            _filter = filter;
            Reset();
        }

        private readonly IMatrixMatcher _filter;

        internal Word.Node _node;
        internal Word.Node _startNode;
        internal Word.Node _lastNode;
        protected bool _beforeFirst = false;
        protected bool _afterLast = false;

        public abstract class Marker
        {
            protected internal Marker() {}
            internal abstract void Restore(SegmentEnumerator seg);
        }

        private sealed class MarkerImpl : Marker
        {
            private SegmentEnumerator _seg;
            private Word.Node _node;
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

        public bool MoveNext()
        {
            if (!_beforeFirst)
            {
                _node = _node.Next;
            }
            while (_node != null && _filter != null && !_filter.Matches(null, _node.Segment))
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
                _node = _node.Prev;
            }
            while (_node != null && _filter != null && !_filter.Matches(null, _node.Segment))
            {
                _node = _node.Prev;
            }
            _afterLast = false;

            if (_node == null)
            {
                _beforeFirst = true;
                _node = _startNode.Word.First;
            }

            return !_beforeFirst;
        }

        protected MutableSegment CurrentMutable
        {
            get 
            { 
                CheckValid();
                return _node.Segment;
            }
            set 
            {
                CheckValid();
                _node.Segment = value; 
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
            if (_node.Deleted)
            {
                throw new SegmentDeletedException();
            }
        }
    }
}
