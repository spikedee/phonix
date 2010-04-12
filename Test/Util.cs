using System;
using Phonix;

namespace Phonix.Test
{
    using NUnit.Framework;

    public static class Util
    {
        public static void AssertThrow<T>(Action action) where T : Exception
        {
            try
            {
                action();
                Assert.Fail("Expected exception of type " + typeof(T));
            }
            catch (T)
            {
            }
        }
    }
}

