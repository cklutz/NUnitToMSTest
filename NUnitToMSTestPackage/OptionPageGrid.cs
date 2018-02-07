using System;
using System.Collections.Generic;
using System.ComponentModel;
using Microsoft.VisualStudio.Shell;

namespace NUnitToMSTestPackage
{
    public class OptionPageGrid : DialogPage, IOptions
    {
        [Category(PackageConstants.Title)]
        [DisplayName("Transform Assert Statements")]
        [Description("Transform Assert statements.")]
        public bool TransformAsserts { get; set; } = true;

        [Category(PackageConstants.Title)]
        [DisplayName("MSTest Package Version")]
        [Description("The version of the MSTest NuGet packages to install. Leave empty to not attempt to install NuGet packages.")]
        public string MSTestPackageVersion { get; set; } = "1.2.0";

        [Category(PackageConstants.Title)]
        [DisplayName("Make sure Project file has UnitTest type")]
        [Description("Adds potentially missing properties to project to make it look like a Unit-Test project to Visual Studio.")]
        public bool MakeSureProjectFileHasUnitTestType { get; set; } = true;

        [Category(PackageConstants.Title)]
        [DisplayName("Uninstall NUnit Packages")]
        [Description("Uninstall NUnit packages.")]
        public bool UninstallNUnitPackages { get; set; } = true;

        [Category(PackageConstants.Title)]
        [DisplayName("Uninstall NUnit Packages List")]
        [Description("If 'uninstall NUnit packages' is selected, attempt to uninstall the following packages.")]
        public string[] NUnitPackages { get; set; } = {
            "NUnit3Adapter",
            "NUnit.Console",
            "NUnit"
        };

        [Category(PackageConstants.Title)]
        [DisplayName("Install MSTest Packages")]
        [Description("Install MSTest packages.")]
        public bool InstallMSTestPackages { get; set; } = true;

        [Category(PackageConstants.Title)]
        [DisplayName("Install MSTest Packages List")]
        [Description("If 'install MSTest packages' is selected, install the following packages (format ID,Version).")]
        public string[] MSTestPackages { get; set; } = {
            "MSTest.TestAdapter,1.2.0",
            "MSTest.TestFramework,1.2.0"
        };
    }
}
