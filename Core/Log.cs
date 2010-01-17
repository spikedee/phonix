using System;
using System.IO;
using System.Text;

namespace Phonix
{
    public enum Level
    {
        Error = 0,
        Warning = 1,
        Info = 2,
        Verbose = 3
    };

    public class Logger
    {
        public readonly Level LogLevel;
        public readonly Level ErrorLevel;
        public readonly TextWriter Writer;
        private readonly Phonology _phono;

        public Logger(Level logLevel, Level errorLevel, TextWriter writer, Phonology phono)
        {
            LogLevel = logLevel;
            ErrorLevel = errorLevel;
            Writer = writer;
            _phono = phono;
        }

        public void Start()
        {
            Trace.OnFeatureDefined += this.LogFeatureDefined;
            Trace.OnFeatureRedefined += this.LogFeatureRedefined;
            Trace.OnSymbolDefined += this.LogSymbolDefined;
            Trace.OnSymbolRedefined += this.LogSymbolRedefined;
            Trace.OnSymbolDuplicate += this.LogSymbolDuplicate;
            Trace.OnRuleDefined += this.LogRuleDefined;
            Trace.OnRuleRedefined += this.LogRuleRedefined;
            Trace.OnRuleEntered += this.LogRuleEntered;
            Trace.OnRuleExited += this.LogRuleExited;
            Trace.OnRuleApplied += this.LogRuleApplied;
            Trace.OnUndefinedVariableUsed += this.LogUndefinedVariableUsed;
        }

        public void Stop()
        {
            Trace.OnFeatureDefined -= this.LogFeatureDefined;
            Trace.OnFeatureRedefined -= this.LogFeatureRedefined;
            Trace.OnSymbolDefined -= this.LogSymbolDefined;
            Trace.OnSymbolRedefined -= this.LogSymbolRedefined;
            Trace.OnSymbolDuplicate -= this.LogSymbolDuplicate;
            Trace.OnRuleDefined -= this.LogRuleDefined;
            Trace.OnRuleRedefined -= this.LogRuleRedefined;
            Trace.OnRuleEntered -= this.LogRuleEntered;
            Trace.OnRuleExited -= this.LogRuleExited;
            Trace.OnRuleApplied -= this.LogRuleApplied;
            Trace.OnUndefinedVariableUsed -= this.LogUndefinedVariableUsed;

            Writer.Flush();
        }

        public void Log(Level level, string format, params object[] args)
        {
            string message = String.Format(format, args);
            if (this.LogLevel >= level)
            {
                Writer.WriteLine(message);
            }
            if (this.ErrorLevel >= level)
            {
                throw new FatalWarningException(message);
            }
        }

        private void LogFeatureDefined(Feature f)
        {
            Log(Level.Info, "{0} {1} defined", f.GetType().Name, f.Name);
        }

        private void LogFeatureRedefined(Feature old, Feature newer)
        {
            Log(Level.Warning, "{0} {1} overwrites {2} {3}", newer.GetType().Name, newer.Name, old.GetType().Name, old.Name);
        }

        private void LogSymbolDefined(Symbol s)
        {
            Log(Level.Info, "symbol {0} defined", s);
        }

        private void LogSymbolRedefined(Symbol old, Symbol newer)
        {
            Log(Level.Warning, "symbol {0} changed from {1} to {2}", old.Label, old.FeatureMatrix, newer.FeatureMatrix);
        }

        private void LogSymbolDuplicate(Symbol old, Symbol newer)
        {
            Log(Level.Warning, "symbol {0} is identical to symbol {1}", newer, old);
        }

        private void LogRuleDefined(Rule r)
        {
            Log(Level.Info, "rule {0} defined", r);
        }

        private void LogRuleRedefined(Rule old, Rule newer)
        {
            Log(Level.Warning, "rule {0} redefined", newer, old);
        }

        private void LogRuleEntered(Rule rule, Word word)
        {
            Log(Level.Verbose, "rule {0} entered", rule);
        }

        private void LogRuleExited(Rule rule, Word word)
        {
            Log(Level.Verbose, "rule {0} exited", rule);
        }

        private void LogRuleApplied(Rule rule, Word word, IWordSlice slice)
        {
            // since this method is potentially expensive, skip it if we're not
            // going to log anything
            if (this.LogLevel < Level.Info)
            {
                return;
            }

            Log(Level.Info, "rule {0} applied", rule);

            // match until we get to the current segment, so that we can
            // display which segment was acted upon
            var ctx = new RuleContext();
            var seg = rule.Segments.GetEnumerator();
            FeatureMatrix current = null;
            try
            {
                var pos = slice.GetEnumerator();
                while (seg.MoveNext() && seg.Current is ContextSegment)
                {
                    seg.Current.Matches(ctx, pos);
                }

                current = pos.MoveNext() ? pos.Current : null;
            }
            catch (SegmentDeletedException)
            {
                // this occurs when we try to get the enumerator for a deleted
                // segment. this exception (and only this exception) can be
                // safely swallowed
            }

            foreach (var fm in word)
            {
                var str = new StringBuilder();
                if (current != null && fm == current)
                {
                    str.Append("> ");
                }
                else
                {
                    str.Append("  ");
                }

                Symbol symbol;
                try
                {
                    symbol = _phono.SymbolSet.Spell(fm);
                }
                catch (SpellingException)
                {
                    symbol = Symbol.Unknown;
                }
                str.Append("{0} : {1}");

                Log(Level.Info, str.ToString(), symbol, fm);
            }
        }

        private void LogUndefinedVariableUsed(AbstractFeatureValue fv)
        {
            Log(Level.Warning, "variable {0} used in rule action without appearing in rule context", fv);
        }
    }
}
