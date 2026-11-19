# Counter: from state to pixels

```powershell
dotnet run --project Samples/Nazor.Samples.Counter
dotnet run --project Samples/Nazor.Samples.Counter -- --headless
dotnet run --project Samples/Nazor.Samples.Counter -- --headless --png artifacts/counter
```

Read `Counter.nazor` first. A field stores state, the markup displays it, and the button's event callback increments it.
`ComponentBase` requests a render after handling the event, so the example does not manually repaint or call `StateHasChanged`.
The inline `@style` block compiles into a stylesheet scoped to the component's declared markup.

`Program.cs` selects neutral builder startup or the headless path. For native startup, the generated executable bootstrap supplies the Windows host to `NazorApplication.CreateBuilder(args)`. English culture keeps visible text and framework formatting consistent.
In a real application, omit the explicit culture to follow the user's system settings.

`Headless.cs` makes the usually hidden pipeline explicit:

1. A dispatcher owns UI work, and a renderer creates the retained component and element trees.
2. A scene resolves styles and computes layout using the same fonts used for drawing.
3. An event is dispatched directly to the button, and the dispatcher processes asynchronous continuations.
4. Skia rasterizes the display list. Optional PNG output demonstrates physical scales of 1, 1.5, and 2.

Direct event dispatch exercises the callback; it does not test pointer hit testing or a native window.
The headless process returns a nonzero exit code if the text is not `Count: 1` after the click.

Try adding a Reset button with a second handler before moving on to [Tasks](../Nazor.Samples.Tasks/README.md).
