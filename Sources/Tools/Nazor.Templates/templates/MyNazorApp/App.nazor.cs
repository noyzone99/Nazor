namespace MyNazorApp;

public partial class App
{
    private string name = "";
    private int count;

    private string Name => string.IsNullOrWhiteSpace(name) ? "Nazor" : name;

    private void Increment() => count++;
}
