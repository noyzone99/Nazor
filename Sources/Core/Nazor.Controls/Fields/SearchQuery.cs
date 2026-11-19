namespace Nazor.Controls;

/// <summary>Describes an explicit search submission, including a suggestion chosen by the same Enter key.</summary>
/// <typeparam name="TItem">The suggestion type.</typeparam>
/// <param name="Text">The submitted text.</param>
/// <param name="ChosenSuggestion">The suggestion chosen by this submission, or the default value when none was chosen.</param>
/// <param name="HasChosenSuggestion">Whether this submission chose a highlighted suggestion.</param>
public readonly record struct SearchQuery<TItem>(string Text, TItem? ChosenSuggestion, bool HasChosenSuggestion);
