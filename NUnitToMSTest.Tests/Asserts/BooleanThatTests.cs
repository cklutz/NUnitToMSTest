using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnitToMSTest.Tests.Support;

namespace NUnitToMSTest.Tests.Asserts
{
    [TestClass]
    public class BooleanThatTests : RefactoringTestBase
    {
        [TestMethod]
        public void TestBooleanThat()
        {
            const string actual = @"
using NUnit.Framework;
public class FooTests
{ 
    bool Func() { return true; }
    void Test()
    {
        Assert.That(true);
        Assert.That(true, ""message"");
        Assert.That(Func(), ""message"");
        Assert.That(1.Bar(), ""message"");
    }
}
public static class Extensions { public static bool Bar(this int i) { return true; } }
";
            const string expected = @"
using Microsoft.VisualStudio.TestTools.UnitTesting;
public class FooTests
{ 
    bool Func() { return true; }
    void Test()
    {
        Assert.IsTrue(true);
        Assert.IsTrue(true, ""message"");
        Assert.IsTrue(Func(), ""message"");
        Assert.IsTrue(1.Bar(), ""message"");
    }
}
public static class Extensions { public static bool Bar(this int i) { return true; } }
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