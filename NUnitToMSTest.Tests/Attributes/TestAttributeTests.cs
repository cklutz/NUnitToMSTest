using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnitToMSTest.Tests.Support;

namespace NUnitToMSTest.Tests.Attributes
{
    [TestClass]
    public class TestAttributeTests : RefactoringTestBase
    {
        [TestMethod]
        public void Test()
        {
            const string actual = @"
using NUnit.Framework;
[TestFixture]
public class FooTests
{ 
    [Test] void Test() {}
}
";
            const string expected = @"
using Microsoft.VisualStudio.TestTools.UnitTesting;
[TestClass]
public class FooTests
{ 
    [TestMethod] void Test() {}
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

        [TestMethod]
        public void TestWithDescription()
        {
            const string actual = @"
using NUnit.Framework;
[TestFixture]
public class FooTests
{ 
    [Test(Description = ""The description"")]
    void Test() {}
}
";
            const string expected = @"
using Microsoft.VisualStudio.TestTools.UnitTesting;
[TestClass]
public class FooTests
{ 
    [TestMethod]
    [Description(""The description"")]
    void Test() {}
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

        [TestMethod]
        public void TestWithAdditionalArguments()
        {
            const string actual = @"
using NUnit.Framework;
[TestFixture]
public class FooTests
{ 
    [Test(Author = ""Author"")]
    void Test() {}
}
";
            const string expected = @"
using Microsoft.VisualStudio.TestTools.UnitTesting;
[TestClass]
public class FooTests
{ 
    [TestMethod]
    void Test() {}
}
";
            TestRefactoring(actual, expected,
                (result, rw) =>
                {
                    Assert.IsTrue(rw.Changed);
                    Assert.AreEqual(1, rw.Diagnostics.Count());
                    Assert.AreEqual(expected, result.ToFullString());
                });
        }
    }
}