using System.Text;

namespace Nazor.Controls.Primitives;

/// <summary>
/// An input mask such as <c>(000) 000-0000</c> or <c>AA-0000</c>, and the rules that fit typed or pasted text into it.
/// Mask characters: <c>0</c> a digit, <c>9</c> an optional digit, <c>A</c> a letter, <c>a</c> an optional letter,
/// <c>*</c> any character, <c>\</c> makes the next character literal; every other character is a literal written by the
/// mask. <see cref="Apply" /> reads text left to right: a character that fits the next slot fills it (optional slots that
/// it does not fit are skipped), a character equal to the next literal is kept, any other character is dropped. Literals
/// the user did not type are written only before the next filled slot, so the text never ends with a literal the user
/// would have to delete, and deleting the last character also removes the literals before it.
/// </summary>
public sealed class TextMask
{
    private readonly Token[] tokens;

    private enum Kind
    {
        Literal,
        Digit,
        OptionalDigit,
        Letter,
        OptionalLetter,
        Any,
    }

    private readonly record struct Token(Kind Kind, char Literal)
    {
        public bool IsSlot => Kind != Kind.Literal;

        public bool IsOptional => Kind is Kind.OptionalDigit or Kind.OptionalLetter;

        public bool Accepts(char c) => Kind switch
        {
            Kind.Digit or Kind.OptionalDigit => char.IsDigit(c),
            Kind.Letter or Kind.OptionalLetter => char.IsLetter(c),
            Kind.Any => !char.IsControl(c),
            _ => false,
        };
    }

    /// <summary>Parses a mask.</summary>
    /// <param name="mask">The mask pattern.</param>
    /// <exception cref="ArgumentException">The mask is empty, has no slot or ends with an escape.</exception>
    public TextMask(string mask)
    {
        ArgumentException.ThrowIfNullOrEmpty(mask);
        var parsed = new List<Token>(mask.Length);
        for (var i = 0; i < mask.Length; i++)
        {
            var c = mask[i];
            if (c == '\\')
            {
                if (++i >= mask.Length) throw new ArgumentException("The mask ends with an escape character.", nameof(mask));
                parsed.Add(new Token(Kind.Literal, mask[i]));
                continue;
            }
            parsed.Add(c switch
            {
                '0' => new Token(Kind.Digit, '\0'),
                '9' => new Token(Kind.OptionalDigit, '\0'),
                'A' => new Token(Kind.Letter, '\0'),
                'a' => new Token(Kind.OptionalLetter, '\0'),
                '*' => new Token(Kind.Any, '\0'),
                _ => new Token(Kind.Literal, c),
            });
        }
        if (!parsed.Any(t => t.IsSlot)) throw new ArgumentException("The mask has no editable position.", nameof(mask));
        tokens = [.. parsed];
        Pattern = mask;
    }

    /// <summary>Gets the mask pattern.</summary>
    public string Pattern { get; }

    /// <summary>Gets the number of editable positions.</summary>
    public int SlotCount => tokens.Count(t => t.IsSlot);

    /// <summary>Gets the number of positions that must be filled.</summary>
    public int RequiredSlotCount => tokens.Count(t => t.IsSlot && !t.IsOptional);

    /// <summary>Fits text into the mask.</summary>
    /// <param name="input">The typed, pasted or stored text, raw or formatted.</param>
    /// <param name="prompt">The character shown for each empty position in <see cref="MaskResult.Prompt" />.</param>
    /// <returns>The formatted text, the raw characters, the completion and the remaining prompt.</returns>
    public MaskResult Apply(string input, char prompt = '_')
    {
        ArgumentNullException.ThrowIfNull(input);
        var text = new StringBuilder();
        var raw = new StringBuilder();
        var pending = new StringBuilder();
        var position = 0;
        var required = 0;
        foreach (var c in input)
        {
            var (savedPosition, savedPending) = (position, pending.ToString());
            var placed = false;
            while (position < tokens.Length)
            {
                var token = tokens[position];
                if (!token.IsSlot)
                {
                    position++;
                    if (c == token.Literal)
                    {
                        text.Append(pending).Append(c);
                        pending.Clear();
                        placed = true;
                        break;
                    }
                    pending.Append(token.Literal);
                    continue;
                }
                if (token.Accepts(c))
                {
                    text.Append(pending).Append(c);
                    pending.Clear();
                    raw.Append(c);
                    if (!token.IsOptional) required++;
                    position++;
                    placed = true;
                    break;
                }
                if (!token.IsOptional) break;
                position++;
            }
            if (placed) continue;
            position = savedPosition;
            pending.Clear().Append(savedPending);
        }
        var complete = required == RequiredSlotCount && Enumerable.Range(position, tokens.Length - position).All(i => !tokens[i].IsSlot || tokens[i].IsOptional);
        var full = new StringBuilder(text.ToString());
        if (complete)
            for (var i = position; i < tokens.Length && !tokens[i].IsSlot; i++) full.Append(tokens[i].Literal);
        var remaining = new StringBuilder(pending.ToString());
        for (var i = position; i < tokens.Length; i++) remaining.Append(tokens[i].IsSlot ? prompt : tokens[i].Literal);
        return new MaskResult(text.ToString(), raw.ToString(), full.ToString(), complete, remaining.ToString());
    }
}

/// <summary>Text fitted into a <see cref="TextMask" />.</summary>
/// <param name="Text">The formatted text as edited: the typed characters with the literals before them.</param>
/// <param name="Raw">Only the characters that fill editable positions.</param>
/// <param name="Formatted">The formatted text, with the trailing literals once the mask is complete.</param>
/// <param name="IsComplete">Whether every required position is filled.</param>
/// <param name="Prompt">The rest of the mask after the text, with a prompt character for each empty position.</param>
public readonly record struct MaskResult(string Text, string Raw, string Formatted, bool IsComplete, string Prompt);
