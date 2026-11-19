using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Language.CodeLens;
using Microsoft.VisualStudio.Language.CodeLens.Remoting;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using Newtonsoft.Json.Linq;
using StreamJsonRpc;

namespace Nazor.VisualStudio.CodeLens.Tests;

internal static class NssCodeLensChecks
{
    internal static async Task CheckAsync(Assembly extension)
    {
        var callbackType = extension.GetType("Nazor.VisualStudio.NssReferenceCallbacks", true);
        var payload = JObject.FromObject(new
        {
            Version = 1L, Selector = ".primary:hover", Partial = false, Global = false,
            References = new[] {
                new { FilePath = "Page.nazor", LineNumber = 2, ColumnNumber = 5, ReferenceStart = 5, ReferenceLength = 6, ReferenceLineText = "    <Button Class=\"primary\" />" },
                new { FilePath = "Page.nazor", LineNumber = 3, ColumnNumber = 5, ReferenceStart = 5, ReferenceLength = 7, ReferenceLineText = "    <Input Class=\"primary\" />" },
            },
        });
        var oop = Assembly.Load("Nazor.VisualStudio.CodeLens");
        var providerType = oop.GetType("Nazor.VisualStudio.CodeLens.NssCodeLensProvider", true);
        var provider = (IAsyncCodeLensDataPointProvider)Activator.CreateInstance(providerType, true);
        var callback = new Callback(payload);
        providerType.GetProperty("Callback", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(provider, callback);
        foreach (var type in new[] { callbackType, providerType })
            if (!type.GetCustomAttributes<ContentTypeAttribute>().Select(a => a.ContentTypes).OrderBy(s => s)
                .SequenceEqual(new[] { "Nazor", "NazorStyleSheet" }))
                throw new Exception("Both NSS CodeLens processes must be scoped to both Nazor content types");
        var descriptor = new CodeLensDescriptor { FilePath = "Page.nazor.nss", ElementDescription = "nazor-nss:test" };
        if (!await provider.CanCreateDataPointAsync(descriptor, null, CancellationToken.None) ||
            await provider.CanCreateDataPointAsync(new CodeLensDescriptor { ElementDescription = "C# member" }, null, CancellationToken.None))
            throw new Exception("NSS provider must accept only its remoted descriptor, without relying on a remoted span");
        var point = await provider.CreateDataPointAsync(descriptor, null, CancellationToken.None);
        var invalidated = new TaskCompletionSource<bool>();
        var invalidations = 0;
        point.InvalidatedAsync += (_, _) => { Interlocked.Increment(ref invalidations); invalidated.TrySetResult(true); return Task.CompletedTask; };
        var label = await point.GetDataAsync(null, CancellationToken.None);
        if (label.IntValue != 2 || label.Description != "2 references") throw new Exception("Incorrect NSS reference count");
        payload["References"] = new JArray();
        var details = await point.GetDetailsAsync(null, CancellationToken.None);
        if (callback.Calls != 1 || details.Entries.Count() != label.IntValue ||
            details.Entries.First().Fields.ElementAt(3).Text != "    <Button Class=\"primary\" />")
            throw new Exception("NSS count and navigable detail rows must use one immutable source snapshot");
        payload["Version"] = 2L;
        if (await Task.WhenAny(invalidated.Task, Task.Delay(4000)) != invalidated.Task)
            throw new Exception("An existing NSS CodeLens point must invalidate when its result version changes");
        details = await point.GetDetailsAsync(null, CancellationToken.None);
        if (details.Entries.Count() != 2) throw new Exception("Pending refresh must preserve details for the displayed count");
        label = await point.GetDataAsync(null, CancellationToken.None);
        details = await point.GetDetailsAsync(null, CancellationToken.None);
        if (label.IntValue != 0 || details.Entries.Any()) throw new Exception("The same NSS point must update its count and destinations");
        await Task.Delay(1200);
        if (invalidations != 1) throw new Exception("Unchanged NSS results must not repeatedly invalidate the point");
        ((IDisposable)point).Dispose();
        payload["Partial"] = true; payload["Global"] = true;
        point = await provider.CreateDataPointAsync(descriptor, null, CancellationToken.None);
        label = await point.GetDataAsync(null, CancellationToken.None);
        if (label.Description != "0+ potential references" || !label.TooltipText.Contains("Partial static"))
            throw new Exception("Partial/global NSS results must not claim an exact zero");
        ((IDisposable)point).Dispose();
        Console.WriteLine("PASS: native NSS CodeLens remoting/content types, coherent count/details, version invalidation in place and partial/global labels.");
    }

    internal static void CheckTracking(Assembly extension, ITextBuffer buffer)
    {
        CheckEager(extension, buffer);
        buffer.Insert(0, ".primary:hover { color: red; }\n#save { opacity: 1; }");
        var callbackType = extension.GetType("Nazor.VisualStudio.NssReferenceCallbacks", true);
        var callbacks = Activator.CreateInstance(callbackType, true);
        var taggerType = extension.GetType("Nazor.VisualStudio.NssReferenceTaggerProvider+Tagger", true);
        var tagger = (ITagger<ICodeLensTag>)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(taggerType);
        taggerType.GetField("callbacks", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(tagger, callbacks);
        taggerType.GetField("tags", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(tagger, Array.Empty<TagSpan<ICodeLensTag>>());
        var ruleType = extension.GetType("Nazor.VisualStudio.NssReferenceTaggerProvider+Rule", true);
        var notifications = 0;
        tagger.TagsChanged += (_, _) => notifications++;
        object Rule(string selector, int count) => new
        {
            start = buffer.CurrentSnapshot.GetText().IndexOf(selector, StringComparison.Ordinal), length = selector.Length, selector,
            references = Enumerable.Range(0, count).Select(i => new
            {
                uri = new Uri(System.IO.Path.GetFullPath("Page.nazor")).AbsoluteUri,
                range = new { start = new { line = i, character = 1 }, end = new { line = i, character = 7 } }, preview = "<Button />",
            }).ToArray(),
        };
        void Apply(params object[] rules) => taggerType.GetMethod("Apply", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(tagger,
            new[] { buffer.CurrentSnapshot, (object)"Page.nazor.nss", JArray.FromObject(rules).ToObject(ruleType.MakeArrayType()) });
        ICodeLensTag3[] Tags() => tagger.GetTags(new NormalizedSnapshotSpanCollection(new SnapshotSpan(buffer.CurrentSnapshot, 0, buffer.CurrentSnapshot.Length)))
            .Select(t => (ICodeLensTag3)t.Tag).ToArray();
        object Result(ICodeLensTag3 tag)
        {
            var task = (Task)callbackType.GetMethod("NazorNssReferencesAsync").Invoke(callbacks, new object[] { tag.Descriptor.ElementDescription });
            task.GetAwaiter().GetResult();
            return task.GetType().GetProperty("Result").GetValue(task);
        }
        Apply(Rule(".primary:hover", 2), Rule("#save", 1));
        var initial = Tags();
        var disconnected = 0;
        foreach (var tag in initial) tag.Disconnected += (_, _) => disconnected++;
        var version = JObject.FromObject(Result(initial[0]))["Version"].Value<long>();
        Apply(Rule(".primary:hover", 2), Rule("#save", 1));
        if (JObject.FromObject(Result(initial[0]))["Version"].Value<long>() != version || notifications != 1)
            throw new Exception("Identical NSS responses must preserve versions and layout");
        Apply(Rule(".primary:hover", 0), Rule("#save", 1));
        if (!Tags().SequenceEqual(initial) || disconnected != 0 || notifications != 1 ||
            JObject.FromObject(Result(initial[0]))["Version"].Value<long>() <= version)
            throw new Exception("Reference changes must reuse tags and update results without changing layout");
        buffer.Insert(0, "\n");
        if (!(initial[0].Descriptor is ICodeLensDescriptorContextProvider) ||
            initial[0].DescriptorContextProvider.GetCurrentContextAsync().GetAwaiter().GetResult().ApplicableSpan != new Span(1, 14))
            throw new Exception("Native NSS descriptors must track the current editor span");
        Apply(Rule(".primary:hover", 0), Rule("#save", 1));
        if (!Tags().SequenceEqual(initial) || notifications != 1) throw new Exception("Moving rules must preserve indicator identities");
        buffer.Insert(1 + ".primary:hover".Length, ":");
        Apply(Rule("#save", 1));
        if (!Tags().SequenceEqual(initial) || notifications != 1) throw new Exception("Incomplete selectors must retain their existing indicator while typing");
        buffer.Delete(new Span(0, buffer.CurrentSnapshot.GetText().IndexOf("#save", StringComparison.Ordinal)));
        Apply(Rule("#save", 1));
        if (Tags().Length != 1 || Tags()[0] != initial[1] || disconnected != 1 || Result(initial[0]) != null)
            throw new Exception("Deleting a rule must disconnect only its own indicator and cache");
        buffer.Delete(new Span(0, buffer.CurrentSnapshot.Length));
        Apply();
        if (Tags().Any() || disconnected != 2 || Result(initial[1]) != null) throw new Exception("Deleted NSS rules must release all indicators");
        buffer.Insert(0, ".primary { color: red; }");
        Apply(Rule(".primary", 1));
        var commented = Tags().Single();
        buffer.Insert(0, "/* ");
        buffer.Insert(buffer.CurrentSnapshot.Length, " */");
        Apply();
        if (Tags().Any() || Result(commented) != null) throw new Exception("Commented rules must not leave stale indicators behind");
        Console.WriteLine("PASS: NSS tag identity/layout survives value refresh, shifts and incomplete selectors; deleted rules release their cache.");
    }

    private static void CheckEager(Assembly extension, ITextBuffer buffer)
    {
        buffer.Insert(0, ".top { color: red; }\n" + string.Concat(Enumerable.Repeat("/* spacer */\n", 300)) +
            "@media (min-width: 1px) { .bottom { color: blue; } }\n@keyframes fade { from { opacity: 0; } }");
        var type = extension.GetType("Nazor.VisualStudio.NssReferenceTaggerProvider+Tagger", true);
        var callbacksType = extension.GetType("Nazor.VisualStudio.NssReferenceCallbacks", true);
        var callbacks = Activator.CreateInstance(callbacksType, true);
        var tagger = System.Runtime.Serialization.FormatterServices.GetUninitializedObject(type);
        type.GetField("callbacks", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(tagger, callbacks);
        type.GetField("tags", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(tagger, Array.Empty<TagSpan<ICodeLensTag>>());
        var seed = type.GetMethod("Seed", BindingFlags.Instance | BindingFlags.NonPublic, null, new[] { typeof(ITextSnapshot), typeof(string) }, null);
        seed.Invoke(tagger, new object[] { buffer.CurrentSnapshot, "Page.nazor.nss" });
        var all = (TagSpan<ICodeLensTag>[])type.GetField("tags", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(tagger);
        if (all.Length != 2 || all.Any(t => !((ICodeLensTag3)t.Tag).Properties.DisplayBeforeCreatingDataPoints))
            throw new Exception("Every NSS rule, including offscreen rules, must reserve its CodeLens row before remote data points exist");
        foreach (var tag in all)
        {
            var task = (Task)callbacksType.GetMethod("NazorNssReferencesAsync").Invoke(callbacks, new object[] { ((ICodeLensTag)tag.Tag).Descriptor.ElementDescription });
            task.GetAwaiter().GetResult();
            if (JObject.FromObject(task.GetType().GetProperty("Result").GetValue(task))["References"].Any())
                throw new Exception("Initial reference placeholders must be zero without waiting for the server");
        }
        seed.Invoke(tagger, new object[] { buffer.CurrentSnapshot, "Page.nazor.nss" });
        if (((TagSpan<ICodeLensTag>[])type.GetField("tags", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(tagger)).Length != 2)
            throw new Exception("Seeding must preserve existing indicator identities");
        buffer.Delete(new Span(0, buffer.CurrentSnapshot.Length));
        Console.WriteLine("PASS: all NSS rules reserve CodeLens rows and zero counts before server startup, independent of the viewport.");
    }

    private sealed class Callback : ICodeLensCallbackService
    {
        private readonly JObject payload;
        internal int Calls;
        internal Callback(JObject payload) => this.payload = payload;
        public JsonRpc GetCallbackJsonRpc(IAsyncCodeLensDataPointProvider provider) => null;
        public JsonRpc GetCallbackJsonRpc(IAsyncCodeLensDataPoint point) => null;
        public Task InvokeAsync(IAsyncCodeLensDataPointProvider provider, string method, IReadOnlyList<object> args, CancellationToken token) => throw new NotSupportedException();
        public Task InvokeAsync(IAsyncCodeLensDataPoint point, string method, IReadOnlyList<object> args, CancellationToken token) => throw new NotSupportedException();
        public Task<T> InvokeAsync<T>(IAsyncCodeLensDataPoint point, string method, IReadOnlyList<object> args, CancellationToken token) => InvokeAsync<T>((IAsyncCodeLensDataPointProvider)null, method, args, token);
        public Task<T> InvokeAsync<T>(IAsyncCodeLensDataPointProvider provider, string method, IReadOnlyList<object> args, CancellationToken token)
        {
            if (!((string)args[0]).StartsWith("nazor-nss:", StringComparison.Ordinal))
                throw new Exception("NSS references must not call native C#/VB count callbacks");
            if (method == "NazorNssReferencesVersionAsync") return Task.FromResult(payload["Version"].ToObject<T>());
            if (method != "NazorNssReferencesAsync") throw new Exception("Unexpected NSS callback: " + method);
            Calls++;
            return Task.FromResult(payload.ToObject<T>());
        }
    }
}
