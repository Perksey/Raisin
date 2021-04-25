using Tallinn.Models.Textual;

namespace Tallinn.Models.Json
{
    internal enum TextKind
    {
        [ForType(typeof(InlineCodeText))] C,
        [ForType(typeof(CodeText))] Code,
        [ForType(typeof(DescriptionText))] Description,
        [ForType(typeof(ItemText))] Item,
        [ForType(typeof(ListText))] List,
        [ForType(typeof(ParagraphText))] Para,
        [ForType(typeof(ParameterReferenceText))] ParamRef,
        [ForType(typeof(PlainText))] Plain,
        [ForType(typeof(SeeText))] See,
        [ForType(typeof(TermText))] Term,
        [ForType(typeof(TypeParameterReferenceText))] TypeParamRef,
        [ForType(typeof(ValueText))] Value
    }
}