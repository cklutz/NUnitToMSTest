using System;
using System.Collections.Generic;
using System.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MSTestSample
{
    [TestClass]
    public class MSTestSample
    {
        //[TestInitialize]
        //public void Initialize()
        //{

        //}

        //[TestCleanup]
        //public void Cleanup()
        //{

        //}

        //[ClassInitialize]
        //public static void ClassInitialize()
        //{

        //}

        //[ClassCleanup]
        //public void ClassCleanup()
        //{

        //}

        [TestMethod]
        [DynamicData(nameof(BytePoolInstances), DynamicDataSourceType.Method)]
        public void UsePoolInParallel(int arg1, int arg2)
        {
            Console.WriteLine(arg1 + " - " + arg2);
        }

        public static IEnumerable<object[]> BytePoolInstances()
        {
            yield return new object[] { 1, 2 };
            yield return new object[] { 3, 4 };
        }

        [TestMethod]
        //[TestCategory("ddlld")]
        ////[TestProperty("doubleProp", 0.1)]
        ////[TestProperty("intProp", 1)]
        //[TestProperty("stringProp", "dkdkd")]
        //[Description("blah")]
        public void TestMethod1()
        {
            Assert.IsInstanceOfType(Assert.ThrowsException<Exception>(() => { throw new Exception("dkddk"); }), typeof(ArgumentException));

            

        }

        [DataTestMethod]
        [DataRow(1)]
        [Ignore("xxx")]
        public void DataTestMethod1(int i)
        {
            var l1 = new List<int>();
            var l2 = new List<int>();
            CollectionAssert.AreEqual(l1, l2);

            try
            {

            }
            catch (Exception ex)
            {
            }

            {
                int j;
            }

            {
                int j;
            }

            {
                int j;
            }
        }
    }
}
