using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using EnvDTE;
using Microsoft.VisualStudio.Debugger.Interop;
using Microsoft.VisualStudio.Shell;
using Newtonsoft.Json;

namespace Nazor.VisualStudio
{
    public sealed partial class NazorBreakpointReader
    {
        private const enum_DEBUGPROP_INFO_FLAGS RawFlags = enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_NAME | enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_TYPE
            | enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_PROP | enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_VALUE | enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_ATTRIB
            | enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_NOFUNCEVAL | enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_VALUE_RAW | enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_VALUE_NO_TOSTRING;
        private const enum_DBG_ATTRIB_FLAGS BadValue = enum_DBG_ATTRIB_FLAGS.DBG_ATTRIB_VALUE_ERROR | enum_DBG_ATTRIB_FLAGS.DBG_ATTRIB_VALUE_SIDE_EFFECT
            | enum_DBG_ATTRIB_FLAGS.DBG_ATTRIB_VALUE_TIMEOUT | enum_DBG_ATTRIB_FLAGS.DBG_ATTRIB_VALUE_INVALID;

        public string GetState(int processId) => ThreadHelper.JoinableTaskFactory.Run(async () =>
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            var attached = Attached(processId);
            var state = !attached ? "exited" : debugger.CurrentMode == dbgDebugMode.dbgBreakMode ? "paused" : "running";
            return JsonConvert.SerializeObject(new { processId, adapter = "Visual Studio AD7", state, stopId = state == "paused" ? Generation.ToString(CultureInfo.InvariantCulture) : null,
                canReadFields = attached, message = (string)null });
        });

        private bool Attached(int processId)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            foreach (EnvDTE.Process candidate in debugger.DebuggedProcesses) if (candidate.ProcessID == processId) return true;
            return false;
        }

        public string ReadTree(int processId, string sessionId, long stop) => ThreadHelper.JoinableTaskFactory.Run(async () =>
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            var timer = Stopwatch.StartNew();
            var window = Window(processId, sessionId, stop, timer);
            var tree = Text(Member(window, "TreeJson", stop, timer));
            Current(stop, timer);
            if (string.IsNullOrEmpty(tree) || tree.Length > 1024 * 1024) throw new InvalidOperationException("No complete materialized tree is available.");
            return tree;
        });

        public string ReadComponent(int processId, string sessionId, long captureGeneration, string componentId, long stop) => ThreadHelper.JoinableTaskFactory.Run(async () =>
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            if (captureGeneration <= 0 || componentId == null || componentId.Length > 128) throw new ArgumentException("Invalid component context.");
            var timer = Stopwatch.StartNew();
            var window = Window(processId, sessionId, stop, timer);
            var entries = Children(Member(window, "Components", stop, timer), 4096, stop, timer);
            var fields = new List<object>();
            foreach (var entry in entries.Where(e => e.bstrName.StartsWith("[", StringComparison.Ordinal)))
            {
                if (Text(Member(entry.pProperty, "Id", stop, timer)) != componentId) continue;
                var instance = Member(entry.pProperty, "Instance", stop, timer);
                if (instance.GetDerivedMostProperty(out var concrete) == 0 && concrete != null) instance = concrete;
                foreach (var field in Children(instance, 128, stop, timer))
                {
                    var available = (field.dwAttrib & BadValue) == 0 && (field.dwFields & enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_VALUE) != 0;
                    fields.Add(new { name = field.bstrName, type = field.bstrType ?? "unknown", value = available ? field.bstrValue ?? "null" : "<unavailable>", available });
                }
                Current(stop, timer);
                return JsonConvert.SerializeObject(new { capture = new { sessionId, generation = captureGeneration, source = "debugger", stopId = stop.ToString(CultureInfo.InvariantCulture),
                    capturedAtUtc = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture) }, componentId, fields,
                    message = "Read-only fields at the current debugger stop. The target may be inside an incomplete render/layout; retained trees and geometry describe their original capture." });
            }
            throw new InvalidOperationException("The component is not in the materialized window registry.");
        });

        private IDebugProperty2 Window(int processId, string sessionId, long stop, Stopwatch timer)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (string.IsNullOrEmpty(sessionId) || sessionId.Length > 128) throw new ArgumentException("Invalid session.");
            Current(stop, timer);
            if (!Attached(processId) || !debuggerPrograms.TryGetValue(processId, out var selectedProgram)) throw new InvalidOperationException("No managed debugger program is available for this process.");
            Marshal.ThrowExceptionForHR(selectedProgram.GetProcess(out var process));
            var ids = new AD_PROCESS_ID[1]; Marshal.ThrowExceptionForHR(process.GetPhysicalProcessId(ids));
            if (ids[0].dwProcessId != processId) throw new InvalidOperationException("The debugger program belongs to another process.");
            Marshal.ThrowExceptionForHR(selectedProgram.EnumThreads(out var threads));
            var thread = new IDebugThread2[1]; uint fetched = 0;
            for (var t = 0; t < 32 && threads.Next(1, thread, ref fetched) == 0 && fetched == 1; t++)
            {
                Marshal.ThrowExceptionForHR(thread[0].EnumFrameInfo(enum_FRAMEINFO_FLAGS.FIF_FRAME, 10, out var frames));
                var frame = new FRAMEINFO[1];
                for (var f = 0; f < 64 && frames.Next(1, frame, ref fetched) == 0 && fetched == 1; f++)
                {
                    Current(stop, timer);
                    if (frame[0].m_pFrame.GetExpressionContext(out var context) != 0 || context == null) continue;
                    if (context.ParseText("Nazor.DevTools.DebuggerRoots.Windows", enum_PARSEFLAGS.PARSE_EXPRESSION, 10, out var expression, out _, out _) != 0 || expression == null) continue;
                    if (expression.EvaluateSync(enum_EVALFLAGS.EVAL_NOSIDEEFFECTS | enum_EVALFLAGS.EVAL_NOFUNCEVAL, 500, null, out var root) != 0 || root == null) continue;
                    foreach (var item in Children(root, 64, stop, timer).Where(e => e.bstrName.StartsWith("[", StringComparison.Ordinal)))
                        if (Text(Member(item.pProperty, "SessionId", stop, timer)) == sessionId) return item.pProperty;
                }
            }
            throw new InvalidOperationException("The requested window has no supported materialized debugger root.");
        }

        private void Current(long stop, Stopwatch timer)
        {
            if (!IsCurrent(stop)) throw new InvalidOperationException("The debugger stop changed.");
            if (timer.ElapsedMilliseconds > 2000) throw new TimeoutException("The bounded debugger read expired.");
        }
        private DEBUG_PROPERTY_INFO[] Children(IDebugProperty2 property, int limit, long stop, Stopwatch timer)
        {
            Current(stop, timer);
            var filter = Guid.Empty;
            Marshal.ThrowExceptionForHR(property.EnumChildren(RawFlags, 10, ref filter, enum_DBG_ATTRIB_FLAGS.DBG_ATTRIB_ALL, null, 500, out var children));
            Marshal.ThrowExceptionForHR(children.GetCount(out var count));
            if (count > limit) throw new InvalidOperationException("The complete field list exceeds its budget.");
            var result = new List<DEBUG_PROPERTY_INFO>(); var item = new DEBUG_PROPERTY_INFO[1]; uint fetched = 0;
            while (children.Next(1, item, out fetched) == 0 && fetched == 1)
            { Current(stop, timer); result.Add(item[0]); }
            return result.ToArray();
        }
        private IDebugProperty2 Member(IDebugProperty2 property, string name, long stop, Stopwatch timer)
        {
            var member = Children(property, 128, stop, timer).FirstOrDefault(c => c.bstrName == name);
            if (member.pProperty == null || (member.dwAttrib & BadValue) != 0) throw new InvalidOperationException("A required materialized field is unavailable: " + name);
            return member.pProperty;
        }
        private static string Text(IDebugProperty2 property)
        {
            if (property is IDebugProperty3 text && text.GetStringCharLength(out var length) == 0 && length <= 1024 * 1024)
            {
                var value = new ushort[length];
                Marshal.ThrowExceptionForHR(text.GetStringChars(length, value, out var fetched));
                if (fetched != length) throw new InvalidOperationException("A debugger string was truncated.");
                return new string(value.Select(c => (char)c).ToArray());
            }
            throw new InvalidOperationException("The debugger cannot read the raw string without evaluation.");
        }
    }
}
