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
    }
}
