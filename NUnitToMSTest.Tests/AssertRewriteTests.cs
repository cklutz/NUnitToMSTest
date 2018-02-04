using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnitToMSTest.Tests.Support;

namespace NUnitToMSTest.Tests
{
    [TestClass]
    public class AssertRewriteTests : RefactoringTestBase
    {
        [TestMethod]
        public void TestNull()
        {
            const string actual = @"
using NUnit.Framework;
public class FooTests
{ 
    void Test()
    {
        Assert.Null(null);
        Assert.NotNull(null);
        Assert.Null(null, ""The Message"");
        Assert.NotNull(null, ""The Message"");
    }
}
";
            const string expected = @"
using Microsoft.VisualStudio.TestTools.UnitTesting;
public class FooTests
{ 
    void Test()
    {
        Assert.IsNull(null);
        Assert.IsNotNull(null);
        Assert.IsNull(null, ""The Message"");
        Assert.IsNotNull(null, ""The Message"");
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

        [TestMethod]
        public void TestBooleanThat()
        {
            const string actual = @"
using NUnit.Framework;
public class FooTests
{ 
    bool Func() { return true; }
    void Test()
    {
        Assert.That(true);
        Assert.That(true, ""message"");
        Assert.That(Func(), ""message"");
        Assert.That(1.Bar(), ""message"");
    }
}
public static class Extensions { public static bool Bar(this int i) { return true; } }
";
            const string expected = @"
using Microsoft.VisualStudio.TestTools.UnitTesting;
public class FooTests
{ 
    bool Func() { return true; }
    void Test()
    {
        Assert.IsTrue(true);
        Assert.IsTrue(true, ""message"");
        Assert.IsTrue(Func(), ""message"");
        Assert.IsTrue(1.Bar(), ""message"");
    }
}
public static class Extensions { public static bool Bar(this int i) { return true; } }
";
            TestRefactoringWithAsserts(actual, expected,
                (result, rw) =>
                {
                    Assert.IsTrue(rw.Changed);
                    Assert.AreEqual(0, rw.Diagnostics.Count());
                    Assert.AreEqual(expected, result.ToFullString());
                });
        }

        [TestMethod]
        public void TestThrows()
        {
            const string actual = @"
using NUnit.Framework;
public class FooTests
{ 
    void Dummy() { }
    void Test()
    {
        Assert.That(Dummy, Throws.ArgumentNullException);
        Assert.That(() => { int i = 0; i++; }, Throws.ArgumentNullException);
        Assert.That(() => Dummy(), Throws.ArgumentNullException);
        Assert.That(() => Dummy(), Throws.Exception.TypeOf<OutOfMemoryException>());
        Assert.That(() => Dummy(), Throws.Exception.InstanceOf<OutOfMemoryException>());
        Assert.That(() => Dummy(), Throws.TypeOf<OutOfMemoryException>());
        Assert.That(() => Dummy(), Throws.InstanceOf<OutOfMemoryException>());

        // Leave unsupported stuff alone.
        Assert.That(() => Foo(), Throws.InstanceOf<OutOfMemoryException>().And.InnerException.Not.Null);
    }
}
";
            const string expected = @"
using Microsoft.VisualStudio.TestTools.UnitTesting;
public class FooTests
{ 
    void Dummy() { }
    void Test()
    {
        Assert.ThrowsException<ArgumentNullException>(Dummy);
        Assert.ThrowsException<ArgumentNullException>(() => { int i = 0; i++; });
        Assert.ThrowsException<ArgumentNullException>(() => Dummy());
        Assert.ThrowsException<OutOfMemoryException>(() => Dummy());
        AssertEx.ThrowsInstanceOf<OutOfMemoryException>(() => Dummy());
        Assert.ThrowsException<OutOfMemoryException>(() => Dummy());
        AssertEx.ThrowsInstanceOf<OutOfMemoryException>(() => Dummy());

        // Leave unsupported stuff alone.
        Assert.That(() => Foo(), Throws.InstanceOf<OutOfMemoryException>().And.InnerException.Not.Null);
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