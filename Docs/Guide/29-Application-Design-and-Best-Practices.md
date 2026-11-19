# 29. Application design and best practices

Apply these practices to the [first desktop application](38-First-Desktop-Application.md). Its editor owns transient input, its window-scoped session retains the saved name across navigation, and the sidecar styles stay with the component that declares the markup.

## Start with responsibilities

Keep `Program.cs` as the composition root: choose the platform host, configure options, register services, and run the shell. Let the shell own navigation/window composition. A view owns its transient interaction state. Models represent application data; services perform persistence, domain operations, or shared coordination.

The [Tasks sample](../../Samples/Nazor.Samples.Tasks/README.md) provides a practical structure: Components for reusable visual pieces, Views for screens, Models for data, Services for the store, and local NSS sidecars for layout. Its in-memory store is intentionally replaceable; a real persistence implementation can use the same interface without introducing database work into render code.

Do not create every layer in advance. A one-screen utility may need only a root component and a service. Extract a component when it has a coherent reusable contract or independent interaction responsibility.

## Keep state at the right lifetime

Use local fields for temporary UI state, parameters for parent-owned values, a scoped service for window-specific sessions, and singletons for deliberately shared app state. Navigation and conditional rendering destroy components; hiding a window preserves them, closing it destroys them. Design persistence independently of all three.

Use a draft model for cancelable editing. Avoid mutating a parameter object invisibly and assuming every consumer will rerender. Prefer a callback or explicit notification boundary. Cancel stale asynchronous work and release event/shortcut subscriptions with their component.

## Make component contracts explicit

Expose typed parameters and callbacks, name binding pairs consistently (`Value`/`ValueChanged`), and mark required inputs with EditorRequired. A required attribute is not domain validation. Expose a style token or a forwarding Class parameter when callers need supported customization.

Use stable keys on repeated outer rows. Supply a typed lambda when generic callback inference needs help. Keep sidecar names and namespaces synchronized. Use captured dictionaries for extra element metadata instead of spelling undeclared component parameters.

## Keep styles maintainable

Use component sheets for local structure and global sheets for theme/application tokens. Remember cascade levels before increasing selector specificity. Prefer a custom property to duplicating a theme. Use `::deep` intentionally and document dependence on child markup if it becomes a supported contract.

Use flex/grid for ordinary composition. Give scrollable regions a bounded viewport and shrinking ancestors `min-height: 0`/`min-width: 0` as appropriate. Reserve inline style for data-driven values and format those values invariantly. Use transforms for visual movement when layout should remain fixed.

## Build usable interactions

Use controls with the right semantics rather than clickable anonymous containers when possible. Label fields and icon-only commands. Preserve visible keyboard focus, source reading order, predictable popup dismissal, and an exit path from fullscreen. Honor reduced motion and forced colors. Use live regions for visible status and announcements for offscreen messages without duplicating speech.

Treat backend capabilities as results to inspect. Check effective window operations; a request is not proof that an OS feature exists. Tray apps need a usable exit command and recovery when the tray is unavailable. Notification acceptance does not guarantee delivery.

## Review before shipping

| Area | Practical check |
| --- | --- |
| Build | Clean restore/build from documented inputs; package versions aligned |
| State | Navigation, window closure/reopening, async cancellation, and disposal behave deliberately |
| Layout | Narrow/wide windows, long text, scrolling, and multiple DPI scales |
| Input | Keyboard-only use, default actions, focus restoration, and text editing |
| Accessibility | Names, descriptions, live status, high contrast, real screen-reader interaction |
| Localization | Resource fallbacks, long translations, dates/numbers, native-vs-app language |
| Assets | Paths work outside the source tree; fonts/images/licenses are included |
| Deployment | Run the actual publish directory on the intended OS, display backend, architecture and runtime configuration |

Use [testing and diagnostics](27-Testing-and-Diagnostics.md) to investigate failures and [support limitations](32-Support-and-Limitations.md) to make unsupported assumptions visible in the application design.

---

[Guide index](README.md) · [API reference](../API.md)
