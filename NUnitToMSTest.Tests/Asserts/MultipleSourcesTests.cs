using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnitToMSTest.Tests.Support;

namespace NUnitToMSTest.Tests.Asserts
{
    [TestClass]
    public class MultipleSourcesTests : RefactoringTestBase
    {
        [TestMethod]
        public void TestMultipleCompilations()
        {
            const string auxiliary = @"
public class TestObj
{
    public int Value() => 41;
}
";
            const string actual = @"
using System.Threading.Tasks;
using NUnit.Framework;
public class FooTests
{ 
    void Test()
    {
        TestObj to = new TestObj();
        Assert.Less(to.Value(), 42);
    }
}
";
            const string expected = @"
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
public class FooTests
{ 
    void Test()
    {
        TestObj to = new TestObj();
        Assert.IsTrue(to.Value() < 42, ""Expected <to.Value()> to be less than <42>."");
    }
}
";
            TestRefactoringMultipCompilations(actual, expected, auxiliary,
                (result, rw) =>
                {
                    Assert.IsTrue(rw.Changed);
                    Assert.AreEqual(0, rw.Diagnostics.Count());
                    Assert.AreEqual(expected, result.ToFullString());
                }, true);
        }


    }
}