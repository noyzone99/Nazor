using System;
using Nazor;

namespace Phase17;

public abstract class BaseCard : ComponentBase
{
    [Parameter] public string Caption { get; set; } = "inherited";
}

[AttributeUsage(AttributeTargets.Class)]
public sealed class DemoTagAttribute : Attribute;
