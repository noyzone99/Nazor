using System;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.ProjectSystem;

namespace Nazor.VisualStudio
{
    [Export(typeof(IProjectTreePropertiesProvider))]
    [AppliesTo("Nazor & CSharp")]
    [Order(1000)]
    internal sealed class NazorProjectIcons : IProjectTreePropertiesProvider
    {
        public void CalculatePropertyValues(IProjectTreeCustomizablePropertyContext propertyContext, IProjectTreeCustomizablePropertyValues propertyValues)
        {
            if (!propertyValues.Flags.Contains(ProjectTreeFlags.Common.ProjectRoot)) return;
            var icon = new ProjectImageMoniker(new Guid("df813fe5-43b7-40ab-9ba5-4ac6892fdbb6"), 1);
            propertyValues.Icon = icon;
            propertyValues.ExpandedIcon = icon;
        }
    }
}
