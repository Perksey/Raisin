using System;
using System.IO;
using System.Xml;
using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Tallinn.Models;
using Tallinn.Models.Types;
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
        
        public InterfaceDocumentation? CurrentInterface { get; private set; }

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
            GetProject().GetOrCreateNamespace(ns, out var ret);
            return ret;
        }

        public ClassDocumentation GetClass(string ns, string name)
        {
            var retrieved = GetNamespace(ns).GetOrCreateClass(name, out var ret);
            if (retrieved == RetrievalResult.ErrorExistedTypeMismatch)
            {
                static void Throw()
                    => throw new InvalidOperationException("Existing type with the given name was not a class!");

                Throw();
            }

            return ret;
        }

        public StructDocumentation GetStruct(string ns, string name)
        {
            var retrieved = GetNamespace(ns).GetOrCreateStruct(name, out var ret);
            if (retrieved == RetrievalResult.ErrorExistedTypeMismatch)
            {
                static void Throw()
                    => throw new InvalidOperationException("Existing type with the given name was not a struct!");

                Throw();
            }

            return ret;
        }

        public RecordDocumentation GetRecord(string ns, string name)
        {
            var retrieved = GetNamespace(ns).GetOrCreateRecord(name, out var ret);
            if (retrieved == RetrievalResult.ErrorExistedTypeMismatch)
            {
                static void Throw()
                    => throw new InvalidOperationException("Existing type with the given name was not a record!");

                Throw();
            }

            return ret;
        }

        public DelegateDocumentation GetDelegate(string ns, string name)
        {
            var retrieved = GetNamespace(ns).GetOrCreateDelegate(name, out var ret);
            if (retrieved == RetrievalResult.ErrorExistedTypeMismatch)
            {
                static void Throw()
                    => throw new InvalidOperationException("Existing type with the given name was not a delegate!");

                Throw();
            }

            return ret;
        }

        public InterfaceDocumentation GetInterface(string ns, string name)
        {
            var retrieved = GetNamespace(ns).GetOrCreateInterface(name, out var ret);
            if (retrieved == RetrievalResult.ErrorExistedTypeMismatch)
            {
                static void Throw()
                    => throw new InvalidOperationException("Existing type with the given name was not a interface!");

                Throw();
            }

            return ret;
        }
    }
}