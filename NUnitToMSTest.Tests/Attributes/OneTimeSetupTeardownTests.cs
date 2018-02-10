using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnitToMSTest.Rewriter;
using NUnitToMSTest.Tests.Support;

namespace NUnitToMSTest.Tests.Attributes
{
    [TestClass]
    public class OneTimeSetupTeardownTests : RefactoringTestBase
    {
        [TestMethod]
        public void SetupTeardown()
        {
            const string actual = @"
using NUnit.Framework;
[TestFixture]
public class FooTests
{ 
    [OneTimeSetUp] public static void SetupOnce() {}
    [OneTimeTearDown] public static void TeardownOnce() {}
    // Bogus: in reality you wouldn't have a mixed thing where attribute is from
    // NUnit and argument from MSTestV2, but we want to check that this does not
    // cause a warning.
    // (Note using fqn, because NUnit.Framework.TestContext also exists.)
    [OneTimeSetUp] public static void SetupOnce(Microsoft.VisualStudio.TestTools.UnitTesting.TestContext tc) {}
}
";
            const string expected = @"
using Microsoft.VisualStudio.TestTools.UnitTesting;
[TestClass]
public class FooTests
{ 
    [ClassInitialize] public static void SetupOnce() {}
    [ClassCleanup] public static void TeardownOnce() {}
    // Bogus: in reality you wouldn't have a mixed thing where attribute is from
    // NUnit and argument from MSTestV2, but we want to check that this does not
    // cause a warning.
    // (Note using fqn, because NUnit.Framework.TestContext also exists.)
    [ClassInitialize] public static void SetupOnce(Microsoft.VisualStudio.TestTools.UnitTesting.TestContext tc) {}
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
    [OneTimeSetUp] public static void SetupOnce(int wrongType) {}
    [OneTimeSetUp] public static int SetupOnce() { return 0; }
    [OneTimeSetUp] public static void SetupOnce(TestContext testContext) {}
    [OneTimeSetUp] public static void SetupOnce(NUnit.Framework.TestContext nunitTestContext) {}
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
    [ClassInitialize] public static void SetupOnce(int wrongType) {}
    [ClassInitialize] public static int SetupOnce() { return 0; }
    [ClassInitialize] public static void SetupOnce(TestContext testContext) {}
    [ClassInitialize] public static void SetupOnce(NUnit.Framework.TestContext nunitTestContext) {}
}
";
            TestRefactoring(actual, expected,
                (result, rw) =>
                {
                    Assert.IsTrue(rw.Changed);
                    Assert.AreEqual(8, rw.Diagnostics.Count(d => d.Id == DiagnosticsDescriptors.IncompatibleClassInitiazeMethod.Id));
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
    [OneTimeTearDown] public static int TeardownOnce() { return 0; }
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
    [ClassCleanup] public static int TeardownOnce() { return 0; }
}
";
            TestRefactoring(actual, expected,
                (result, rw) =>
                {
                    Assert.IsTrue(rw.Changed);
                    Assert.AreEqual(4, rw.Diagnostics.Count(d => d.Id == DiagnosticsDescriptors.IncompatibleClassCleanupMethod.Id));
                    Assert.AreEqual(expected, result.ToFullString());
                });
        }
    }
}