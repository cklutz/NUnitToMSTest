using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnitToMSTest.Tests.Support;

namespace NUnitToMSTest.Tests.Attributes
{
    [TestClass]
    public class MiscAttributesTests : RefactoringTestBase
    {
        [TestMethod]
        public void SetupTeardown()
        {
            const string actual = @"
using NUnit.Framework;
[TestFixture]
public class FooTests
{ 
    [SetUp] void Setup() {}
    [TearDown] void Teardown() {}
    [OneTimeSetUp] void SetupOnce() {}
    [OneTimeTearDown] void TeardownOnce() {}
}
";
            const string expected = @"
using Microsoft.VisualStudio.TestTools.UnitTesting;
[TestClass]
public class FooTests
{ 
    [TestInitialize] void Setup() {}
    [TestCleanup] void Teardown() {}
    [ClassInitialize] void SetupOnce() {}
    [ClassCleanup] void TeardownOnce() {}
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