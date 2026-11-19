# Semantic controls and composition primitives

Use `Heading`, `List`, `DefinitionList` and `Table` for document structure. A document list keeps ordinary focus behavior for its children. Use `ListView` or `TreeView` when the collection owns keyboard navigation and selection.

`Field` associates an editor with its label, description and validation error. `GroupBox` names a set of related controls and can disable or collapse it. `Form` handles submission and reset requests. Application state owns validation and model updates.

```nazor
<Form OnSubmit="ValidateName" OnReset="Reset">
    <Field LabelText="Display name" Invalid="@(error is not null)" Required="true">
        <Description>Name shown to teammates.</Description>
        <Error>@error</Error>
        <ChildContent><TextField @bind-Value="displayName" /></ChildContent>
    </Field>
    <ActionBar>
        <Button Type="ButtonType.Reset">Clear</Button>
        <Button Type="ButtonType.Submit">Save</Button>
    </ActionBar>
</Form>

@code {
    private string displayName = "";
    private string? error;

    private void ValidateName() => error = string.IsNullOrWhiteSpace(displayName)
        ? "Enter a display name."
        : null;

    private void Reset()
    {
        displayName = "";
        error = null;
    }
}
```

Submit an empty name to see the error associated with the editor. `Required` exposes required state, while `ValidateName` determines whether the value is valid. See the [field example](../../Samples/Nazor.Samples.Gallery/Examples/Fields/FieldSample.nazor), [form layout example](../../Samples/Nazor.Samples.Gallery/Examples/Fields/FormLayoutSample.nazor), and [accessible names and semantics](24-Input-and-Accessibility.md#accessible-names-and-semantics).

Import `Nazor.Controls.Primitives` to compose custom controls. `Pressable` supplies activation, `RovingFocusGroup` coordinates keyboard focus, `Collapsible` retains the content of a disclosure, and `PopoverContainer` and `DialogContainer` provide overlay behavior. `Virtualizer` limits which rows are rendered. `Draggable` reports pointer gestures. `ResizeHandle` supports pointer dragging and keyboard resizing.

```nazor
@using Nazor.Controls.Primitives

<Pressable OnPress="() => count++">Increase</Pressable>
<TextBlock>@count</TextBlock>
@code { private int count; }
```

An ordinary `Button` already supplies activation and default presentation. Use `Pressable` when building the behavior and appearance of a custom control. Preserve focus cues and a meaningful label when styling it. See the [primitive examples](../../Samples/Nazor.Samples.Gallery/Examples/Primitives) for focus, selection, measurement, accessibility and custom overlays. The [catalog](09-Control-Catalog.md#reusable-primitives) links each primitive to its API contract.

## Rectangular color composition

Use the ready [ColorPicker](10-Text-Input-and-Data-Controls.md#choose-a-color) for the complete surface, sliders, channel text, hex and preview. `ColorArea` remains a bare primitive. Its default SaturationValue behavior and original static MapPoint overload are unchanged. The new `Components` parameter and HSV MapPoint overload select six axis permutations while retaining the unedited channel and alpha. Themes consume `--nz-color-area-background` for the complete layered gradients; the existing hue/color/x/y variables remain available. Hue/saturation planes show full brightness while preserving the stored value, matching the default picker interaction.

| Components | Horizontal growth | Vertical growth | Remaining slider |
| --- | --- | --- | --- |
| SaturationValue | Saturation right | Value up | Hue |
| HueSaturation | Hue right | Saturation up | Value |
| HueValue | Hue right | Value up | Saturation |
| SaturationHue | Saturation left | Hue down | Value |
| ValueHue | Value left | Hue down | Saturation |
| ValueSaturation | Value left | Saturation down | Hue |

The slider range describes the horizontal channel; value-text names both channels and their units. Keep that description when replacing the primitive's visuals. A lost capture cancels completion while retaining the last preview. Existing custom SaturationValue styles keep working; when exposing the new planes, replace a hardcoded SV gradient with `background: var(--nz-color-area-background)`.
