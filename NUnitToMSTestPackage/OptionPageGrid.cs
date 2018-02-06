using System;
using System.ComponentModel;
using Microsoft.VisualStudio.Shell;

namespace NUnitToMSTestPackage
{
    public class OptionPageGrid : DialogPage, IOptions
    {
        [Category("NUnit To MSTest")]
        [DisplayName("Transform Assert Statements")]
        [Description("Transform Assert statements.")]
        public bool TransformAsserts { get; set; } = true;

        [Category("NUnit To MSTest")]
        [DisplayName("MSTest Package Version")]
        [Description("The version of the MSTest NuGet packages to install. Leave empty to not attempt to install NuGet packages.")]
        public string MSTestPackageVersion { get; set; } = "1.2.0";

        [Category("NUnit To MSTest")]
        [DisplayName("Make sure Project file has UnitTest type")]
        [Description("Make sure Project file has UnitTest type.")]
        public bool MakeSureProjectFileHasUnitTestType { get; set; } = true;

        [Category("NUnit To MSTest")]
        [DisplayName("Uninstall NUnit packages")]
        [Description("Uninstall NUnit packages.")]
        public bool UninstallNUnitPackages { get; set; } = true;
    }
}
