using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnitToMSTest.Tests.Support;

namespace NUnitToMSTest.Tests.Asserts
{
    [TestClass]
    public class GreaterLessOrEqualTests : RefactoringTestBase
    {
        [DataTestMethod]
        [DataRow("Less", "less than", "<")]
        [DataRow("LessOrEqual", "less than or equal to", "<=")]
        [DataRow("Greater", "greater than", ">")]
        [DataRow("GreaterOrEqual", "greater than or equal to", ">=")]
        public void Test(string origName, string symbolic, string token)
        {
            string actual = @"
using NUnit.Framework;
public class FooTests
{
    int A() { return 1; }
    int B() { return 2; }
    string Complicated() { return string.Empty; }
    void Test()
    {
        Assert.@@NAME@@(1, 2);
        Assert.@@NAME@@(A(), B());
        Assert.@@NAME@@(A(), B(), ""message {0}"", 3);
        Assert.@@NAME@@(A(), B(), Complicated(), 3);
    }
}
".Replace("@@NAME@@", origName).Replace("@@SYMBOLIC@@", symbolic).Replace("@@TOKEN@@", token);

            string expected = @"
using Microsoft.VisualStudio.TestTools.UnitTesting;
public class FooTests
{
    int A() { return 1; }
    int B() { return 2; }
    string Complicated() { return string.Empty; }
    void Test()
    {
        Assert.IsTrue(1 @@TOKEN@@ 2, ""Expected <1> to be @@SYMBOLIC@@ <2>."");
        Assert.IsTrue(A() @@TOKEN@@ B(), ""Expected <A()> to be @@SYMBOLIC@@ <B()>."");
        Assert.IsTrue(A() @@TOKEN@@ B(), ""Expected <A()> to be @@SYMBOLIC@@ <B()>."" + ""message {0}"", 3);
        Assert.IsTrue(A() @@TOKEN@@ B(), ""Expected <A()> to be @@SYMBOLIC@@ <B()>."" + Complicated(), 3);
    }
}
".Replace("@@NAME@@", origName).Replace("@@SYMBOLIC@@", symbolic).Replace("@@TOKEN@@", token);

            TestRefactoringWithAsserts(
                actual,
                expected,
                (result, rw) =>
                {
                    Assert.IsTrue(rw.Changed);
                    Assert.AreEqual(0, rw.Diagnostics.Count());
                    Assert.AreEqual(expected, result.ToFullString());
                });
        }
    }
}