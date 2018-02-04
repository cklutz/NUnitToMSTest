using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnitToMSTest.Rewriter;
using NUnitToMSTest.Tests.Support;

namespace NUnitToMSTest.Tests.Attributes
{
    [TestClass]
    public class ExplicitAttributeTests : RefactoringTestBase
    {
        [TestMethod]
        public void Explicit()
        {
            const string actual = @"
using NUnit.Framework;
[TestFixture]
public class FooTests
{ 
    [Explicit] void Test {}
}
";
            const string expected = @"
using Microsoft.VisualStudio.TestTools.UnitTesting;
[TestClass]
public class FooTests
{ 
    [Ignore(""EXPLICIT"")] void Test {}
}
";
            TestRefactoring(actual, expected,
                (result, rw) =>
                {
                    Assert.IsTrue(rw.Changed);
                    Assert.AreEqual(1, rw.Diagnostics.Count(d => d.Id == DiagnosticsDescriptors.TransformedUnsupported.Id));
                    Assert.AreEqual(expected, result.ToFullString());
                });
        }

        [TestMethod]
        public void ExplicitWithDescription()
        {
            const string actual = @"
using NUnit.Framework;
[TestFixture]
public class FooTests
{ 
    [Explicit(""The description"")]
    void Test() {}
}
";
            const string expected = @"
using Microsoft.VisualStudio.TestTools.UnitTesting;
[TestClass]
public class FooTests
{ 
    [Ignore(""EXPLICIT: The description"")]
    void Test() {}
}
";
            TestRefactoring(actual, expected,
                (result, rw) =>
                {
                    Assert.IsTrue(rw.Changed);
                    Assert.AreEqual(1, rw.Diagnostics.Count(d => d.Id == DiagnosticsDescriptors.TransformedUnsupported.Id));
                    Assert.AreEqual(expected, result.ToFullString());
                });
        }
    }
}