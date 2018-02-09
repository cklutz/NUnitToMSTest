using System.Linq;
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
    [SetUp] public void Setup() {}
    [TearDown] public void Teardown() {}
    [OneTimeSetUp] public static void SetupOnce() {}
    [OneTimeTearDown] public static void TeardownOnce() {}
}
";
            const string expected = @"
using Microsoft.VisualStudio.TestTools.UnitTesting;
[TestClass]
public class FooTests
{ 
    [TestInitialize] public void Setup() {}
    [TestCleanup] public void Teardown() {}
    [ClassInitialize] public static void SetupOnce() {}
    [ClassCleanup] public static void TeardownOnce() {}
}
";
            TestRefactoring(actual, expected,
                (result, rw) =>
                {
                    Assert.IsTrue(rw.Changed);
                    // One diagnostic, because SetupOnce() does not have a "TestContext" parameter, which we currently don't add
                    // automatically during conversion.
                    Assert.AreEqual(1, rw.Diagnostics.Count(d => d.Id == DiagnosticsDescriptors.IncompatibleClassInitiazeMethod.Id));
                    Assert.AreEqual(expected, result.ToFullString());
                });
        }

        [TestMethod]
        public void SetupIssues()
        {
            const string actual = @"
using NUnit.Framework;
[TestFixture]
public class FooTests
{ 
    [OneTimeSetUp] void SetupOnce() {}
    [OneTimeSetUp] static void SetupOnce() {}
    [OneTimeSetUp] public void SetupOnce() {}
    [OneTimeSetUp] public static void SetupOnce() {}
}
";
            const string expected = @"
using Microsoft.VisualStudio.TestTools.UnitTesting;
[TestClass]
public class FooTests
{ 
    [ClassInitialize] void SetupOnce() {}
    [ClassInitialize] static void SetupOnce() {}
    [ClassInitialize] public void SetupOnce() {}
    [ClassInitialize] public static void SetupOnce() {}
}
";
            TestRefactoring(actual, expected,
                (result, rw) =>
                {
                    Assert.IsTrue(rw.Changed);
                    Assert.AreEqual(4, rw.Diagnostics.Count(d => d.Id == DiagnosticsDescriptors.IncompatibleClassInitiazeMethod.Id));
                    Assert.AreEqual(expected, result.ToFullString());
                });
        }

        [TestMethod]
        public void TeardownIssues()
        {
            const string actual = @"
using NUnit.Framework;
[TestFixture]
public class FooTests
{ 
    [OneTimeTearDown] void TeardownOnce() {}
    [OneTimeTearDown] static void TeardownOnce() {}
    [OneTimeTearDown] public void TeardownOnce() {}
}
";
            const string expected = @"
using Microsoft.VisualStudio.TestTools.UnitTesting;
[TestClass]
public class FooTests
{ 
    [ClassCleanup] void TeardownOnce() {}
    [ClassCleanup] static void TeardownOnce() {}
    [ClassCleanup] public void TeardownOnce() {}
}
";
            TestRefactoring(actual, expected,
                (result, rw) =>
                {
                    Assert.IsTrue(rw.Changed);
                    Assert.AreEqual(3, rw.Diagnostics.Count(d => d.Id == DiagnosticsDescriptors.IncompatibleClassCleanupMethod.Id));
                    Assert.AreEqual(expected, result.ToFullString());
                });
        }
    }
}