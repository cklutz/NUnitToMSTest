using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnitToMSTest.Tests.Support;

namespace NUnitToMSTest.Tests.Asserts
{
    [TestClass]
    public class NullNotNullTests : RefactoringTestBase
    {
        [TestMethod]
        public void TestNull()
        {
            const string actual = @"
using NUnit.Framework;
public class FooTests
{ 
    void Test()
    {
        Assert.Null(null);
        Assert.NotNull(null);
        Assert.Null(null, ""The Message"");
        Assert.NotNull(null, ""The Message"");
    }
}
";
            const string expected = @"
using Microsoft.VisualStudio.TestTools.UnitTesting;
public class FooTests
{ 
    void Test()
    {
        Assert.IsNull(null);
        Assert.IsNotNull(null);
        Assert.IsNull(null, ""The Message"");
        Assert.IsNotNull(null, ""The Message"");
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