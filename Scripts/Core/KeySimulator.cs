using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Threading;
using CommonSDK.Event;

// ReSharper disable InconsistentNaming

namespace ImLag.GUI.Scripts.Core;

public class KeySimulatorErrorOccurredEvent(string msg) : EventBase
{
    public string Message { get; set; } = msg;
}

[SuppressMessage("Interoperability", "SYSLIB1054:使用 \"LibraryImportAttribute\" 而不是 \"DllImportAttribute\" 在编译时生成 P/Invoke 封送代码")]
public static class KeySimulator
{
    [DllImport("user32.dll", SetLastError = true)]
    private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern short VkKeyScan(char ch);
    

    private const uint KEYEVENTF_KEYUP = 0x0002;
        
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
            EventBus.TriggerEvent(new KeySimulatorErrorOccurredEvent($"按键模拟失败: {ex.Message}"));
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
            EventBus.TriggerEvent(new KeySimulatorErrorOccurredEvent($"按键按下失败: {ex.Message}"));
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
            EventBus.TriggerEvent(new KeySimulatorErrorOccurredEvent($"按键释放失败: {ex.Message}"));
        }
    }
        
    public static void SimulateKeyPress(string key)
    {
        if (string.IsNullOrEmpty(key) || key.Length == 0) return;
        SimulateKeyPress(key[0]);
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
            EventBus.TriggerEvent(new KeySimulatorErrorOccurredEvent($"释放按键失败: {ex.Message}"));
        }
    }
}