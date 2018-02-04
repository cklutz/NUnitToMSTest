using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnitToMSTest.Tests.Support;

namespace NUnitToMSTest.Tests.Attributes
{
    [TestClass]
    public class TestCaseAttributeTests : RefactoringTestBase
    {
        [TestMethod]
        public void TestCase()
        {
            const string actual = @"
using NUnit.Framework;
[TestFixture]
public class FooTests
{ 
    [TestCase(1, 2, 3)]
    void Test(int a, int b, int c) {}
}
";
            const string expected = @"
using Microsoft.VisualStudio.TestTools.UnitTesting;
[TestClass]
public class FooTests
{ 
    [DataRow(1, 2, 3)]
    [DataTestMethod]
    void Test(int a, int b, int c) {}
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
        public void TestCaseMultiple()
        {
            const string actual = @"
using NUnit.Framework;
[TestFixture]
public class FooTests
{ 
    [TestCase(1, 2, 3)]
    [CLSCompliant(false)]
    [TestCase(2, 3, 4)]
    void Test(int a, int b, int c) {}
}
";
            const string expected = @"
using Microsoft.VisualStudio.TestTools.UnitTesting;
[TestClass]
public class FooTests
{ 
    [DataRow(1, 2, 3)]
    [CLSCompliant(false)]
    [DataRow(2, 3, 4)]
    [DataTestMethod]
    void Test(int a, int b, int c) {}
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