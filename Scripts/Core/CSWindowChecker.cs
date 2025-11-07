using System;
using System.Runtime.InteropServices;
using CommonSDK.Event;

// ReSharper disable InconsistentNaming

namespace ImLag.GUI.Scripts.Core;

public class WindowCheckerErrorOccurredEvent(string msg) : EventBase
{
    public string Message { get; set; } = msg;
}
public static class CSWindowChecker
{
    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern int GetWindowText(IntPtr hWnd, System.Text.StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out int lpdwProcessId);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, int dwProcessId);

    [DllImport("psapi.dll", SetLastError = true)]
    private static extern unsafe uint GetModuleFileNameExW(IntPtr hProcess, IntPtr hModule, char* lpFilename, uint nSize);
    
    [DllImport("kernel32.dll")]
    private static extern bool CloseHandle(IntPtr hObject);
    
    private const uint PROCESS_QUERY_INFORMATION = 0x0400;
    private const uint PROCESS_VM_READ = 0x0010;
    
    public static unsafe bool IsCS2Active()
    {
        try
        {
            var foregroundWindow = GetForegroundWindow();
            if (foregroundWindow == IntPtr.Zero) return false;

            if (GetWindowThreadProcessId(foregroundWindow, out int pid) == 0) return false;

            var hProcess = OpenProcess(PROCESS_QUERY_INFORMATION | PROCESS_VM_READ, false, pid);
            if (hProcess == IntPtr.Zero) return false;

            try
            {
                var processPath = stackalloc char[32767];
                if (GetModuleFileNameExW(hProcess, IntPtr.Zero, processPath, 32767) == 0)
                    return false;

                var processName = System.IO.Path.GetFileName(new string(processPath));
                return processName.Equals("cs2.exe", StringComparison.InvariantCultureIgnoreCase);
            }
            finally
            {
                CloseHandle(hProcess);
            }
        }
        catch (Exception ex)
        {
            EventBus.TriggerEvent(new KeySimulatorErrorOccurredEvent($"检查CS2窗口状态失败: {ex.Message}"));
            return false;
        }
    }
}