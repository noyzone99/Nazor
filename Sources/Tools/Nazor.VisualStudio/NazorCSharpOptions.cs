using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Threading;

namespace Nazor.VisualStudio
{
    internal sealed class NazorCSharpOptions
    {
        internal static IReadOnlyDictionary<string, string> Current = new Dictionary<string, string>();
        private readonly Func<IReadOnlyDictionary<string, string>, Task> publish;
        private object enumerator;
        private object service;
        private int pending;

        internal NazorCSharpOptions(Func<IReadOnlyDictionary<string, string>, Task> publish) => this.publish = publish;

        internal static bool BraceCompletionEnabled(IServiceProvider services, bool fallback)
        {
            if (!(services?.GetService(typeof(SVsTextManager)) is IVsTextManager4 manager)) return fallback;
            var preferences = new[] { new LANGPREFERENCES3 { guidLang = new Guid("694dd9b6-b865-4c5b-ad85-86356e9c88dc") } };
            // Nazor views inherit PlainText's disabled switch; C# regions use the C# language preference.
            return ErrorHandler.Succeeded(manager.GetUserPreferences4(null, preferences, null)) ? preferences[0].fBraceCompletion != 0 : fallback;
        }

        internal async Task StartAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            try
            {
                if (service != null) { await publish(Read(enumerator, service)); return; }
                var componentModel = (IComponentModel)Package.GetGlobalService(typeof(SComponentModel));
                var workspaces = NazorRoslynAssemblies.Load("Microsoft.CodeAnalysis.Workspaces");
                var features = NazorRoslynAssemblies.Load("Microsoft.CodeAnalysis.Features");
                object Export(Type type) => typeof(IComponentModel).GetMethod(nameof(IComponentModel.GetService)).MakeGenericMethod(type).Invoke(componentModel, null);
                enumerator = Export(features.GetType("Microsoft.CodeAnalysis.Options.EditorConfigOptionsEnumerator", true));
                var contract = workspaces.GetType("Microsoft.CodeAnalysis.Options.IGlobalOptionService", true);
                var optionsService = Export(contract);
                var subscribe = contract.GetMethod("AddOptionChangedHandler");
                var handlerType = subscribe.GetParameters()[1].ParameterType;
                var handler = CreateChangedHandler(handlerType, Changed);
                subscribe.Invoke(optionsService, new object[] { this, handler });
                service = optionsService;
                await publish(Read(enumerator, service));
            }
            catch (Exception exception) when (!(exception is OutOfMemoryException))
            {
                ActivityLog.LogWarning("Nazor", "C# editor preferences could not be synchronized: " + exception.GetBaseException().Message);
            }
        }

        internal static Delegate CreateChangedHandler(Type type, Action changed) =>
            Expression.Lambda(type, Expression.Invoke(Expression.Constant(changed)),
                type.GetMethod("Invoke").GetParameters().Select(p => Expression.Parameter(p.ParameterType))).Compile();

        private void Changed()
        {
            if (Interlocked.Exchange(ref pending, 1) != 0) return;
            ThreadHelper.JoinableTaskFactory.StartOnIdle(async () =>
            {
                await Task.Delay(250);
                Interlocked.Exchange(ref pending, 0);
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                try { await publish(Read(enumerator, service)); }
                catch (Exception exception) when (!(exception is OutOfMemoryException))
                {
                    ActivityLog.LogWarning("Nazor", "C# editor preferences could not be synchronized: " + exception.GetBaseException().Message);
                }
            }).FileAndForget("nazor/csharpOptions");
        }

        internal static IReadOnlyDictionary<string, string> Read(object enumerator, object service)
        {
            var features = NazorRoslynAssemblies.Load("Microsoft.CodeAnalysis.Features");
            var groups = enumerator.GetType().GetMethod("GetOptions").Invoke(enumerator, new object[] { "C#", false });
            var config = (string)features.GetType("Microsoft.CodeAnalysis.Options.EditorConfigFileGenerator", true)
                .GetMethod("Generate").Invoke(null, new[] { groups, service, "C#" });
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            using (var reader = new StringReader(config))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    line = line.Trim();
                    var equals = line.IndexOf('=');
                    if (equals <= 0 || line[0] == '#') continue;
                    var key = line.Substring(0, equals).Trim();
                    if (key != "root") result[key] = line.Substring(equals + 1).Trim();
                }
            }
            var workspaces = NazorRoslynAssemblies.Load("Microsoft.CodeAnalysis.Workspaces");
            var storage = Assembly.LoadFrom(Path.Combine(Path.GetDirectoryName(features.Location), "Microsoft.CodeAnalysis.LanguageServer.Protocol.dll"))
                .GetType("Microsoft.CodeAnalysis.Formatting.AutoFormattingOptionsStorage", true);
            var keyType = workspaces.GetType("Microsoft.CodeAnalysis.Options.OptionKey2", true);
            var get = workspaces.GetType("Microsoft.CodeAnalysis.Options.IGlobalOptionService", true).GetMethods()
                .Single(m => m.Name == "GetOption" && m.IsGenericMethodDefinition && m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType == keyType).MakeGenericMethod(typeof(bool));
            foreach (var name in new[] { "Return", "Typing", "Semicolon", "CloseBrace" })
            {
                var option = storage.GetField("FormatOn" + name, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).GetValue(null);
                var key = Activator.CreateInstance(keyType, option, "C#");
                var configName = name == "CloseBrace" ? "close_brace" : name.ToLowerInvariant();
                result["csharp_format_on_" + configName] = (bool)get.Invoke(service, new[] { key }) ? "true" : "false";
            }
            return result;
        }
    }
}
