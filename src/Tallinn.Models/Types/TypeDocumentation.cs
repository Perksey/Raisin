using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Tallinn.Models.References;
using Tallinn.Models.Textual;

namespace Tallinn.Models.Types
{
    public abstract class TypeDocumentation : BaseXmlDocumentation
    {
        public string? TypeName { get; set; }
        public Accessibility Access { get; set; }
        public override ITextualDocumentation? Summary { get; set; }
        public override ITextualDocumentation? Remarks { get; set; }
        public override List<ITextualDocumentation?> SeeAlso { get; set; } = new();
        public override List<ITextualDocumentation?> Examples { get; set; } = new();
        public ConcurrentDictionary<string, TypeParameterDocumentation> TypeParameters { get; set; } = new();

        /// <summary>
        /// Lines of C# code representing:
        /// <list type="bullet">
        /// <item>Attributes on the type</item>
        /// <item>The type declaration line (including inheritance)</item>
        /// <item>Generic type parameter constraints</item>
        /// </list>
        /// </summary>
        public override List<string> DeclarationLines { get; set; } = new();
    }
}