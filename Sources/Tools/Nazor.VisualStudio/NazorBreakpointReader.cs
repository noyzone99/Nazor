using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using EnvDTE;
using EnvDTE100;
using Microsoft.VisualStudio.Debugger.Interop;
using Microsoft.VisualStudio.Shell.Interop;
using Newtonsoft.Json;

namespace Nazor.VisualStudio
{
    [ComVisible(true)]
    [Guid("18BD1E52-CE6C-4E46-AE34-C8A20A4CE441")]
    [InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
    public interface INazorBreakpointSpike
    {
        [DispId(1)] long Generation { get; }
        [DispId(2)] bool IsCurrent(long stop);
        [DispId(3)] string ReadFields(int processId, string framePrefix, string fieldPaths);
        [DispId(4)] string GetState(int processId);
        [DispId(5)] string ReadComponent(int processId, string sessionId, long captureGeneration, string componentId, long stop);
        [DispId(6)] string ReadTree(int processId, string sessionId, long stop);
    }

    [ComVisible(true)]
    [ClassInterface(ClassInterfaceType.None)]
    [ComDefaultInterface(typeof(INazorBreakpointSpike))]
    public sealed partial class NazorBreakpointReader : IDisposable, IDebugEventCallback2, INazorBreakpointSpike
    {
        private readonly Debugger5 debugger;
        private readonly DebuggerEvents events;
        private readonly IVsDebugger shell;
        private volatile IDebugProgram2 program = null!;
        private readonly System.Collections.Concurrent.ConcurrentDictionary<int, IDebugProgram2> debuggerPrograms = new System.Collections.Concurrent.ConcurrentDictionary<int, IDebugProgram2>();
        private long generation;

        internal NazorBreakpointReader(DTE dte, IVsDebugger shell)
        {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();
            debugger = (Debugger5)dte.Debugger;
            this.shell = shell;
            Marshal.ThrowExceptionForHR(shell.AdviseDebugEventCallback(this));
            events = dte.Events.DebuggerEvents;
            events.OnEnterBreakMode += OnBreak;
            events.OnEnterRunMode += OnRun;
            events.OnEnterDesignMode += OnRun;
        }

        public long Generation => Interlocked.Read(ref generation);

        public bool IsCurrent(long stop)
        {
            return Microsoft.VisualStudio.Shell.ThreadHelper.JoinableTaskFactory.Run(async () =>
            {
                await Microsoft.VisualStudio.Shell.ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                return stop == generation && debugger.CurrentMode == dbgDebugMode.dbgBreakMode;
            });
        }

        public string ReadFields(int processId, string framePrefix, string fieldPaths)
        {
            if (fieldPaths == null || fieldPaths.Length > 16384) throw new ArgumentException("The field request is too large.", nameof(fieldPaths));
            return Microsoft.VisualStudio.Shell.ThreadHelper.JoinableTaskFactory.Run(async () =>
            {
                await Microsoft.VisualStudio.Shell.ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                return JsonConvert.SerializeObject(Read(processId, framePrefix, fieldPaths.Split('\n')));
            });
        }

        private Dictionary<string, string> Read(int processId, string framePrefix, params string[] fieldPaths)
        {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();
            if (string.IsNullOrEmpty(framePrefix) || framePrefix.Length > 256) throw new ArgumentException("A supported frame prefix is required.", nameof(framePrefix));
            if (fieldPaths.Length > 32) throw new ArgumentException("At most 32 field paths are supported.", nameof(fieldPaths));
            foreach (var path in fieldPaths)
                if (path == null || path.Length > 512 || !Regex.IsMatch(path, @"\A[A-Za-z_][A-Za-z0-9_]*(\.[A-Za-z_][A-Za-z0-9_]*)+\z"))
                    throw new ArgumentException("Only qualified field paths are supported.", nameof(fieldPaths));
            if (debugger.CurrentMode != dbgDebugMode.dbgBreakMode)
                throw new InvalidOperationException("The target is not paused.");

            var stop = generation;
            IDebugStackFrame2 frame = null!;
            if (program == null) throw new InvalidOperationException("No debugger program is available.");
            Marshal.ThrowExceptionForHR(program.GetProcess(out var process));
            var processIds = new AD_PROCESS_ID[1];
            Marshal.ThrowExceptionForHR(process.GetPhysicalProcessId(processIds));
            if (processIds[0].dwProcessId != processId) throw new InvalidOperationException("The debugger program belongs to another process.");
            Marshal.ThrowExceptionForHR(program.EnumThreads(out var threads));
            var thread = new IDebugThread2[1];
            uint fetched = 0;
            for (var threadCount = 0; threadCount < 32 && threads.Next(1, thread, ref fetched) == 0 && fetched == 1; threadCount++)
            {
                Marshal.ThrowExceptionForHR(thread[0].EnumFrameInfo(enum_FRAMEINFO_FLAGS.FIF_FUNCNAME | enum_FRAMEINFO_FLAGS.FIF_FRAME, 10, out var frames));
                var info = new FRAMEINFO[1];
                for (var frameCount = 0; frameCount < 64 && frames.Next(1, info, ref fetched) == 0 && fetched == 1; frameCount++)
                {
                    if (!(info[0].m_bstrFuncName ?? "").StartsWith(framePrefix, StringComparison.Ordinal)) continue;
                    frame = info[0].m_pFrame;
                    break;
                }
                if (frame != null) break;
            }
            if (frame == null) throw new InvalidOperationException("No supported frame in the specified process is available.");

            var values = new Dictionary<string, string>(StringComparer.Ordinal);
            Marshal.ThrowExceptionForHR(frame.GetExpressionContext(out var context));
            foreach (var path in fieldPaths)
            {
                if (!IsCurrent(stop)) throw new InvalidOperationException("The debugger stop changed during the read.");
                var parsed = context.ParseText(path, enum_PARSEFLAGS.PARSE_EXPRESSION, 10, out var expression, out _, out _);
                if (parsed != 0 || expression == null || expression.EvaluateSync(enum_EVALFLAGS.EVAL_NOSIDEEFFECTS | enum_EVALFLAGS.EVAL_NOFUNCEVAL, 1000, null, out var property) != 0 || property == null)
                {
                    values.Add(path, "<unavailable>");
                    continue;
                }
                var info = new DEBUG_PROPERTY_INFO[1];
                var flags = enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_VALUE | enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_ATTRIB
                    | enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_NOFUNCEVAL | enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_VALUE_RAW
                    | enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_VALUE_NO_TOSTRING;
                var result = property.GetPropertyInfo(flags, 10, 1000, null, 0, info);
                var unavailable = enum_DBG_ATTRIB_FLAGS.DBG_ATTRIB_VALUE_ERROR | enum_DBG_ATTRIB_FLAGS.DBG_ATTRIB_VALUE_SIDE_EFFECT
                    | enum_DBG_ATTRIB_FLAGS.DBG_ATTRIB_VALUE_TIMEOUT | enum_DBG_ATTRIB_FLAGS.DBG_ATTRIB_VALUE_INVALID;
                values.Add(path, result == 0 && (info[0].dwFields & enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_VALUE) != 0
                    && (info[0].dwAttrib & unavailable) == 0 ? info[0].bstrValue : "<unavailable>");
            }
            if (!IsCurrent(stop)) throw new InvalidOperationException("The debugger stop changed during the read.");
            return values;
        }

        private void OnBreak(dbgEventReason reason, ref dbgExecutionAction action) { Interlocked.Increment(ref generation); }
        private void OnRun(dbgEventReason reason) { Interlocked.Increment(ref generation); }

        public int Event(IDebugEngine2 engine, IDebugProcess2 process, IDebugProgram2 eventProgram, IDebugThread2 thread, IDebugEvent2 debugEvent, ref Guid eventId, uint attributes)
        {
            if (eventProgram != null)
            {
                program = eventProgram;
                try
                {
                    if (eventProgram.GetProcess(out var owner) == 0)
                    {
                        var ids = new AD_PROCESS_ID[1];
                        if (owner.GetPhysicalProcessId(ids) == 0 && (debuggerPrograms.Count < 64 || debuggerPrograms.ContainsKey((int)ids[0].dwProcessId)))
                            debuggerPrograms[(int)ids[0].dwProcessId] = eventProgram;
                    }
                }
                catch (COMException) { }
            }
            return 0;
        }

        public void Dispose()
        {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();
            events.OnEnterBreakMode -= OnBreak;
            events.OnEnterRunMode -= OnRun;
            events.OnEnterDesignMode -= OnRun;
            Marshal.ThrowExceptionForHR(shell.UnadviseDebugEventCallback(this));
            program = null!;
            debuggerPrograms.Clear();
        }
    }
}
