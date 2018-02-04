using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnitToMSTest.Rewriter;
using NUnitToMSTest.Tests.Support;

namespace NUnitToMSTest.Tests.Attributes
{
    [TestClass]
    public class PropertyAttributeTests : RefactoringTestBase
    {
        [TestMethod]
        public void Property()
        {
            const string actual = @"
using NUnit.Framework;
[TestFixture]
[Property(""p1"", ""value 1"")]
[Property(""p2"", ""value 2"")]
public class FooTests
{ 
    [Property(""p3"", ""value 3"")]
    [Property(""p4"", ""value 4"")]
    void Test() {}
}
";
            const string expected = @"
using Microsoft.VisualStudio.TestTools.UnitTesting;
[TestClass]
[TestProperty(""p1"",""value 1"")]
[TestProperty(""p2"",""value 2"")]
public class FooTests
{ 
    [TestProperty(""p3"",""value 3"")]
    [TestProperty(""p4"",""value 4"")]
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
        public void PropertyUnsupportedTypes()
        {
            const string actual = @"
using NUnit.Framework;
[TestFixture]
[Property(""p1"", ""value 1"")]
[Property(""p2"", 1.0)]
[Property(""p3"", 1)]
public class FooTests { }
";
            const string expected = @"
using Microsoft.VisualStudio.TestTools.UnitTesting;
[TestClass]
[TestProperty(""p1"",""value 1"")]
[TestProperty(""p2"", ""1.0"")]
[TestProperty(""p3"", ""1"")]
public class FooTests { }
";
            TestRefactoring(actual, expected,
                (result, rw) =>
                {
                    Assert.IsTrue(rw.Changed);
                    Assert.AreEqual(2, rw.Diagnostics.Count(d => d.Id == DiagnosticsDescriptors.ConvertedArgumentValueToString.Id));
                    Assert.AreEqual(expected, result.ToFullString());
                });
        }
    }
}