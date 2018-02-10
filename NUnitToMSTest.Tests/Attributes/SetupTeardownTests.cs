using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnitToMSTest.Rewriter;
using NUnitToMSTest.Tests.Support;

namespace NUnitToMSTest.Tests.Attributes
{
    [TestClass]
    public class SetupTeardownTests : RefactoringTestBase
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
}
";
            const string expected = @"
using Microsoft.VisualStudio.TestTools.UnitTesting;
[TestClass]
public class FooTests
{ 
    [TestInitialize] public void Setup() {}
    [TestCleanup] public void Teardown() {}
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
        public void SetupIssues()
        {
            const string actual = @"
using NUnit.Framework;
[TestFixture]
public class FooTests
{ 
    [SetUp] void Setup() {}
    [SetUp] public static void Setup() {}
    [SetUp] public int Setup() { return 0; }
}
";
            const string expected = @"
using Microsoft.VisualStudio.TestTools.UnitTesting;
[TestClass]
public class FooTests
{ 
    [TestInitialize] void Setup() {}
    [TestInitialize] public static void Setup() {}
    [TestInitialize] public int Setup() { return 0; }
}
";
            TestRefactoring(actual, expected,
                (result, rw) =>
                {
                    Assert.IsTrue(rw.Changed);
                    Assert.AreEqual(3, rw.Diagnostics.Count(d => d.Id == DiagnosticsDescriptors.IncompatibleTestInitiazeMethod.Id));
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
    [TearDown] void Teardown() {}
    [TearDown] public static void Teardown() {}
    [TearDown] public int Teardown() { return 0; }
}
";
            const string expected = @"
using Microsoft.VisualStudio.TestTools.UnitTesting;
[TestClass]
public class FooTests
{ 
    [TestCleanup] void Teardown() {}
    [TestCleanup] public static void Teardown() {}
    [TestCleanup] public int Teardown() { return 0; }
}
";
            TestRefactoring(actual, expected,
                (result, rw) =>
                {
                    Assert.IsTrue(rw.Changed);
                    Assert.AreEqual(3, rw.Diagnostics.Count(d => d.Id == DiagnosticsDescriptors.IncompatibleTestCleanupMethod.Id));
                    Assert.AreEqual(expected, result.ToFullString());
                });
        }
    }
}