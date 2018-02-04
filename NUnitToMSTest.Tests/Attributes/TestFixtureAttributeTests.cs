using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnitToMSTest.Rewriter;
using NUnitToMSTest.Tests.Support;

namespace NUnitToMSTest.Tests.Attributes
{
    [TestClass]
    public class TestFixtureAttributeTests : RefactoringTestBase
    {
        [TestMethod]
        public void NoNamespaceMatch()
        {
            const string actual = @"
[TestFixture]
public class FooTests { }
";
            const string expected = @"
[TestFixture]
public class FooTests { }
";
            TestRefactoring(actual, expected,
                (result, rw) =>
                {
                    Assert.IsFalse(rw.Changed);
                    Assert.AreEqual(0, rw.Diagnostics.Count());
                    Assert.AreEqual(expected, result.ToFullString());
                });
        }

        [TestMethod]
        public void TestFixture()
        {
            const string actual = @"
using NUnit.Framework;
[TestFixture]
public class FooTests { }
";
            const string expected = @"
using Microsoft.VisualStudio.TestTools.UnitTesting;
[TestClass]
public class FooTests { }
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
        public void TestFixtureWithAdditionalArguments()
        {
            const string actual = @"
using NUnit.Framework;
[TestFixture(Author = ""Author"")]
public class FooTests { }
";
            const string expected = @"
using Microsoft.VisualStudio.TestTools.UnitTesting;
[TestClass]
public class FooTests { }
";
            TestRefactoring(actual, expected,
                (result, rw) =>
                {
                    Assert.IsTrue(rw.Changed);
                    Assert.AreEqual(1,
                        rw.Diagnostics.Count(v => v.Id == DiagnosticsDescriptors.IgnoredAllArguments.Id));
                    Assert.AreEqual(expected, result.ToFullString());
                });
        }

    }
}