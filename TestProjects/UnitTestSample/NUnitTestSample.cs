using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace UnitTestSample
{
    //[TestFixture]
    [NUnit.Framework.TestFixture(Author = "Hello")]
    [Explicit()]
    [Description("blah")]
    public class NUnitTestSample
    {
        [SetUp]
        public void Initialize()
        {

        }

        [TearDown]
        public void Cleanup()
        {
        }

        [OneTimeSetUp]
        public static void ClassInitialize()
        {
            Console.WriteLine("OneTimeSetup");
        }

        [OneTimeTearDown]
        public static void ClassCleanup()
        {
            Console.WriteLine("OneTimeTearDown");
        }
        
        [Test]
        public void Vanilla()
        {
            Assert.IsInstanceOf(typeof(Type), GetType());
            Assert.IsInstanceOf<Type>(GetType());
        }

        [Ignore("Message", Until = "Forever")]
        [Property("doubleProp", 0.1)]
        [Property("intProp", 1)]
        [Property("stringProp", "dkdkd")]
        public void Ingore()
        {

        }

        [Test(Description = "The description")]
        [Category("ddddd")]
        [Explicit("With Reason")]
        public void Test()
        {
            Assert.That(() => { int i = 0; i++; }, Throws.ArgumentNullException, "message {0}", 1234);
            Assert.That(() => Dummy(), Throws.ArgumentNullException);
            Assert.That(() => Dummy(), Throws.ArgumentNullException);
            Assert.That(Dummy, Throws.ArgumentNullException);
            Assert.That(() => Dummy(), Throws.InstanceOf<OutOfMemoryException>());
            Assert.That(() => Dummy(), Throws.TypeOf<OutOfMemoryException>());
            Assert.That(() => Dummy(), Throws.Exception.TypeOf<OutOfMemoryException>());

            Assert.That(1.Foo(), "dkdkdkdk");
        }

        private void Dummy() {}

        [TestCaseSource("sourceName")]
        [TestCaseSource(typeof(NUnitTestSample), "sourceName")]
        public void TestDataSource(int x)
        {
        }



        /// <summary>
        /// dkddk
        /// </summary>
        /// <param name="i"></param>
        /// <param name="s"></param>
        [TestCase(0, "0", TestName = "The test name")]
        [Theory]
        [NUnit.Framework.Author("Foo")]
        [Description("blah")]
        /**/
        public void TestCase(int i, string s)
        {
            Assert.Null(null);
            Assert.IsNull(null);

            Assert.Less(0, 0);
            Assert.LessOrEqual(0, 0);
            Assert.Greater(0, 0);
            Assert.GreaterOrEqual(0, 0);
        }
    }

    internal static class FooExtensions
    {
        public static bool Foo(this int i)
        {
            return true;
        }
    }
}
