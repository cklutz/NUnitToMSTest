using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnitToMSTest.Tests.Support;

namespace NUnitToMSTest.Tests.Asserts
{
    [TestClass]
    public class NestedTests : RefactoringTestBase
    {
        [TestMethod]
        public void TestNested()
        {
            const string actual = @"
using System.Threading.Tasks;
using NUnit.Framework;
public class FooTests
{ 
    void Test()
    {
        Parallel.For(0, 10, i => {
            Assert.That(true);
            for (int j = 0; j < 10; j++)
            {
                Assert.That(true);
            }
        });

        for (int i = 0; i < 10; i++)
        {
            Assert.That(true);
        }
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
        Parallel.For(0, 10, i => {
            Assert.IsTrue(true);
            for (int j = 0; j < 10; j++)
            {
                Assert.IsTrue(true);
            }
        });

        for (int i = 0; i < 10; i++)
        {
            Assert.IsTrue(true);
        }
    }
}
";
            TestRefactoringWithAsserts(actual, expected,
                (result, rw) =>
                {
                    Assert.IsTrue(rw.Changed);
                    Assert.AreEqual(0, rw.Diagnostics.Count());
                    Assert.AreEqual(expected, result.ToFullString());
                });
        }

        
   }
}