using Nazor.Controls;

namespace Nazor.Samples.Gallery;

public static partial class GalleryCatalog
{
    // Pages of the "Text and content" group.
    private static IEnumerable<GalleryEntry> TextEntries() =>
    [
        new("text", "TextBlock", "Text and content", "Display text, optionally selectable and copyable.", typeof(TextBlock), typeof(Examples.Text.TextBlockSample)),
        new("heading", "Heading", "Text and content", "Express the levels of the document outline.", typeof(Heading), typeof(Examples.Text.HeadingSample)),
        new("shortcut-label", "ShortcutLabel", "Text and content", "Show a keyboard shortcut in the notation of the current platform.", typeof(ShortcutLabel), typeof(Examples.Text.ShortcutLabelSample)),
        new("image", "Image", "Text and content", "Show an image with an accessible alternative.", typeof(Image), typeof(Examples.Text.ImageSample)),
        new("icon", "Icon", "Text and content", "Show a monochrome glyph or image in the current color.", typeof(Icon), typeof(Examples.Text.IconSample)),
        new("formatted-text", "Paragraph and inline formatting", "Text and content", "Show selectable paragraphs, styled running text, keyboard keys, quotations and code.", typeof(Paragraph), typeof(Examples.Text.FormattedTextSample),
            typeof(Kbd), typeof(Blockquote), typeof(CodeBlock), typeof(Span), typeof(Strong), typeof(Emphasis), typeof(InlineCode), typeof(Mark), typeof(Abbreviation),
            typeof(Deleted), typeof(Inserted), typeof(Subscript), typeof(Superscript), typeof(SmallText), typeof(Quote), typeof(LineBreak)),
        new("document-lists", "List and DefinitionList", "Text and content", "Compose nested lists and pairs of terms and descriptions.", typeof(List), typeof(Examples.Text.DocumentListsSample), typeof(ListItem), typeof(DefinitionList), typeof(DefinitionTerm), typeof(DefinitionDescription)),
        new("figure", "Figure", "Text and content", "Associate a caption with a piece of document content.", typeof(Figure), typeof(Examples.Text.FigureSample), typeof(FigureCaption)),
    ];
}
