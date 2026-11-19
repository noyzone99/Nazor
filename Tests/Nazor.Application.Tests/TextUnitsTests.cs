using Nazor.Accessibility;

namespace Nazor.Application.Tests;

public sealed class TextUnitsTests
{
    private const string Sentence = "Ciao, mondo!  Bello";

    [Theory]
    [InlineData("ciao", new[] { 0, 1, 2, 3, 4 })]
    [InlineData("a\U0001F44Db", new[] { 0, 1, 3, 4 })]
    [InlineData("e\u0301x", new[] { 0, 2, 3 })]
    [InlineData("", new[] { 0 })]
    public void Characters_are_graphemes(string text, int[] expected) =>
        Assert.Equal(expected, TextUnits.Boundaries(text, TextUnit.Character, []));

    [Fact]
    public void Words_take_their_spaces_and_punctuation_marks_are_words()
    {
        Assert.Equal([0, 4, 6, 11, 14, 19], TextUnits.Boundaries(Sentence, TextUnit.Word, []));
        Assert.Equal([0, 7, 9], TextUnits.Boundaries("caffè\u0301 ok", TextUnit.Word, []));
        Assert.Equal([0, 6], TextUnits.Boundaries("abc_12", TextUnit.Word, []));
    }

    [Fact]
    public void Lines_are_those_shown_and_paragraphs_end_with_a_line_break()
    {
        Assert.Equal([0, 5, 10], TextUnits.Boundaries("Ciao mondo", TextUnit.Line, [(0, 5), (5, 10)]));
        Assert.Equal([0, 2, 5], TextUnits.Boundaries("a\nbc\n", TextUnit.Paragraph, []));
        Assert.Equal([0, 19], TextUnits.Boundaries(Sentence, TextUnit.Document, []));
        Assert.Equal([0, 19], TextUnits.Boundaries(Sentence, TextUnit.Format, []));
    }

    [Theory]
    [InlineData(7, 6, 11)]
    [InlineData(0, 0, 4)]
    [InlineData(6, 6, 11)]
    [InlineData(19, 14, 19)]
    public void The_enclosing_unit_contains_the_position(int position, int start, int end) =>
        Assert.Equal((start, end), TextUnits.Enclosing(TextUnits.Boundaries(Sentence, TextUnit.Word, []), position));

    [Theory]
    [InlineData(7, 1, 11, 1)]
    [InlineData(7, 2, 14, 2)]
    [InlineData(7, -1, 6, -1)]
    [InlineData(6, -1, 4, -1)]
    [InlineData(18, 5, 19, 1)]
    [InlineData(0, -1, 0, 0)]
    public void A_position_moves_by_boundaries(int from, int count, int to, int moved)
    {
        var position = from;
        Assert.Equal(moved, TextUnits.Move(TextUnits.Boundaries(Sentence, TextUnit.Word, []), ref position, count));
        Assert.Equal(to, position);
    }

    [Theory]
    [InlineData(6, 11, 1, 11, 14, 1)]
    [InlineData(6, 11, 10, 14, 19, 2)]
    [InlineData(7, 8, -1, 4, 6, -1)]
    [InlineData(6, 8, 0, 6, 8, 0)]
    [InlineData(7, 7, 1, 11, 11, 1)]
    [InlineData(7, 7, -2, 4, 4, -2)]
    public void A_range_moves_by_whole_units_and_an_empty_one_stays_empty(int start, int end, int count, int newStart, int newEnd, int moved)
    {
        Assert.Equal(moved, TextUnits.MoveRange(TextUnits.Boundaries(Sentence, TextUnit.Word, []), ref start, ref end, count));
        Assert.Equal((newStart, newEnd), (start, end));
    }

    [Fact]
    public void An_empty_text_has_one_empty_unit()
    {
        var boundaries = TextUnits.Boundaries("", TextUnit.Word, []);
        Assert.Equal((0, 0), TextUnits.Enclosing(boundaries, 0));
        var (start, end) = (0, 0);
        Assert.Equal(0, TextUnits.MoveRange(boundaries, ref start, ref end, 1));
    }
}
