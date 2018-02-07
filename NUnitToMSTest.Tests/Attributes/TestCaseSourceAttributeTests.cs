using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnitToMSTest.Rewriter;
using NUnitToMSTest.Tests.Support;

namespace NUnitToMSTest.Tests.Attributes
{
    [TestClass]
    public class TestCaseSourceAttributeTests : RefactoringTestBase
    {
        [TestMethod]
        public void TestCaseSource()
        {
            const string actual = @"
using NUnit.Framework;
[TestFixture]
public class FooTests
{ 
    [TestCaseSource(""Method"")]
    [TestCaseSource(nameof(Method))]
    [TestCaseSource(""Property"")]
    [TestCaseSource(typeof(FooTests), ""Method"")]
    [TestCaseSource(typeof(Helper), ""MethodFromHelper"")]
    [TestCaseSource(typeof(Helper), ""PropertyFromHelper"")]
    void Test(int a, int b, int c) {}

    IEnumerable<object[]> Method() { return null; }
    IEnumerable<object[]> Property => null; 
}

public class Helper 
{ 
    IEnumerable<object[]> MethodFromHelper() { return null; } 
    IEnumerable<object[]> PropertyFromHelper => null; 
}
";
            const string expected = @"
using Microsoft.VisualStudio.TestTools.UnitTesting;
[TestClass]
public class FooTests
{ 
    [DynamicData(""Method"")]
    [DynamicData(""Method"")]
    [DynamicData(""Property"", DynamicDataSourceType.Property)]
    [DynamicData(""Method"", typeof (FooTests))]
    [DynamicData(""MethodFromHelper"", typeof (Helper))]
    [DynamicData(""PropertyFromHelper"", typeof (Helper), DynamicDataSourceType.Property)]
    void Test(int a, int b, int c) {}

    IEnumerable<object[]> Method() { return null; }
    IEnumerable<object[]> Property => null; 
}

public class Helper 
{ 
    IEnumerable<object[]> MethodFromHelper() { return null; } 
    IEnumerable<object[]> PropertyFromHelper => null; 
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
        public void TestCaseSourceUnsupported()
        {
            const string actual = @"
using NUnit.Framework;
[TestFixture]
public class FooTests
{ 
    [TestCaseSource(""Field"")]
    void Test(int a, int b, int c) {}

    IEnumerable<object[]> Field = null; 
}
";
            const string expected = @"
using Microsoft.VisualStudio.TestTools.UnitTesting;
[TestClass]
public class FooTests
{ 
    [TestCaseSource(""Field"")]
    void Test(int a, int b, int c) {}

    IEnumerable<object[]> Field = null; 
}
";
            TestRefactoring(actual, expected,
                (result, rw) =>
                {
                    Assert.IsTrue(rw.Changed);
                    Assert.AreEqual(1, rw.Diagnostics.Count(d => d.Id == DiagnosticsDescriptors.UnsupportedAttributeUsage.Id));
                    Assert.AreEqual(expected, result.ToFullString());
                });
        }
    }
}