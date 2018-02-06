using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnitToMSTest.Rewriter;
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
    [OneTimeSetUp] static void SetupOnce() {}
    [OneTimeTearDown] static void TeardownOnce() {}
}
";
            const string expected = @"
using Microsoft.VisualStudio.TestTools.UnitTesting;
[TestClass]
public class FooTests
{ 
    [TestInitialize] void Setup() {}
    [TestCleanup] void Teardown() {}
    [ClassInitialize] static void SetupOnce() {}
    [ClassCleanup] static void TeardownOnce() {}
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
        public void SetupTeardownNonStatic()
        {
            const string actual = @"
using NUnit.Framework;
[TestFixture]
public class FooTests
{ 
    [OneTimeSetUp] void SetupOnce() {}
    [OneTimeTearDown] void TeardownOnce() {}
}
";
            const string expected = @"
using Microsoft.VisualStudio.TestTools.UnitTesting;
[TestClass]
public class FooTests
{ 
    [ClassInitialize] void SetupOnce() {}
    [ClassCleanup] void TeardownOnce() {}
}
";
            TestRefactoring(actual, expected,
                (result, rw) =>
                {
                    Assert.IsTrue(rw.Changed);
                    Assert.AreEqual(2, rw.Diagnostics.Count(d => d.Id == DiagnosticsDescriptors.MethodMustBeStaticForAttribute.Id));
                    Assert.AreEqual(expected, result.ToFullString());
                });
        }
    }
}