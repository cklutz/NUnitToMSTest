using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnitToMSTest.Rewriter;
using NUnitToMSTest.Tests.Support;

namespace NUnitToMSTest.Tests.Attributes
{
    [TestClass]
    public class DescriptionAttributeTests : RefactoringTestBase
    {
        [TestMethod]
        public void Description()
        {
            const string actual = @"
using NUnit.Framework;
[TestFixture]
public class FooTests
{ 
    [Description(""The description"")]
    void Test() {}
}
";
            const string expected = @"
using Microsoft.VisualStudio.TestTools.UnitTesting;
[TestClass]
public class FooTests
{ 
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
        public void DescriptionOnClass()
        {
            const string actual = @"
using NUnit.Framework;
[TestFixture]
[Description(""The description"")]
public class FooTests { }
";
            const string expected = @"
using Microsoft.VisualStudio.TestTools.UnitTesting;
[TestClass]
[NUnit.Framework.DescriptionAttribute(""The description"")]
public class FooTests { }
";
            TestRefactoring(actual, expected,
                (result, rw) =>
                {
                    Assert.IsTrue(rw.Changed);
                    Assert.AreEqual(1, rw.Diagnostics.Count(d => d.Id == DiagnosticsDescriptors.UnsupportedAttribute.Id));
                    Assert.AreEqual(expected, result.ToFullString());
                });
        }
    }
}