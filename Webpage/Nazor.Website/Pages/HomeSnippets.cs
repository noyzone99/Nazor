namespace Nazor.Website.Pages;

/// <summary>The component shown in the home page window, taken from the getting-started guide and the app template.</summary>
internal static class HomeSnippets
{
    public static readonly (string Name, string Language, string Code)[] All =
    [
        ("App.nazor", "razor", """
            @window
            <Container Class="page">
                <TextBlock Class="heading">Hello, @Name</TextBlock>
                <TextField @bind-Value="name" AccessibleName="Your name" />
                <Button OnClick="Increment">Count: @count</Button>
            </Container>

            @code {
                protected override void OnInitialized()
                {
                    Window.Title = "Hello Nazor";
                    Window.Width = 480;
                    Window.Height = 320;
                }
            }
            """),
        ("App.nazor.nss", "nss", """
            /* Scoped to the elements declared by App.nazor */
            .page {
                display: flex;
                flex-direction: column;
                gap: 12px;
                padding: 24px;
            }

            .heading {
                font-size: 24px;
                font-weight: bold;
            }
            """),
        ("App.nazor.cs", "csharp", """
            namespace HelloNazor;

            public partial class App
            {
                private string name = "";
                private int count;

                private string Name => string.IsNullOrWhiteSpace(name) ? "Nazor" : name;

                private void Increment() => count++;
            }
            """),
    ];

    /// <summary>The first component from the getting-started chapter, with its styles inline.</summary>
    public const string FirstComponent = """
        @window
        <Container Class="page">
            <TextBlock Class="heading">Hello, @Name</TextBlock>
            <TextField @bind-Value="name" AccessibleName="Your name" />
            <Button OnClick="Increment">Count: @count</Button>
        </Container>

        @code {
            protected override void OnInitialized()
            {
                Window.Title = "Hello Nazor";
                Window.Width = 480;
                Window.Height = 320;
            }
        }

        @style {
            .page { display: flex; flex-direction: column; gap: 12px; padding: 24px; }
            .heading { font-size: 24px; font-weight: bold; }
        }
        """;
}
