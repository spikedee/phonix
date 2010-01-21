using System;

namespace Phonix
{
    public class PhonixException : Exception
    {
        public PhonixException(string message)
            : base(message)
        {}
    }

    public class FeatureNotFoundException : PhonixException
    {
        public FeatureNotFoundException(string feature)
            : base(String.Format("feature '{0}' not found", feature))
            {}
    }

    public class FeatureTypeException : PhonixException
    {
        public FeatureTypeException(string name, string type)
            : base(String.Format("feature '{0}' is not a {1} feature", name, type))
        {
        }
    }

    public class SpellingException : PhonixException 
    {
        public SpellingException(string desc) 
            : base(desc) {}
    }

    public class RuleException : PhonixException
    {
        public RuleException(string message)
            : base(message)
        {
        }
    }

    public class UnknownParameterException : PhonixException
    {
        public UnknownParameterException(string param)
            : base("Unknown parameter: " + param)
        {
        }
    }

    public class InvalidParameterValueException : PhonixException
    {
        public InvalidParameterValueException(string key, string val)
            : base(String.Format("Invalid value for {0}: {1}", key, val))
        {
        }
    }

    public class SegmentDeletedException : PhonixException
    {
        public SegmentDeletedException()
            : base("An attempt was made to access a segment which was deleted.")
        {
        }
    }

    public class UndefinedFeatureVariableException : PhonixException
    {
        public readonly IMatchCombine Variable;

        public UndefinedFeatureVariableException(IMatchCombine var)
            : base("Undefined variable used: " + var)
        {
            Variable = var;
        }
    }

    public class ParseException : Exception
    {
        public ParseException(string file)
            : base("Parsing errors in " + file)
        {
        }
    }

    public class FatalWarningException : Exception
    {
        public FatalWarningException(string message)
            : base(message)
        {}
    }

}
