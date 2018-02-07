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
            s_instance.UninstallNUnitPackages = storage.UninstallNUnitPackages;
            s_instance.NUnitPackages = storage.NUnitPackages;
            s_instance.InstallMSTestPackages = storage.InstallMSTestPackages;
            s_instance.MSTestPackages = storage.MSTestPackages;
        }

        public static IOptions Instance => s_instance;
        public bool TransformAsserts { get; private set; }
        public bool MakeSureProjectFileHasUnitTestType { get; private set; }
        public bool UninstallNUnitPackages { get; private set; }
        public string[] NUnitPackages { get; private set; }
        public bool InstallMSTestPackages { get; private set; }
        public string[] MSTestPackages { get; private set; }
    }

    public interface IOptions
    {
        bool TransformAsserts { get; }
        bool MakeSureProjectFileHasUnitTestType { get; }

        bool UninstallNUnitPackages { get; }
        string[] NUnitPackages { get; }

        bool InstallMSTestPackages { get; }
        string[] MSTestPackages { get; }
    }
}