using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Phonix
{
    public class Symbol : IMatrixMatcher, IMatrixCombiner
    {
        public static readonly Symbol Unknown = new Symbol("[?]", FeatureMatrix.Empty);

        public readonly string Label;
        public readonly FeatureMatrix FeatureMatrix;

        public Symbol(string label, FeatureMatrix fm)
        {
            if (label == null || fm == null)
            {
                throw new ArgumentNullException();
            }

            Label = label;
            FeatureMatrix = fm;
        }

        public override string ToString()
        {
            return Label;
        }

        public bool Matches(RuleContext ctx, Segment segment)
        {
            return FeatureMatrix.Equals(segment.Matrix);
        }

        public void Combine(RuleContext ctx, MutableSegment segment)
        {
            segment.Matrix = FeatureMatrix;
        }

#region IEnumerable members

        IEnumerator<IMatchable> IEnumerable<IMatchable>.GetEnumerator()
        {
            foreach (FeatureValue fv in this)
            {
                yield return fv;
            }
            yield break;
        }

        IEnumerator<ICombinable> IEnumerable<ICombinable>.GetEnumerator()
        {
            foreach (FeatureValue fv in this)
            {
                yield return fv;
            }
            yield break;
        }

        public IEnumerator<FeatureValue> GetEnumerator()
        {
            return FeatureMatrix.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
#endregion
    }
}
