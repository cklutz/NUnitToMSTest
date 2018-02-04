using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnitToMSTest.Tests.Support;

namespace NUnitToMSTest.Tests.Attributes
{
    [TestClass]
    public class CategoryAttributeTests : RefactoringTestBase
    {
        [TestMethod]
        public void Category()
        {
            const string actual = @"
using NUnit.Framework;
[Category(""EXPLICIT"")]
[Category(""PERF"")]
public class FooTests
{ 
    [Category(""PERF2"")] void Test() {}
}
";
            const string expected = @"
using Microsoft.VisualStudio.TestTools.UnitTesting;
[TestCategory(""EXPLICIT"")]
[TestCategory(""PERF"")]
public class FooTests
{ 
    [TestCategory(""PERF2"")] void Test() {}
}
";
            TestRefactoring(actual, expected,
                (result, rw) =>
                {
                    Assert.IsTrue(rw.Changed);
                    Assert.AreEqual(0, rw.Diagnostics.Count());
                    Assert.AreEqual(expected, result.ToFullString());
                });
        }
   }
}