using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using Tallinn.Models.Textual;

namespace Tallinn.Models.Members
{
    public sealed class MethodDocumentation : MemberDocumentation
    {
        public override ITextualDocumentation? Summary { get; set; }
        public override ITextualDocumentation? Remarks { get; set; }
        public override List<ITextualDocumentation?> SeeAlso { get; set; } = new();
        public override List<ITextualDocumentation?> Examples { get; set; } = new();
        public ConcurrentDictionary<string, ITextualDocumentation?> TypeParameters { get; set; } = new();

        /// <summary>
        /// Lines of C# code representing:
        /// <list type="bullet">
        /// <item>Attributes on the method</item>
        /// <item>The method declaration line (including inheritance)</item>
        /// <item>Parameter declarations (including attributes)</item>
        /// <item>Generic type parameter constraints</item>
        /// </list>
        /// </summary>
        public override List<string> DeclarationLines { get; set; } = new();

        public ConcurrentDictionary<string, ITextualDocumentation?> Parameters { get; set; } = new();
        public ITextualDocumentation? Returns { get; set; }
    }
}