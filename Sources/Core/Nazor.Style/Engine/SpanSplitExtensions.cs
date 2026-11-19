namespace Nazor.Style;

internal static class SpanSplitExtensions
{
    public static SplitEnumerator SplitAny(this ReadOnlySpan<char> value, ReadOnlySpan<char> separators) => new(value, separators);

    internal ref struct SplitEnumerator(ReadOnlySpan<char> value, ReadOnlySpan<char> separators)
    {
        private ReadOnlySpan<char> remaining = value;
        private readonly ReadOnlySpan<char> separators = separators;
        private int offset;
        private bool finished;

        public Range Current { get; private set; }
        public SplitEnumerator GetEnumerator() => this;

        public bool MoveNext()
        {
            if (finished) return false;
            var separator = remaining.IndexOfAny(separators);
            var length = separator < 0 ? remaining.Length : separator;
            Current = offset..(offset + length);
            if (separator < 0) finished = true;
            else
            {
                offset += length + 1;
                remaining = remaining[(length + 1)..];
            }
            return true;
        }
    }
}
