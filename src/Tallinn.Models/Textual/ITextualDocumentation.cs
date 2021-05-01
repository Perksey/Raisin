using System.Collections.Generic;

namespace Tallinn.Models.Textual
{
    public interface ITextualDocumentation
    {
        List<ITextualDocumentation> Children { get; set; }
    }
}