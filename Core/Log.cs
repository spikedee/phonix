using System;
using System.IO;
using System.Text;
using Phonix.Parse;

namespace Phonix
{
    public class Log
    {
        public enum Level
        {
            Error = 0,
            Warning = 1,
            Debug = 2,
            Verbose = 3
        };

        public readonly Level LogLevel;
        public readonly Level ErrorLevel;
        public readonly TextWriter Writer;
        private readonly Phonology _phono;
        private readonly PhonixParser _parser;
        private int _parseLevel = 0;

        public Log(Level logLevel, Level errorLevel, TextWriter writer, Phonology phono, PhonixParser parser)
        {
            LogLevel = logLevel;
            ErrorLevel = errorLevel;
            Writer = writer;
            _phono = phono;
            _parser = parser;
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

            if (LogLevel >= Level.Debug)
            {
                _parser.ParseBegin += this.LogParseBegin;
                _parser.ParseEnd += this.LogParseEnd;
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
            _parser.ParseBegin -= this.LogParseBegin;
            _parser.ParseEnd -= this.LogParseEnd;

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

        private void LogParseBegin(string filename)
        {
            _parseLevel++;
            string arrows = new string('>', _parseLevel);
            WriteLog(Level.Debug, "{0} begin parsing {1}", arrows, filename);
        }

        private void LogParseEnd(string filename)
        {
            string arrows = new string('<', _parseLevel);
            WriteLog(Level.Debug, "{0} end parsing {1}", arrows, filename);
            _parseLevel--;
        }

        private void LogFeatureDefined(Feature f)
        {
            WriteLog(Level.Debug, "{0} {1} defined", f.TypeName, f.Name);
        }

        private void LogFeatureRedefined(Feature old, Feature newer)
        {
            WriteLog(Level.Warning, "{0} {1} overwrites {2} {3}", newer.TypeName, newer.Name, old.TypeName, old.Name);
        }

        private void LogSymbolDefined(Symbol s)
        {
            WriteLog(Level.Debug, "Symbol {0} defined", s);
        }

        private void LogSymbolRedefined(Symbol old, Symbol newer)
        {
            WriteLog(Level.Warning, "Symbol {0} changed from {1} to {2}", old.Label, old.FeatureMatrix, newer.FeatureMatrix);
        }

        private void LogSymbolDuplicate(Symbol old, Symbol newer)
        {
            WriteLog(Level.Warning, "Symbol {0} is identical to symbol {1}", newer, old);
        }

        private void LogRuleDefined(AbstractRule r)
        {
            WriteLog(Level.Debug, "Rule {0} defined", r);
        }

        private void LogRuleRedefined(AbstractRule old, AbstractRule newer)
        {
            WriteLog(Level.Warning, "Rule {0} redefined", newer, old);
        }

        private void LogRuleEntered(AbstractRule rule, Word word)
        {
            WriteLog(Level.Verbose, "Rule {0} entered: {1}", rule, rule.Description);
        }

        private void LogRuleExited(AbstractRule rule, Word word)
        {
            WriteLog(Level.Verbose, "Rule {0} exited", rule);
        }

        private void LogRuleApplied(AbstractRule rule, Word word, WordSlice slice)
        {
            // since this method is potentially expensive, skip it if we're not
            // going to log anything
            if (this.LogLevel < Level.Debug)
            {
                return;
            }

            WriteLog(Level.Debug, "Rule {0} applied", rule.Name);
            WriteLog(Level.Debug, rule.ShowApplication(word, slice, _phono.SymbolSet));
        }

        private void LogUndefinedVariableUsed(Rule rule, IFeatureValue var)
        {
            WriteLog(Level.Warning, "In rule '{0}': variable {1} used without appearing in rule context; some parts of this rule may be skipped", rule.Name, var);
        }

        private void LogScalarValueRangeViolation(Rule rule, ScalarFeature feature, int val)
        {
            string valueMsg = null;
            if (val < feature.Min)
            {
                valueMsg = String.Format("less than the minimum value {0}", feature.Min);
            }
            else if (val > feature.Max.Value)
            {
                valueMsg = String.Format("greater than the maximum value {0}", feature.Max.Value);
            }

            if (valueMsg == null) // sanity check -- this should never happen
            {
                throw new ArgumentException("Scalar range out of value, but all value checks succeeded in handler. WTF?");
            }

            WriteLog(Level.Warning, "In rule '{0}': resulting value {1}={2} is {3}; some parts of this rule may be skipped", 
                    rule.Name, feature.Name, val, valueMsg);
        }

        private void LogInvalidScalarValueOp(Rule rule, ScalarFeature feature, string message)
        {
            WriteLog(Level.Warning, "In rule '{0}': invalid operation on feature '{1}': {2}", rule.Name, feature.Name, message);
        }
    }
}
