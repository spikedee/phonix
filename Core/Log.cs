using System;
using System.IO;
using System.Text;

namespace Phonix
{
    public class Log
    {
        public enum Level
        {
            Error = 0,
            Warning = 1,
            Info = 2,
            Verbose = 3
        };

        public readonly Level LogLevel;
        public readonly Level ErrorLevel;
        public readonly TextWriter Writer;
        private readonly Phonology _phono;

        public Log(Level logLevel, Level errorLevel, TextWriter writer, Phonology phono)
        {
            LogLevel = logLevel;
            ErrorLevel = errorLevel;
            Writer = writer;
            _phono = phono;
        }

        public void Start()
        {
            if (LogLevel >= Level.Warning)
            {
                _phono.FeatureSet.FeatureRedefined += this.LogFeatureRedefined;
                _phono.SymbolSet.SymbolRedefined += this.LogSymbolRedefined;
                _phono.SymbolSet.SymbolDuplicate += this.LogSymbolDuplicate;
                _phono.RuleSet.RuleRedefined += this.LogRuleRedefined;
                _phono.RuleSet.UndefinedVariableUsed += this.LogUndefinedVariableUsed;
                _phono.RuleSet.ScalarValueRangeViolation += this.LogScalarValueRangeViolation;
                _phono.RuleSet.InvalidScalarValueOp += this.LogInvalidScalarValueOp;
            }

            if (LogLevel >= Level.Info)
            {
                _phono.FeatureSet.FeatureDefined += this.LogFeatureDefined;
                _phono.SymbolSet.SymbolDefined += this.LogSymbolDefined;
                _phono.RuleSet.RuleDefined += this.LogRuleDefined;
                _phono.RuleSet.RuleApplied += this.LogRuleApplied;
            }

            if (LogLevel >= Level.Verbose)
            {
                _phono.RuleSet.RuleEntered += this.LogRuleEntered;
                _phono.RuleSet.RuleExited += this.LogRuleExited;
            }
        }

        public void Stop()
        {
            _phono.FeatureSet.FeatureDefined -= this.LogFeatureDefined;
            _phono.FeatureSet.FeatureRedefined -= this.LogFeatureRedefined;

            _phono.SymbolSet.SymbolDefined -= this.LogSymbolDefined;
            _phono.SymbolSet.SymbolRedefined -= this.LogSymbolRedefined;
            _phono.SymbolSet.SymbolDuplicate -= this.LogSymbolDuplicate;

            _phono.RuleSet.RuleDefined -= this.LogRuleDefined;
            _phono.RuleSet.RuleRedefined -= this.LogRuleRedefined;
            _phono.RuleSet.RuleEntered -= this.LogRuleEntered;
            _phono.RuleSet.RuleExited -= this.LogRuleExited;
            _phono.RuleSet.RuleApplied -= this.LogRuleApplied;
            _phono.RuleSet.UndefinedVariableUsed -= this.LogUndefinedVariableUsed;
            _phono.RuleSet.ScalarValueRangeViolation -= this.LogScalarValueRangeViolation;
            _phono.RuleSet.InvalidScalarValueOp -= this.LogInvalidScalarValueOp;

            Writer.Flush();
        }

        private void WriteLog(Level level, string format, params object[] args)
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
            WriteLog(Level.Info, "{0} {1} defined", f.GetType().Name, f.Name);
        }

        private void LogFeatureRedefined(Feature old, Feature newer)
        {
            WriteLog(Level.Warning, "{0} {1} overwrites {2} {3}", newer.GetType().Name, newer.Name, old.GetType().Name, old.Name);
        }

        private void LogSymbolDefined(Symbol s)
        {
            WriteLog(Level.Info, "symbol {0} defined", s);
        }

        private void LogSymbolRedefined(Symbol old, Symbol newer)
        {
            WriteLog(Level.Warning, "symbol {0} changed from {1} to {2}", old.Label, old.FeatureMatrix, newer.FeatureMatrix);
        }

        private void LogSymbolDuplicate(Symbol old, Symbol newer)
        {
            WriteLog(Level.Warning, "symbol {0} is identical to symbol {1}", newer, old);
        }

        private void LogRuleDefined(AbstractRule r)
        {
            WriteLog(Level.Info, "rule {0} defined", r);
        }

        private void LogRuleRedefined(AbstractRule old, AbstractRule newer)
        {
            WriteLog(Level.Warning, "rule {0} redefined", newer, old);
        }

        private void LogRuleEntered(AbstractRule rule, Word word)
        {
            WriteLog(Level.Verbose, "rule {0} entered", rule);
        }

        private void LogRuleExited(AbstractRule rule, Word word)
        {
            WriteLog(Level.Verbose, "rule {0} exited", rule);
        }

        private void LogRuleApplied(AbstractRule abstractRule, Word word, IWordSlice slice)
        {
            // since this method is potentially expensive, skip it if we're not
            // going to log anything
            if (this.LogLevel < Level.Info)
            {
                return;
            }

            var rule = abstractRule as Rule;
            if (rule == null)
            {
                // TODO
                return;
            }

            WriteLog(Level.Info, "rule {0} applied: {1}", rule.Name, rule.Description);

            // match until we get to the current segment, so that we can
            // display which segment was acted upon
            FeatureMatrix current = null;
            try
            {
                var ctx = new RuleContext();
                var seg = rule.Segments.GetEnumerator();
                var pos = slice.GetEnumerator();

                while (seg.MoveNext() && seg.Current.IsMatchOnlySegment)
                {
                    seg.Current.Matches(ctx, pos);
                }

                current = pos.MoveNext() ? pos.Current.Matrix : null;
            }
            catch (SegmentDeletedException)
            {
                // this occurs when we try to get the enumerator for a deleted
                // segment. this exception (and only this exception) can be
                // safely swallowed
            }

            var str = new StringBuilder();
            foreach (var seg in word)
            {
                string marker = " ";
                Symbol symbol;

                if (current != null && seg.Matrix == current)
                {
                    marker = ">";
                }

                try
                {
                    symbol = _phono.SymbolSet.Spell(seg.Matrix);
                }
                catch (SpellingException)
                {
                    symbol = Symbol.Unknown;
                }

                str.AppendLine(String.Format("{0} {1} : {2}", marker, symbol, seg.Matrix));
            }

            WriteLog(Level.Info, str.ToString());
        }

        private void LogUndefinedVariableUsed(Rule rule, IFeatureValue var)
        {
            WriteLog(Level.Warning, "variable {0} used in rule '{1}' without appearing in rule context; some parts of this rule may be skipped", var, rule);
        }

        private void LogScalarValueRangeViolation(Rule rule, ScalarFeature feature, int val)
        {
            WriteLog(Level.Warning, "in rule '{0}' resulting value {1}={2} is not in the range ({3}, {4}); some parts of this rule may be skipped", rule.Name, feature.Name, val, feature.Min, feature.Max);
        }

        private void LogInvalidScalarValueOp(Rule rule, ScalarFeature feature, string message)
        {
            WriteLog(Level.Warning, "invalid operation in rule '{0}' on feature '{1}': {2}", rule.Name, feature.Name, message);
        }
    }
}
