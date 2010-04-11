using System.Collections.Generic;

namespace Phonix
{
    public interface IMatrixMatcher : IEnumerable<IMatchable>
    {
        bool Matches(RuleContext ctx, FeatureMatrix matrix);
    }

    public interface IMatchable
    {
        bool Matches(RuleContext ctx, FeatureMatrix matrix);
    }

    public interface IMatchCombine : IMatchable, ICombinable
    {
        // this exists just to provide a convenient way to specify both
        // behaviors
    }
}
