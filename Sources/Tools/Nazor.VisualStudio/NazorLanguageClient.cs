using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.LanguageServer.Client;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Utilities;
using StreamJsonRpc;

namespace Nazor.VisualStudio
{
    internal static class NazorContentTypes
    {
        public const string Component = "Nazor";
        public const string StyleSheet = "NazorStyleSheet";

#pragma warning disable CS0649
        [Export]
        [Name(Component)]
        [BaseDefinition(CodeRemoteContentDefinition.CodeRemoteContentTypeName)]
        internal static ContentTypeDefinition ComponentDefinition;

        [Export]
        [FileExtension(".nazor")]
        [ContentType(Component)]
        internal static FileExtensionToContentTypeDefinition ComponentExtension;

        [Export]
        [Name(StyleSheet)]
        [BaseDefinition(CodeRemoteContentDefinition.CodeRemoteContentTypeName)]
        internal static ContentTypeDefinition StyleSheetDefinition;

        [Export]
        [FileExtension(".nss")]
        [ContentType(StyleSheet)]
        internal static FileExtensionToContentTypeDefinition StyleSheetExtension;
#pragma warning restore CS0649
    }

    [Export(typeof(ILanguageClient))]
    [Export(typeof(NazorLanguageClient))]
    [ContentType(NazorContentTypes.Component)]
    [ContentType(NazorContentTypes.StyleSheet)]
    internal sealed class NazorLanguageClient : ILanguageClient, ILanguageClientCustomMessage2
    {
        [Import(AllowDefault = true)]
        private Lazy<IGeneratedCodeWatcher> Watcher { get; set; }

        [Import]
        private Lazy<NazorSidecars> Sidecars { get; set; }

        public string Name => "Nazor";

        public JsonRpc Rpc { get; private set; }
        public bool IsReady { get; private set; }

        public event Action Initialized;

        private NazorCompletion middleLayer;
        private readonly NazorCompletionImages completionImages = new NazorCompletionImages();
        private NazorCSharpOptions csharpOptions;
        public object MiddleLayer => middleLayer ?? (middleLayer = new NazorCompletion(() => Watcher?.Value, arguments => Sidecars.Value.DeleteEmptyAsync(arguments)));

        private readonly NazorParameterUpdates parameterUpdates = new NazorParameterUpdates();
        public object CustomMessageTarget => parameterUpdates;
        public event Action ParametersChanged { add => parameterUpdates.Changed += value; remove => parameterUpdates.Changed -= value; }

        public Task AttachForCustomMessageAsync(JsonRpc rpc)
        {
            Rpc = rpc;
            return Task.CompletedTask;
        }

        public IEnumerable<string> ConfigurationSections => null;

        public object InitializationOptions => new { nativeEditing = true };

        public IEnumerable<string> FilesToWatch => new[] { "**/.editorconfig", "**/*.globalconfig", "**/nazorc.rsp", "**/nazorc.roslyn.rsp", "**/nazor.stamp", "**/*.dll", "**/*.nazor", "**/*.nss" };

        public bool ShowNotificationOnInitializeFailed => true;

        public event AsyncEventHandler<EventArgs> StartAsync;

#pragma warning disable CS0067
        public event AsyncEventHandler<EventArgs> StopAsync;
#pragma warning restore CS0067

        public Task<Connection> ActivateAsync(CancellationToken token)
        {
            var folder = Path.GetDirectoryName(typeof(NazorLanguageClient).Assembly.Location);
            var server = Path.Combine(folder, "Server", "Nazor.LanguageServer.dll");
            if (!File.Exists(server)) return Task.FromResult<Connection>(null);
            var process = Process.Start(new ProcessStartInfo(DotnetHost(), "\"" + server + "\"")
            {
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = folder,
            });
            return Task.FromResult(new Connection(process.StandardOutput.BaseStream, process.StandardInput.BaseStream));
        }

        public async Task OnLoadedAsync()
        {
            if (StartAsync != null) await StartAsync.InvokeAsync(this, EventArgs.Empty);
        }

        public async Task OnServerInitializedAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            csharpOptions = csharpOptions ?? new NazorCSharpOptions(options =>
            {
                NazorCSharpOptions.Current = options;
                return Rpc.NotifyWithParameterObjectAsync("nazor/csharpOptions", new { options });
            });
            await csharpOptions.StartAsync();
            var service = Package.GetGlobalService(typeof(SVsImageService)) as IVsImageService2;
            service?.GetImageMonikerType(new ImageMoniker { Guid = NazorCompletionImages.CssCatalog, Id = 255 });
            if (ImageLibrary.Default is ImageLibrary library)
            {
                var manifest = Path.Combine(Path.GetDirectoryName(typeof(NazorLanguageClient).Assembly.Location), "NazorCompletionColors.imagemanifest");
                var images = completionImages.CreateMap(library, manifest);
                await Rpc.NotifyWithParameterObjectAsync("nazor/completionImages", new { images });
            }
            IsReady = true;
            Watcher?.Value.Start();
            Initialized?.Invoke();
        }

        public Task<InitializationFailureContext> OnServerInitializeFailedAsync(ILanguageClientInitializationInfo initializationState) =>
            Task.FromResult(new InitializationFailureContext
            {
                FailureMessage = "The Nazor language client could not be initialized (" + initializationState.StatusMessage +
                                 "). " + initializationState.InitializationException?.GetBaseException().Message,
            });

        private static string DotnetHost()
        {
            foreach (var root in new[] { Environment.GetEnvironmentVariable("DOTNET_ROOT"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "dotnet") })
            {
                if (string.IsNullOrEmpty(root)) continue;
                var host = Path.Combine(root, "dotnet.exe");
                if (File.Exists(host)) return host;
            }
            return "dotnet";
        }
    }
}
