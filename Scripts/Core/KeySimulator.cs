using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Threading;

// ReSharper disable InconsistentNaming

namespace ImLag.GUI.Scripts.Core
{
    [SuppressMessage("Interoperability", "SYSLIB1054:使用 \"LibraryImportAttribute\" 而不是 \"DllImportAttribute\" 在编译时生成 P/Invoke 封送代码")]
    public static class KeySimulator
    {
        [DllImport("user32.dll", SetLastError = true)]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern short VkKeyScan(char ch);

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

        private const uint KEYEVENTF_KEYUP = 0x0002;
        private const uint PROCESS_QUERY_INFORMATION = 0x0400;
        private const uint PROCESS_VM_READ = 0x0010;
        
        public static void SimulateKeyPress(char key)
        {
            try
            {
                byte virtualKey;
                
                switch (key)
                {
                    case (char)0x0D: // Enter
                        virtualKey = 0x0D;
                        break;
                    case (char)0x11: // Ctrl
                        virtualKey = 0x11;
                        break;
                    case (char)0x41: // A
                        virtualKey = 0x41;
                        break;
                    case (char)0x56: // V
                        virtualKey = 0x56;
                        break;
                    case (char)0x2E: // Delete
                        virtualKey = 0x2E;
                        break;
                    default:
                        // 普通字符用 VkKeyScan
                        var scanResult = VkKeyScan(key);
                        if (scanResult == -1)
                        {
                            // 失败用ASCII
                            virtualKey = (byte)char.ToUpper(key);
                        }
                        else
                        {
                            virtualKey = (byte)(scanResult & 0xFF);
                        }
                        break;
                }
                
                // 按下键
                keybd_event(virtualKey, 0, 0, UIntPtr.Zero);
                Thread.Sleep(50);
                
                // 释放键
                keybd_event(virtualKey, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
                Thread.Sleep(50);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"按键模拟失败: {ex.Message}");
            }
        }
        
        public static void SimulateKeyDown(char key)
        {
            try
            {
                byte virtualKey;
                
                switch (key)
                {
                    case (char)0x11: // Ctrl
                        virtualKey = 0x11;
                        break;
                    default:
                        var scanResult = VkKeyScan(key);
                        virtualKey = scanResult == -1 ? (byte)char.ToUpper(key) : (byte)(scanResult & 0xFF);
                        break;
                }
                
                keybd_event(virtualKey, 0, 0, UIntPtr.Zero);
                Thread.Sleep(20);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"按键按下失败: {ex.Message}");
            }
        }
        
        public static void SimulateKeyUp(char key)
        {
            try
            {
                byte virtualKey;
                
                switch (key)
                {
                    case (char)0x11: // Ctrl
                        virtualKey = 0x11;
                        break;
                    default:
                        var scanResult = VkKeyScan(key);
                        virtualKey = scanResult == -1 ? (byte)char.ToUpper(key) : (byte)(scanResult & 0xFF);
                        break;
                }
                
                keybd_event(virtualKey, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
                Thread.Sleep(20);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"按键释放失败: {ex.Message}");
            }
        }
        
        public static void SimulateKeyPress(string key)
        {
            if (string.IsNullOrEmpty(key) || key.Length == 0) return;
            SimulateKeyPress(key[0]);
        }

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
                Console.WriteLine($"检查CS2窗口状态失败: {ex.Message}");
                return false;
            }
        }
        
        public static void ReleaseAllKeys()
        {
            try
            {
                byte[] keysToRelease =
                [
                    0x57, // W
                    0x41, // A
                    0x53, // S
                    0x44, // D
                    0x20, // Space
                    0x10, // Shift
                    0x11, // Ctrl
                    0x12, // Alt
                    0x01, // Left Mouse
                    0x02  // Right Mouse
                ];

                foreach (var key in keysToRelease)
                {
                    keybd_event(key, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
                }
                
                Thread.Sleep(50);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"释放按键失败: {ex.Message}");
            }
        }
    }
}