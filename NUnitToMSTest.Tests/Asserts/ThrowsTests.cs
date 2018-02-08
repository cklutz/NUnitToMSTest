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
        Assert.That(() => Dummy(), Throws.Exception.Message.Contains(""the message"").And.InstanceOf<ArgumentException>());
        Assert.That(() => Dummy(), Throws.InnerException.TypeOf<ArgumentException>());
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
        Assert.That(() => Dummy(), Throws.Exception.Message.Contains(""the message"").And.InstanceOf<ArgumentException>());
        Assert.That(() => Dummy(), Throws.InnerException.TypeOf<ArgumentException>());
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
        public void TestHandlesWithProperty()
        {
            const string actual = @"
using NUnit.Framework;
public class FooTests
{ 
    string Dummy() { return ""ParamName""; }
    void Test()
    {
        Assert.That(() => Dummy(), Throws.TypeOf<ArgumentException>().With.Property(""ParamName"").Contains(""arg0""));
        Assert.That(() => Dummy(), Throws.TypeOf<ArgumentException>().With.Property(nameof(ArgumentException.ParamName)).Contains(""arg0""));
        Assert.That(() => Dummy(), Throws.TypeOf<ArgumentException>().With.Property(Dummy()).Contains(""arg0""));
        Assert.That(() => Dummy(), Throws.TypeOf<ArgumentException>().With.Property(""It's invalid"").Contains(""arg0""));
    }
}
";
            const string expected = @"
using Microsoft.VisualStudio.TestTools.UnitTesting;
public class FooTests
{ 
    string Dummy() { return ""ParamName""; }
    void Test()
    {
        StringAssert.Contains(Assert.ThrowsException<ArgumentException>(() => Dummy()).ParamName,""arg0"");
        StringAssert.Contains(Assert.ThrowsException<ArgumentException>(() => Dummy()).ParamName,""arg0"");
        Assert.That(() => Dummy(), Throws.TypeOf<ArgumentException>().With.Property(Dummy()).Contains(""arg0""));
        Assert.That(() => Dummy(), Throws.TypeOf<ArgumentException>().With.Property(""It's invalid"").Contains(""arg0""));
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
        public void TestHandlesWithMessage()
        {
            const string actual = @"
using NUnit.Framework;
public class FooTests
{ 
    void Dummy() { }
    void Test()
    {
        Assert.That(() => Dummy(), Throws.TypeOf<InvalidOperationException>().With.Message.Contains(""the message""));
        Assert.That(() => Dummy(), Throws.TypeOf<InvalidOperationException>().With.Message.StartsWith(""the message""));
        Assert.That(() => Dummy(), Throws.TypeOf<InvalidOperationException>().With.Message.StartWith(""the message""));
        Assert.That(() => Dummy(), Throws.TypeOf<InvalidOperationException>().With.Message.EndsWith(""the message""));
        Assert.That(() => Dummy(), Throws.TypeOf<InvalidOperationException>().With.Message.EndWith(""the message""));
        Assert.That(() => Dummy(), Throws.TypeOf<InvalidOperationException>().With.Message.EqualTo(""the message""));
        Assert.That(() => Dummy(), Throws.TypeOf<InvalidOperationException>().With.Message.Match(""the message""));
        Assert.That(() => Dummy(), Throws.TypeOf<InvalidOperationException>().With.Message.Matches(""the message""));
        Assert.That(() => Dummy(), Throws.Exception.Message.Contains(""the message""));
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
        StringAssert.Contains(Assert.ThrowsException<InvalidOperationException>(() => Dummy()).Message,""the message"");
        StringAssert.StartsWith(Assert.ThrowsException<InvalidOperationException>(() => Dummy()).Message,""the message"");
        StringAssert.StartsWith(Assert.ThrowsException<InvalidOperationException>(() => Dummy()).Message,""the message"");
        StringAssert.EndsWith(Assert.ThrowsException<InvalidOperationException>(() => Dummy()).Message,""the message"");
        StringAssert.EndsWith(Assert.ThrowsException<InvalidOperationException>(() => Dummy()).Message,""the message"");
        Assert.AreEqual(Assert.ThrowsException<InvalidOperationException>(() => Dummy()).Message,""the message"");
        StringAssert.Matches(Assert.ThrowsException<InvalidOperationException>(() => Dummy()).Message,new System.Text.RegularExpressions.Regex(""the message""));
        StringAssert.Matches(Assert.ThrowsException<InvalidOperationException>(() => Dummy()).Message,new System.Text.RegularExpressions.Regex(""the message""));
        StringAssert.Contains(Assert.ThrowsException<Exception>(() => Dummy()).Message,""the message"");
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
        public void TestThrowsMoreArgs()
        {
            const string actual = @"
using NUnit.Framework;
public class FooTests
{ 
    void Dummy() { }
    void Test()
    {
        Assert.That(Dummy, Throws.ArgumentNullException, ""message {0}"", 1);
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
        Assert.ThrowsException<ArgumentNullException>(Dummy,""message {0}"",1);
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
            // TODO: This doesn't work in practice, because Assert.ThrowsException<Exception>(),
            // will fail if not exactly System.Exception is thrown.
            // We would need to do something like this:
            // try {
            //     /* test */
            //     Assert.Fail($"Expected exception of type {exceptionType}.");
            // } catch (Exception ex) when (!(ex is AssertFailException)) {
            //     Assert.IsInstanceOfType(ex, exceptionType);
            // }
            // The "ex" variable name would need to be generated, so that it doesn't
            // collide with an existing name in the same scope.
#if false

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
        Assert.IsInstanceOfType(Assert.ThrowsException<Exception>(Dummy),typeof(OutOfMemoryException));
        Assert.IsInstanceOfType(Assert.ThrowsException<Exception>(() => Dummy()),typeof(OutOfMemoryException));
        Assert.IsInstanceOfType(Assert.ThrowsException<Exception>(() => Dummy()),typeof(OutOfMemoryException));
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
#endif
        }

        [TestMethod]
        public void TestOther()
        {
            const string actual = @"
//using NUnit.Framework;
public class FooTests
{ 
    void Test()
    {
        int j;
        int i = 0;
        string s = ""
        s += i;
        System.Console.WriteLine(s);
        {
            string otto;
        }

try
            {

            }
            catch (Exception ex)
            {
            }
    }

    void Test()
    {
        double a;
    }
}
";
            const string expected = @"
";
            TestRefactoringWithAsserts(actual, expected,
                (result, rw) =>
                {
                    //Assert.IsTrue(rw.Changed);
                    //Assert.AreEqual(0, rw.Diagnostics.Count());
                    //Assert.AreEqual(expected, result.ToFullString());
                });
        }
    }
}