using System;
using System.Collections;
using System.Collections.Generic;

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
            RuleContext ctx = new RuleContext();
            if (!_beforeFirst)
            {
                _node = _node.Next;
            }
            while (_node != null && _filter != null && !_filter.Matches(ctx, _node.Value))
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
            RuleContext ctx = new RuleContext();

            if (!_afterLast)
            {
                _node = _node.Previous;
            }
            while (_node != null && _filter != null && !_filter.Matches(ctx, _node.Value))
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

        protected void CheckValid()
        {
            if (_beforeFirst || _afterLast)
            {
                throw new InvalidOperationException();
            }
        }
    }
}
