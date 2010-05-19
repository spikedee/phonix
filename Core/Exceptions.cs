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

    public class InvalidScalarOpException : PhonixException
    {
        public readonly ScalarFeature Feature;

        public InvalidScalarOpException(ScalarFeature feature, string desc)
            : base(desc)
        {
            Feature = feature;
        }
    }

    public class ScalarValueRangeException : PhonixException
    {
        public readonly ScalarFeature Feature;
        public readonly int Value;

        public ScalarValueRangeException(ScalarFeature feature, int actual)
            : base(String.Format("value {0} for {1} was not in the range ({2}, {3})",
                        actual, feature.Name, feature.Min, feature.Max))
        {
            Feature = feature;
            Value = actual;
        }
    }

    public class SpellingException : PhonixException 
    {
        public SpellingException(string desc) 
            : base(desc) 
        {
        }
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
        public readonly IFeatureValue Variable;

        public UndefinedFeatureVariableException(IFeatureValue var)
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
        {
        }
    }

}
