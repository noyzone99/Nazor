using System.Globalization;

namespace Nazor.Controls;

internal static class NumberExpression
{
    internal static bool TryEvaluate(ReadOnlySpan<char> text, CultureInfo culture, out double value)
    {
        value = 0;
        if (text.Length > 4096) return false;
        var parser = new Parser(text, culture);
        return parser.ReadExpression(out value) && parser.AtEnd;
    }

    private ref struct Parser
    {
        private readonly ReadOnlySpan<char> text;
        private readonly CultureInfo culture;
        private int position;
        private int depth;

        internal Parser(ReadOnlySpan<char> text, CultureInfo culture)
        {
            this.text = text;
            this.culture = culture;
        }

        internal bool AtEnd
        {
            get { SkipSpace(); return position == text.Length; }
        }

        internal bool ReadExpression(out double value)
        {
            if (!ReadProduct(out value)) return false;
            while (true)
            {
                SkipSpace();
                if (position == text.Length || text[position] is not ('+' or '-')) return true;
                var operation = text[position++];
                if (!ReadProduct(out var right)) return false;
                value = operation == '+' ? value + right : value - right;
                if (!double.IsFinite(value)) return false;
            }
        }

        private bool ReadProduct(out double value)
        {
            if (!ReadPower(out value)) return false;
            while (true)
            {
                SkipSpace();
                if (position == text.Length || text[position] is not ('*' or '/')) return true;
                var operation = text[position++];
                if (!ReadPower(out var right) || operation == '/' && right == 0) return false;
                value = operation == '*' ? value * right : value / right;
                if (!double.IsFinite(value)) return false;
            }
        }

        private bool ReadPower(out double value)
        {
            if (!ReadSignedOperand(out value)) return false;
            while (true)
            {
                SkipSpace();
                if (position == text.Length || text[position] != '^') return true;
                position++;
                if (!ReadSignedOperand(out var right)) return false;
                value = Math.Pow(value, right);
                if (!double.IsFinite(value)) return false;
            }
        }

        private bool ReadSignedOperand(out double value)
        {
            SkipSpace();
            var negative = false;
            if (position < text.Length && text[position] is '+' or '-') negative = text[position++] == '-';
            if (!ReadOperand(out value)) return false;
            if (negative) value = -value;
            return true;
        }

        private bool ReadOperand(out double value)
        {
            value = 0;
            SkipSpace();
            if (position == text.Length) return false;
            if (text[position] == '(')
            {
                if (++depth > 64) return false;
                position++;
                if (!ReadExpression(out value)) return false;
                SkipSpace();
                if (position == text.Length || text[position++] != ')') return false;
                depth--;
                return true;
            }

            var start = position;
            while (position < text.Length)
            {
                var character = text[position];
                if (character is '(' or ')' or '*' or '/' or '^') break;
                if (character is '+' or '-' && (position == start || text[position - 1] is not ('e' or 'E'))) break;
                position++;
            }
            return double.TryParse(text[start..position].Trim(), NumberStyles.Float | NumberStyles.AllowThousands, culture, out value)
                && double.IsFinite(value);
        }

        private void SkipSpace()
        {
            while (position < text.Length && char.IsWhiteSpace(text[position])) position++;
        }
    }
}
