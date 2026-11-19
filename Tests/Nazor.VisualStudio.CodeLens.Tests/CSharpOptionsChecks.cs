using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Remoting.Proxies;

internal static class CSharpOptionsChecks
{
    internal static void Check(Assembly extension, string ide)
    {
        var directory = Path.Combine(ide, "CommonExtensions", "Microsoft", "VBCSharp", "LanguageServices");
        var workspaces = Assembly.LoadFrom(Path.Combine(directory, "Microsoft.CodeAnalysis.Workspaces.dll"));
        var csharp = Assembly.LoadFrom(Path.Combine(directory, "Microsoft.CodeAnalysis.CSharp.Features.dll"));
        var enumerator = Activator.CreateInstance(csharp.GetType("Microsoft.CodeAnalysis.CSharp.Options.CSharpEditorConfigOptionsEnumerator", true), true);
        var groups = (IEnumerable)enumerator.GetType().GetMethod("GetOptions").Invoke(enumerator, new object[] { false });
        var contract = workspaces.GetType("Microsoft.CodeAnalysis.Options.IGlobalOptionService", true);
        var proxy = new Options(contract, workspaces);
        var read = extension.GetType("Nazor.VisualStudio.NazorCSharpOptions", true).GetMethod("Read", BindingFlags.NonPublic | BindingFlags.Static);
        var values = (IReadOnlyDictionary<string, string>)read.Invoke(null, new[] { new Groups(groups), proxy.GetTransparentProxy() });
        if (values.Count < 100 || values["csharp_style_expression_bodied_methods"] != "when_on_single_line:suggestion" || !values.ContainsKey("indent_size") ||
            !values.Keys.Any(k => k.StartsWith("dotnet_naming_rule.", StringComparison.Ordinal)) || values["csharp_format_on_return"] != "false" || values["csharp_format_on_typing"] != "true")
            throw new Exception("Native C# options were not serialized with Roslyn's editorconfig serializers");
        if (!proxy.PerLanguage) throw new Exception("The C# preference lookup lost the language");
        var handlerType = contract.GetMethod("AddOptionChangedHandler").GetParameters()[1].ParameterType;
        var calls = 0;
        var handler = (Delegate)read.DeclaringType.GetMethod("CreateChangedHandler", BindingFlags.NonPublic | BindingFlags.Static)
            .Invoke(null, new object[] { handlerType, new Action(() => calls++) });
        var arguments = Array.ConvertAll(handlerType.GetMethod("Invoke").GetParameters(), p => p.ParameterType.IsValueType ? Activator.CreateInstance(p.ParameterType) : null);
        handler.DynamicInvoke(arguments);
        if (calls != 1) throw new Exception("Native C# preference changes did not trigger the synchronization callback");
        Console.WriteLine("PASS: native Roslyn enumerates and serializes " + values.Count + " C# preferences, including expression bodies, severity and indentation.");
    }

    private sealed class Groups
    {
        private readonly IEnumerable groups;
        internal Groups(IEnumerable groups) => this.groups = groups;
        public IEnumerable GetOptions(string language, bool unsupported) => groups;
    }

    private sealed class Options : RealProxy
    {
        private readonly Assembly workspaces;
        internal bool PerLanguage;
        internal Options(Type contract, Assembly workspaces) : base(contract) => this.workspaces = workspaces;
        public override IMessage Invoke(IMessage message)
        {
            var call = (IMethodCallMessage)message;
            if (call.MethodName == "GetType") return new ReturnMessage(GetProxiedType(), null, 0, call.LogicalCallContext, call);
            var key = call.Args[0];
            var option = key.GetType().GetProperty("Option").GetValue(key);
            var definition = workspaces.GetType("Microsoft.CodeAnalysis.Options.IOption2", true).GetProperty("Definition").GetValue(option);
            var type = workspaces.GetType("Microsoft.CodeAnalysis.Options.OptionDefinition", true);
            object value = type.GetProperty("DefaultValue").GetValue(definition);
            if ((string)type.GetProperty("ConfigName").GetValue(definition) == "dotnet_style_qualification_for_field")
                PerLanguage = (string)key.GetType().GetProperty("Language").GetValue(key) == "C#";
            if ((string)type.GetProperty("ConfigName").GetValue(definition) == "csharp_style_expression_bodied_methods")
            {
                var serializer = type.GetProperty("Serializer").GetValue(definition);
                var args = new[] { "when_on_single_line:suggestion", (object)null };
                workspaces.GetType("Microsoft.CodeAnalysis.Options.IEditorConfigValueSerializer", true).GetMethod("TryParse").Invoke(serializer, args);
                value = args[1];
            }
            if ((string)type.GetProperty("ConfigName").GetValue(definition) == "csharp_format_on_return") value = false;
            return call.MethodName == "TryGetOption"
                ? new ReturnMessage(true, new[] { key, value }, 2, call.LogicalCallContext, call)
                : new ReturnMessage(value, null, 0, call.LogicalCallContext, call);
        }
    }
}
