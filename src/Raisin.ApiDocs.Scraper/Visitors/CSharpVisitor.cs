using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Raisin.ApiDocs.Models;

namespace Raisin.ApiDocs.Scraper.Visitors
{
    public partial class CSharpVisitor : CSharpSyntaxVisitor
    {
        public CSharpVisitor(Documentation documentation, Project project, Compilation compilation)
        {
            Documentation = documentation;
            Project = project;
            Compilation = compilation;
        }

        public Documentation Documentation { get; }
        public Project Project { get; }
        public Compilation Compilation { get; }
        
        
    }
}