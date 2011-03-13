using Antlr.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Phonix.Parse
{
    public class SemanticContext
    {
        private class SemanticException : PhonixException
        {
            public SemanticException(object obj, string expected)
                : base(String.Format("{0} cannot be used here; should be {1}", obj, expected))
            {
            }
        }

        private static IEnumerable<T> CheckTypes<T>(IEnumerable<object> objs, string expectedType)
            where T : class
        {
            var tList = new List<T>();
            foreach (var obj in objs)
            {
                if (obj == null)
                {
                    throw new ArgumentNullException("obj");
                }

                var t = obj as T;
                if (t == null)
                {
                    throw new SemanticException(obj, expectedType);
                }
                tList.Add(t);
            }
            return tList;
        }

        public static FeatureMatrix FeatureMatrix(IEnumerable<object> objs)
        {
            return new FeatureMatrix(CheckTypes<FeatureValue>(objs, 
                        "concrete feature value (not a variable, node, or syllable feature)"));
        }

        public static IEnumerable<IMatchable> MatchableMatrix(IEnumerable<object> objs)
        {
            return CheckTypes<IMatchable>(objs, "match value (concrete feature value, variable, node, or syllable feature)");
        }

        public static IEnumerable<ICombinable> CombinableMatrix(IEnumerable<object> objs)
        {
            return CheckTypes<ICombinable>(objs, "combining value (concrete feature value or variable)");
        }
    }
}
