using System.IO;
using System.Xml;
using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Tallinn.Models;
using MSBuildProject = Microsoft.Build.Evaluation.Project;
using Project = Microsoft.CodeAnalysis.Project;

namespace Tallinn.Visitors
{
    public partial class CSharpVisitor : CSharpSyntaxVisitor
    {
        public CSharpVisitor(Documentation documentation, Project project, Compilation compilation)
        {
            Documentation = documentation;
            Project = project;
            Compilation = compilation;
            
            using var xmlReader = XmlReader.Create(File.OpenRead(project.FilePath!));
            MsBuildProject = new MSBuildProject(ProjectRootElement.Create(xmlReader, new ProjectCollection(), true));
        }

        public Documentation Documentation { get; }
        public Project Project { get; }
        public MSBuildProject MsBuildProject { get; }
        public Compilation Compilation { get; }
        
        public NamespaceDocumentation CurrentNamespace { get; private set; }

        public ProjectDocumentation GetProject()
        {
            var retrieved = Documentation.GetOrCreateProject(Project.Name, out var ret);
            if (retrieved == RetrievalResult.Created)
            {
                ret.Name = Project.Name;
                ret.AssemblyName = Project.AssemblyName;
                ret.Description = MsBuildProject.GetPropertyValue("Description");
                ret.PackageId = MsBuildProject.GetPropertyValue("PackageId");
                if (string.IsNullOrWhiteSpace(ret.PackageId))
                {
                    ret.PackageId = ret.AssemblyName;
                }
            }

            return ret;
        }

        public NamespaceDocumentation GetNamespace(string ns)
        {
            var retrieved = GetProject().GetOrCreateNamespace(ns, out var ret);
            if (retrieved == RetrievalResult.Created)
            {
                ret.Namespace = ns;
            }

            return ret;
        }

        public TypeDocumentation GetType(string ns, string name)
        {
            var retrieved = GetNamespace(ns).GetOrCreateType(name, out var ret);
            if (retrieved == RetrievalResult.Created)
            {
                ret.TypeName = name;
            }

            return ret;
        }
    }
}