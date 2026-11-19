using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.LanguageServer.Client;
using Newtonsoft.Json.Linq;

namespace Nazor.VisualStudio
{
    internal sealed class NazorCompletion : ILanguageClientMiddleLayer2<JToken>
    {
        private readonly Func<IGeneratedCodeWatcher> watcher;
        private readonly Func<JToken, Task> deleteEmptySidecars;

        public NazorCompletion(Func<IGeneratedCodeWatcher> watcher, Func<JToken, Task> deleteEmptySidecars)
        {
            this.watcher = watcher;
            this.deleteEmptySidecars = deleteEmptySidecars;
        }

        public bool CanHandle(string methodName) => methodName == "textDocument/codeAction" || methodName == "workspace/executeCommand";

        public Task HandleNotificationAsync(string methodName, JToken methodParam, Func<JToken, Task> sendNotification) => sendNotification(methodParam);

        public async Task<JToken> HandleRequestAsync(string methodName, JToken methodParam, Func<JToken, Task<JToken>> sendRequest)
        {
            if (methodName == "workspace/executeCommand" && methodParam["command"]?.Value<string>() == "nazor.deleteEmptySidecars")
            {
                await deleteEmptySidecars(methodParam["arguments"]?[0]);
                return JValue.CreateNull();
            }
            if (methodName == "textDocument/codeAction" && methodParam["textDocument"]?["uri"]?.Value<string>() is string uri)
                await (watcher()?.SynchronizeAsync(new Uri(uri).LocalPath) ?? Task.CompletedTask);
            return await sendRequest(methodParam);
        }
    }
}
