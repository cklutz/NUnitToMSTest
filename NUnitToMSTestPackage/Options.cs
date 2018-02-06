using System;

namespace NUnitToMSTestPackage
{
    public class Options : IOptions
    {
        private static Options s_instance;

        internal static void Initialize(N2MPackage package)
        {
            s_instance = new Options();

            var storage = (OptionPageGrid)package.GetDialogPage(typeof(OptionPageGrid));
            s_instance.TransformAsserts = storage.TransformAsserts;
            s_instance.MSTestPackageVersion = storage.MSTestPackageVersion;
            s_instance.UninstallNUnitPackages = storage.UninstallNUnitPackages;
        }

        public static IOptions Instance => s_instance;

        public bool TransformAsserts { get; private set; }
        public bool MakeSureProjectFileHasUnitTestType { get; private set; }
        public string MSTestPackageVersion { get; private set; }
        public bool UninstallNUnitPackages { get; private set; }
    }

    public interface IOptions
    {
        bool TransformAsserts { get; }
        string MSTestPackageVersion { get; }
        bool MakeSureProjectFileHasUnitTestType { get; }
        bool UninstallNUnitPackages { get; }
    }
}