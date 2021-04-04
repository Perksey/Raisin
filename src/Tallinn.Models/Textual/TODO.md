ITextualDocumentation will be the type from which all XML elements will inherit. Off the top of my head, this includes:
- **InlineCodeText** - Represents the `<c>` attribute 
- **CodeText** - Represents the `<code>` attribute
- **ListText** - Represents the `<list>` attribute
- **ParagraphText** - Represents the `<para>` attribute
- **ParameterReferenceText** - Represents the `<paramref>` attribute
- **SeeText** - Represents the `<see>` attribute
- **TypeParameterReferenceText** - Represents the `<typeparamref>` attribute
- **ValueText** - Represents the `<value>` attribute
- **PlainText** - Represents any other words.

Basically, we're going to be forming our own AST for XML documentation. Anything not listed above will be present in the *Documentation classes, except:
- `<permission>` because wtf

For example, `<returns>` will have a `Returns` property in `MethodDocumentation`.