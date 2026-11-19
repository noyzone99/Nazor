namespace Nazor.Rendering;

/// <summary>Publishes interaction eligibility for validation styling, independently of application-owned validity and value changes.</summary>
public static class ValidationFeedback
{
    /// <summary>Tests whether an element, its nearest field or its nearest form has enabled validation feedback.</summary>
    /// <param name="element">The control or field to inspect.</param>
    /// <returns>Whether user-valid/user-invalid feedback is eligible.</returns>
    public static bool IsEligible(ElementNode element)
    {
        ArgumentNullException.ThrowIfNull(element);
        if ((element.States & ElementStates.ValidationEligible) != 0) return true;
        var fieldSeen = element.Attributes.GetValueOrDefault("field") is true;
        for (var parent = element.ParentElement; parent is not null; parent = parent.ParentElement)
        {
            if (!fieldSeen && parent.Attributes.GetValueOrDefault("field") is true)
            {
                fieldSeen = true;
                if ((parent.States & ElementStates.ValidationEligible) != 0) return true;
            }
            if (parent.Attributes.GetValueOrDefault("role") is "form") return (parent.States & ElementStates.ValidationEligible) != 0;
        }
        return false;
    }

    /// <summary>Enables feedback for the element and its nearest enclosing field. Ordinary parameter updates do not call this method.</summary>
    /// <param name="element">The interacting control.</param>
    public static void MarkInteracted(ElementNode element)
    {
        ArgumentNullException.ThrowIfNull(element);
        if (element.IsRemoved || element.IsDisabled) return;
        element.SetState(ElementStates.ValidationEligible, true);
        for (var parent = element.ParentElement; parent is not null; parent = parent.ParentElement)
        {
            if (parent.Attributes.GetValueOrDefault("role") is "form") break;
            if (parent.Attributes.GetValueOrDefault("field") is not true) continue;
            parent.SetState(ElementStates.ValidationEligible, true);
            break;
        }
    }

    /// <summary>Enables feedback for a form's descendants before the application's submission callback runs.</summary>
    /// <param name="form">The retained form root.</param>
    /// <exception cref="ArgumentException">The element does not publish role=form.</exception>
    public static void Submit(ElementNode form)
    {
        ArgumentNullException.ThrowIfNull(form);
        if (form.Attributes.GetValueOrDefault("role") is not "form") throw new ArgumentException("Expected a form root.", nameof(form));
        form.SetState(ElementStates.ValidationEligible, true);
    }

    /// <summary>Clears feedback eligibility on a root and its descendants, excluding nested forms. Application values and validity remain application-owned.</summary>
    /// <param name="root">The form, field or control to reset.</param>
    public static void Reset(ElementNode root)
    {
        ArgumentNullException.ThrowIfNull(root);
        var pending = new Stack<ElementNode>();
        pending.Push(root);
        while (pending.TryPop(out var element))
        {
            element.SetState(ElementStates.ValidationEligible, false);
            foreach (var child in element.Children)
                if (child is ElementNode node && node.Attributes.GetValueOrDefault("role") is not "form") pending.Push(node);
        }
    }
}
