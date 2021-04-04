using System.Collections.Immutable;
using Tallinn.Models.Textual;

namespace Tallinn.Models
{
    public class TypeDocumentation : BaseXmlDocumentation
    {
        public TypeKind Kind { get; set; }
        public string TypeName { get; set; }
        public override ITextualDocumentation? Summary { get; set; }
        public override ITextualDocumentation? Remarks { get; set; }
        public override ITextualDocumentation? SeeAlso { get; set; }
        public override ImmutableArray<ITextualDocumentation?> Examples { get; set; }

        /// <summary>
        /// Lines of C# code representing:
        /// <list type="bullet">
        /// <item>Attributes on the type</item>
        /// <item>The type declaration line (including inheritance)</item>
        /// </list>
        /// </summary>
        public override string[] DeclarationLines { get; set; }
    }
}