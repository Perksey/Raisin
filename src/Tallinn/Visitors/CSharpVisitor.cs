using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Tallinn.Models;

namespace Tallinn.Visitors
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