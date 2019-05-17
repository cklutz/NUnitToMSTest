using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnitToMSTest.Tests.Support;

namespace NUnitToMSTest.Tests.Asserts
{
    [TestClass]
    public class InstanceOfTypeTests : RefactoringTestBase
    {
        [TestMethod]
        public void TestInstanceOfType()
        {
            const string actual = @"
using NUnit.Framework;
public class FooTests
{ 
    void Test()
    {
        Assert.IsInstanceOf(typeof(Type), GetType());
        Assert.IsInstanceOf(typeof(Type), GetType(), ""message"");
        Assert.IsInstanceOf(typeof(Type), GetType(), ""message {0}"", 1);
    }
}
";
            const string expected = @"
using Microsoft.VisualStudio.TestTools.UnitTesting;
public class FooTests
{ 
    void Test()
    {
        Assert.IsInstanceOfType(GetType(), typeof(Type));
        Assert.IsInstanceOfType(GetType(), typeof(Type), ""message"");
        Assert.IsInstanceOfType(GetType(), typeof(Type), ""message {0}"", 1);
    }
}
";
            TestRefactoringWithAsserts(actual, expected,
                (result, rw) =>
                {
                    Assert.IsTrue(rw.Changed);
                    Assert.AreEqual(0, rw.Diagnostics.Count());
                    Assert.AreEqual(expected, result.ToFullString());
                });
        }

        [TestMethod]
        public void TestInstanceOfTypeGeneric()
        {
            const string actual = @"
using NUnit.Framework;
public class FooTests
{ 
    void Test()
    {
        Assert.IsInstanceOf<Type>(GetType());
        Assert.IsInstanceOf<Type>(GetType(), ""message"");
        Assert.IsInstanceOf<Type>(GetType(), ""message {0}"", 1);
    }
}
";
            const string expected = @"
using Microsoft.VisualStudio.TestTools.UnitTesting;
public class FooTests
{ 
    void Test()
    {
        Assert.IsInstanceOfType(GetType(), typeof(Type));
        Assert.IsInstanceOfType(GetType(), typeof(Type), ""message"");
        Assert.IsInstanceOfType(GetType(), typeof(Type), ""message {0}"", 1);
    }
}
";
            TestRefactoringWithAsserts(actual, expected,
                (result, rw) =>
                {
                    Assert.IsTrue(rw.Changed);
                    Assert.AreEqual(0, rw.Diagnostics.Count());
                    Assert.AreEqual(expected, result.ToFullString());
                });
        }
    }
}