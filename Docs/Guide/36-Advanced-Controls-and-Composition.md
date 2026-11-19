# 36. Control composition

The SDK supplies `Nazor.Controls`. Use controls in that namespace for common application tasks and import `Nazor.Controls.Primitives` when you need to compose an interaction yourself.

`PropertyGrid` accepts explicit `Items` for trimming and Native AOT. Each item supplies its value type, getter and optional setter; enum values are offered automatically, and `ChoiceText` can provide labels without reflecting over enum attributes. `Target` and `PropertyGridItem.FromObject` inspect public properties and attribute labels and therefore require preserved metadata, reported through `RequiresUnreferencedCode`.

## Choose a control

| Task | Controls and values |
| --- | --- |
| Edit text | Bind `TextField.Value` for one line, `TextArea.Value` for several lines, or `PasswordField.Value` for masked input |
| Edit a number or date | Bind `NumberField.Value` to a nullable `double` or `DatePicker.Value` to a nullable `DateOnly` |
| Choose an item | Use `Select` for fixed choices and `ComboBox` for editable text with suggestions |
| Group related settings | Use `GroupBox` with a `Header` or `HeaderContent`, and `Field` to associate an editor with its label and help text |
| Switch between views | Use `Tabs` for named sections and `SplitView` for a collapsible side pane |
| Display progress or feedback | Use `ProgressBar` for progress, `Spinner` for waiting, and `InlineMessage` for feedback near the affected content |

See the [control catalog](09-Control-Catalog.md) and the [controls reference](../ApiReferences/Nazor.Controls.md) for API details.

## Compose content

Use each control's content slots to supply markup while the control owns interaction and accessibility. Give each `Tab` a `Label` and place its view inside it. Bind `SelectedKey` to track the current section in application state.

```razor
<Tabs @bind-SelectedKey="section">
    <Tab Key="overview" Label="Overview"><TextBlock>Project overview</TextBlock></Tab>
    <Tab Key="details" Label="Details"><TextBlock>Project details</TextBlock></Tab>
</Tabs>

@code { private string? section = "overview"; }
```

Use Tab to reach the strip and the arrow keys to move between tabs. `Tabs.Placement` chooses the strip edge. Use a `Frame` when navigation also needs addresses or history. See the [tab example](../../Samples/Nazor.Samples.Gallery/Examples/Navigation/TabsSample.nazor), [forms and fields](../../Samples/Nazor.Samples.Gallery/Examples/Fields/FormLayoutSample.nazor), and [disclosure controls](../../Samples/Nazor.Samples.Gallery/Examples/Layout/DisclosureSample.nazor).

Use `ContentTransition` for a keyed view replacement, `LayoutTransition` for a stable element whose measured box changes, and `ItemsTransition<TItem>` for collection add/remove/reorder with stable item keys. Their lifetimes, focus behavior, connected geometry and theme contracts are described in [content and layout motion](23-Transitions-and-Animations.md#content-and-layout-motion).

## Style controls and build custom interactions

Basic supplies default styles, including focus cues. Use [theme tokens](16-Styling-and-Themes.md#design-tokens) for shared appearance. To target an individual control part, use its documented retained element name, such as `TextFieldInput` or `SelectOption`, and follow the [selector scope rules](17-Selectors-and-Media-Queries.md#component-boundaries).

When a ready-to-use control does not fit, compose behavior with primitives. `Pressable` supplies activation, `RovingFocusGroup` coordinates focus among related items, and `PopoverContainer` places and dismisses an overlay. Choose the semantics and labels for your composite control, keep focus visible, and verify keyboard operation. See [semantic primitives](37-Semantic-Primitives.md) and [input and accessibility](24-Input-and-Accessibility.md).

---

[Guide index](README.md) · [API reference](../API.md)
