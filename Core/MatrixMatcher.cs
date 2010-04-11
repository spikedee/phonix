using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Text;

namespace Phonix
{
    public class MatrixMatcher : IMatrixMatcher
    {
        public static MatrixMatcher AlwaysMatches = new MatrixMatcher(new IMatchable[] {});
        public static MatrixMatcher NeverMatches = new MatrixMatcher(new IMatchable[] {});

        private readonly IEnumerable<IMatchable> _values;

        public MatrixMatcher(FeatureMatrix fm)
        {
            var list = new List<IMatchable>();
            var iter = fm.GetEnumerator(true);
            while (iter.MoveNext())
            {
                list.Add(iter.Current);
            }
            iter.Dispose();

            _values = list;
        }

        public MatrixMatcher(IEnumerable<IMatchable> values)
        {
            var list = new List<IMatchable>(values);
            _values = list;
        }

        public MatrixMatcher(IEnumerable<IMatchCombine> values)
            : this(new List<IMatchCombine>(values).ConvertAll<IMatchable>(v => v))
        {
        }

#region IEnumerable<IMatchable> members

        public IEnumerator<IMatchable> GetEnumerator()
        {
            return _values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

#endregion

        virtual public bool Matches(RuleContext ctx, FeatureMatrix matrix)
        {
            if (this == NeverMatches)
            {
                return false;
            }

            foreach (IMatchable match in this)
            {
                if (!match.Matches(ctx, matrix))
                {
                    return false;
                }
            }
            return true;
        }

        override public string ToString()
        {
            var str = new StringBuilder();
            str.Append("[");
            str.Append(String.Join(" ", new List<IMatchable>(_values).ConvertAll(s => s.ToString()).ToArray()));
            str.Append("]");
            return str.ToString();
        }
    }
}
