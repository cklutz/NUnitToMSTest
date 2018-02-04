using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NUnitToMSTest.Testing.Helpers
{
    public class AssertEx
    {
        public static T ThrowsInstanceOf<T>(Action action) where T : Exception
        {
            return ThrowsInstanceOf<T>(action, null, null);
        }

        public static T ThrowsInstanceOf<T>(Action action, string message, params object[] args) where T : Exception
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                if (!ex.GetType().IsInstanceOfType(typeof(T)))
                {
                    Assert.AreEqual(typeof(T), ex.GetType(), message ?? "wrong exception type", args);
                }

                return (T)ex;
            }

            Assert.Fail(message ?? typeof(T) + " exception expedcted", args);
            return null; // Not reached
        }

        public static T ThrowsTypeOf<T>(Action action) where T : Exception
        {
            return ThrowsTypeOf<T>(action, null, null);
        }

        public static T ThrowsTypeOf<T>(Action action, string message, params object[] args) where T : Exception
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                if (ex.GetType() != typeof(T))
                {
                    Assert.AreEqual(typeof(T), ex.GetType(), message ?? "wrong exception type", args);
                }

                return (T)ex;
            }

            Assert.Fail(message ?? typeof(T) + " exception expedcted", args);
            return null; // Not reached
        }
    }
}
