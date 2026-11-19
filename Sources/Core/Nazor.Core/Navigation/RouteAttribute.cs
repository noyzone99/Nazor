namespace Nazor;

/// <summary>
/// The address of a page, written in markup with <c>@page "/orders/{Id:int}"</c>: literal segments and parameters in
/// braces, with an optional constraint (<c>int</c>, <c>long</c>, <c>bool</c>, <c>guid</c>, <c>decimal</c>, <c>double</c>,
/// <c>datetime</c>) and a final <c>?</c> for an optional one. A page can have several.
/// </summary>
/// <param name="template">The template (string).</param>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public sealed class RouteAttribute(string template) : Attribute
{
    /// <summary>Gets the route template used to match a page address.</summary>
    public string Template { get; } = template ?? throw new ArgumentNullException(nameof(template));
}
