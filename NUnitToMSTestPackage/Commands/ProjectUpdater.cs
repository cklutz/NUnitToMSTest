using System;
using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;
using RoslynProject = Microsoft.CodeAnalysis.Project;

namespace NUnitToMSTestPackage.Commands
{
    internal class ProjectUpdater
    {
        private readonly RoslynProject m_project;

        public ProjectUpdater(RoslynProject project)
        {
            m_project = project;
        }

        public RoslynProject Update()
        {
            var projectCollection = ProjectCollection.GlobalProjectCollection;
            var projects = projectCollection.GetLoadedProjects(m_project.FilePath);
            foreach (var project in projects)
            {
                ProjectPropertyGroupElement group = null;

                SetPropertyIfNotSet(project, ref group, "ProjectTypeGuids", "{3AC096D0-A1C2-E12C-1390-A8335801FDAB};{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}");
                SetPropertyIfNotSet(project, ref group, "IsCodedUITest", "False", "'$(IsCodedUITest)' == ''");
                SetPropertyIfNotSet(project, ref group, "VSToolsPath", @"$(MSBuildExtensionsPath32)\Microsoft\VisualStudio\v$(VisualStudioVersion)", "'$(VSToolsPath)' == ''");
                SetPropertyIfNotSet(project, ref group, "ReferencePath", @"$(ProgramFiles)\Common Files\microsoft shared\VSTT\$(VisualStudioVersion)\UITestExtensionPackages");

                project.Save();
            }
            return m_project;
        }

        private void SetPropertyIfNotSet(Project project, ref ProjectPropertyGroupElement group, string name, string value, string condition = null)
        {
            string existingValue = project.GetPropertyValue(name);
            if (string.IsNullOrEmpty(existingValue))
            {
                group = group ?? project.Xml.AddPropertyGroup();
                var prop = group.AddProperty(name, value);
                if (condition != null)
                {
                    prop.Condition = condition;
                }
                project.MarkDirty();
            }
        }
    }
}