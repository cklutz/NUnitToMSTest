using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnitToMSTest.Tests.Support;

namespace NUnitToMSTest.Tests.Asserts
{
    [TestClass]
    public class ThrowsTests : RefactoringTestBase
    {
        [TestMethod]
        public void TestDoesNotTouchNotUnderstood()
        {
            const string actual = @"
using NUnit.Framework;
public class FooTests
{ 
    void Dummy() { }
    void Test()
    {
        Assert.That(() => Dummy(), Throws.InstanceOf<OutOfMemoryException>().And.InnerException.Not.Null);
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
        Assert.That(() => Dummy(), Throws.InstanceOf<OutOfMemoryException>().And.InnerException.Not.Null);
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
        public void TestThrowsTypeOfStaticAccessor()
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
        public void TestThrowsTypeOf()
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
        Assert.That(() => Dummy(), Throws.TypeOf<OutOfMemoryException>());
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
        Assert.ThrowsException<OutOfMemoryException>(() => Dummy());
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
        public void TestThrowsInstanceOf()
        {
            const string actual = @"
using NUnit.Framework;
public class FooTests
{ 
    void Dummy() { }
    void Test()
    {
        Assert.That(Dummy, Throws.InstanceOf<OutOfMemoryException>());
        Assert.That(() => Dummy(), Throws.InstanceOf<OutOfMemoryException>());
        Assert.That(() => Dummy(), Throws.Exception.InstanceOf<OutOfMemoryException>());
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
        Assert.InstanceOfType(Assert.ThrowsException<Exception>(Dummy),typeof(OutOfMemoryException));
        Assert.InstanceOfType(Assert.ThrowsException<Exception>(() => Dummy()),typeof(OutOfMemoryException));
        Assert.InstanceOfType(Assert.ThrowsException<Exception>(() => Dummy()),typeof(OutOfMemoryException));
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