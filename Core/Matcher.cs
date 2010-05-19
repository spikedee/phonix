using System.Collections.Generic;

namespace Phonix
{
    public interface IMatrixMatcher : IEnumerable<IMatchable>
    {
        bool Matches(RuleContext ctx, Segment segment);
    }

    public interface IMatchable
    {
        bool Matches(RuleContext ctx, Segment segment);
    }

}
