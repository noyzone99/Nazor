namespace MyNazorApp;

public sealed class Greeter(ILogger<Greeter> logger)
{
    public string Greet(string name)
    {
        logger.LogDebug("Greeting {Name}", name);
        return $"Hello, {name}!";
    }
}
