# How to switch from NUnit to MSTest V2

## Preparation

Make sure your sources compile and all tests are successfull with your
current test framework (NUnit).

Additionally, it is highly advisable to have your sources in version
control; if you get lost in the migration process you can always
(partially) revert what you have done.

## Converting a Project

Since `vstest.console.exe`, and Visual Studio for that matter, run unit tests
by the means of an adapter (there is one for NUnit and MSTest), you can have
multiple projects with different test frameworks. So, it is suggested that
you convert one test project at a time.

The basic steps are as follows:

* Remove NUnit references / NuGet packages.
* Add MSTest.TestFramework and MSTest.TestAdapter NuGet packages.
* Change test related attributes.
* Change `Assert` API calls.

Most of these steps are rather boilerplate and can be done by a tool
([NUnitToMSTest](https://github.com/cklutz/NUnitToMSTest)). If you employ
the tool, make sure to check the "Error List" and "Output" windows of
Visual Studio after converting a project for potential hints of not
understood or incomplete transformations. Afterwards compile the
test project and manually fix the remaining migration issues (see
following chapter).

## Typical Migration Issues

### OneTimeSetUp, OneTimeTearDown -> ClassInitialize, ClassCleanup

In NUnit, these can be instance methods of any accessibility.
In MSTest they have to be `public` and `static`. Also, the method
attributed with `ClassInitialize` (which is the MSTest version of
`OneTimeSetUp`) must accept a parameter of type `TestContext`.

Make sure, though you don't use this reference inside individual
tests. Since it is created during the setup phase, the values
concerning the current test (e.g. `TestName`) will not be filled.
(For this expose a public property of type `TestContext` on your
test (base) class and use its value inside tests for current context
information).

### TestDataSource -> DynamicData

The NUnit `TestDataSource`-attribute is much more flexible than
the MSTest `DynamicData`-attribute. For the method (or property)
you specify as the source of the data, you can pass additional
arguments, etc. However, basic usage can be easily converted.

There are two gotchas however:

* The return type of the data source method / property for MSTest
  must by `object[]`, where each element of the array is one
  parameter for the test method.

* You must indicate whether the source is a method (`DynamicDataSourceType.Method`)
  or a property (`DynamicDataSourceType.Property`). Fields are not supported.
  The default is assumed to be a property.

### Assert.AreEqual on collections

In NUnit `Assert.AreEqual` also works with collections. In MSTest you
have to explicitly use `CollectionAssert.AreEqual`, which would have
also been available in NUnit, btw.

### CollectionAssert overloads

As with many assert functions, the `CollectionAssert` class is a little
less explicit with overloads compared to NUnit. NUnit, generally, allows
you to compare collections of different base-type: e.g. an array with
a List<T>. For MSTest I have found it to be useful to simply convert
all types of "collections" to an array, e.g.

        CollectionAssert.AreEqual(col1.ToArray(), col2.ToArray())

Thanks to LINQ this is a syntactical no-brainer. I assume that for
unit tests to potential extra allocations don't matter. YMMV.

### Missing assert functions

As already said, MSTest has quite less assert functions than NUnit.
Some of NUnit are historical variants, it seems, but some are actually
quite useful.

You basically have two approaches here:

* Emulate missing asserts with existing ones and extra code.
* Write assert extensions (which is [supported](https://github.com/Microsoft/testfx-docs/blob/master/RFCs/002-Framework-Extensibility-Custom-Assertions.md))
  for those you commonly need.

One example, the NUnit `Assert.That(() => {}, Throws.Exception.InstanceOf<...>())` does not exist
in MSTest as such. The MSTest `Assert.ThrowsException<...>(() => {})` tries to match the exact
exception type, while the NUnit constract does not - as long as the inheritance hierarchy matches.

First of all, you should think about whether you really need/want the weaker guarantee that 
`InstanceOf` provices (the stricter counterpart in NUnit would have been `TypeOf`, btw.).
Most tests can be rewritten to also work with `Assert.ThrowsException<>`. But nevertheless
for the sake of this example, you could do something like this:

    try 
    {
        /* test */
        Assert.Fail($"Expected exception of type {exceptionType}.");
    }
    catch (Exception ex) when (!(ex is AssertFailException)) 
    {
        Assert.IsInstanceOfType(ex, exceptionType);
    }


Generally, it must be said that the less rich assert APIs of MSTest force you
to write less "advanced" unit tests, which can be, arguably, a good thing.
