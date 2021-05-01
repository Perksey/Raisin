using System.Collections.Generic;
using Tallinn.Models.References;
using Tallinn.Models.Textual;

namespace Tallinn.Models.Members
{
    public abstract class MemberDocumentation : BaseXmlDocumentation
    {
        public string? Name { get; set; }
        public override ITextualDocumentation? Summary { get; set; }
        public override ITextualDocumentation? Remarks { get; set; }
        public override List<ITextualDocumentation?> SeeAlso { get; set; } = new();
        public override List<ITextualDocumentation?> Examples { get; set; } = new();

        /// <summary>
        /// Lines of C# code representing:
        /// <list type="bullet">
        /// <item>Attributes on the member</item>
        /// <item>The member declaration line</item>
        /// </list>
        /// </summary>
        public override List<string> DeclarationLines { get; set; } = new();
    }
}