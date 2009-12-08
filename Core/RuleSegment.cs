using System;
using System.Collections.Generic;
using System.Linq;

namespace Phonix
{
    public interface IRuleSegment
    {

        /* This function should return true if this matches the segment(s) at
         * the next position(s) of the enumeration. When this is called, the
         * iterator will be at the start of the enumeration or on the last
         * segment matched by the previous IRuleSegment, so implementations
         * should call segment.MoveNext() zero or more times (depending on how
         * many segments from the input they consume) _before_ testing
         * segment.Current.
         */
        bool Matches(RuleContext ctx, SegmentEnumerator segment);

        /* This function consumes zero or more segments from the enumeration,
         * modifying them as appropriate. The directions about MoveNext()
         * mentioned for Matches also applies here.
         */
         void Combine(RuleContext ctx, MutableSegmentEnumerator segment); 
    
    }

    public class FeatureMatrixSegment : IRuleSegment
    {
        private readonly IMatrixMatcher _match;
        private readonly IMatrixCombiner _combo;

        public FeatureMatrixSegment(IMatrixMatcher match, IMatrixCombiner combo)
        {
            _match = match;
            _combo = combo;
        }

        public bool Matches(RuleContext ctx, SegmentEnumerator pos)
        {
            if (pos.MoveNext() && _match.Matches(ctx, pos.Current))
            {
                return true;
            }
            return false;
        }

        public void Combine(RuleContext ctx, MutableSegmentEnumerator pos)
        {
            pos.MoveNext();
            pos.Current = _combo.Combine(ctx, pos.Current);
        }
    }

    public class DeletingSegment : IRuleSegment
    {
        private IMatrixMatcher _match;

        public DeletingSegment(IMatrixMatcher match)
        {
            _match = match;
        }

        public bool Matches(RuleContext ctx, SegmentEnumerator pos)
        {
            if (pos.MoveNext() && _match.Matches(ctx, pos.Current))
            {
                return true;
            }
            return false;
        }

        public void Combine(RuleContext ctx, MutableSegmentEnumerator pos)
        {
            pos.MoveNext();
            pos.Delete();
        }
    }
        
    public class InsertingSegment : IRuleSegment
    {
        private IMatrixCombiner _insert;

        public InsertingSegment(IMatrixCombiner insert)
        {
            _insert = insert;
        }

        public bool Matches(RuleContext ctx, SegmentEnumerator pos)
        {
            // always return true, but take nothing from the input list
            return true;
        }

        public void Combine(RuleContext ctx, MutableSegmentEnumerator pos)
        {
            pos.InsertAfter(_insert.Combine(ctx, FeatureMatrix.Empty));
            pos.MoveNext();
        }
    }

    public class StepSegment : IRuleSegment
    {
        public bool Matches(RuleContext ctx, SegmentEnumerator pos)
        {
            return pos.MoveNext();
        }

        public void Combine(RuleContext ctx, MutableSegmentEnumerator pos)
        {
            Matches(ctx, pos);
        }
    }

    public class BackstepSegment : IRuleSegment
    {
        // this class moves the cursor one step backwards, but doesn't actually
        // do anything to the segments.

        public bool Matches(RuleContext ctx, SegmentEnumerator pos)
        {
            // we only want to return false if we are already before the
            // beginning. This requires us to advance, check IsFirst, then move
            // back

            if (pos.MoveNext() && pos.IsFirst)
            {
                pos.MovePrev();
                return false;
            }
            pos.MovePrev();
            pos.MovePrev();
            return true;
        }

        public void Combine(RuleContext ctx, MutableSegmentEnumerator pos)
        {
            Matches(ctx, pos);
        }
    }
}
