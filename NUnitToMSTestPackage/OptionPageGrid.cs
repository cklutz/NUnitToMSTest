using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

    public interface IOptions
    {
        bool TransformAsserts { get; }
    }
}
