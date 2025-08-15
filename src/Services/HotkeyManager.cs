using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Windows.Interop;
using FastScreeny.Models;

namespace FastScreeny.Services
{
    public class HotkeyManager : IDisposable
    {
        private readonly System.Windows.Application _app;
        private readonly Dictionary<int, Action> _idToAction = new();
        private int _nextId = 1;
        private HwndSource? _source;
        private static readonly IntPtr HWND_MESSAGE = new IntPtr(-3);

        public HotkeyManager(System.Windows.Application app)
        {
            _app = app;
            EnsureMessageWindow();
        }

        private void EnsureMessageWindow()
        {
            if (_source != null) return;
            var hook = new HwndSourceHook(WndProc);
            _source = new HwndSource(new HwndSourceParameters("FastScreenyHotkeySource")
            {
                WindowStyle = 0,
                ParentWindow = HWND_MESSAGE,
            });
            _source.AddHook(hook);
        }

        public bool RegisterHotkey(Hotkey hotkey, Action action)
        {
            EnsureMessageWindow();
            int id = _nextId++;
            if (RegisterHotKey(_source!.Handle, id, (uint)hotkey.Modifiers, KeyInterop.VirtualKeyFromKey(hotkey.Key)))
            {
                _idToAction[id] = action;
                return true;
            }
            return false;
        }

        public void UnregisterAll()
        {
            if (_source == null) return;
            foreach (var id in _idToAction.Keys)
            {
                UnregisterHotKey(_source.Handle, id);
            }
            _idToAction.Clear();
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_HOTKEY = 0x0312;
            if (msg == WM_HOTKEY)
            {
                int id = wParam.ToInt32();
                if (_idToAction.TryGetValue(id, out var action))
                {
                    action();
                    handled = true;
                }
            }
            return IntPtr.Zero;
        }

        public void Dispose()
        {
            UnregisterAll();
            _source?.Dispose();
        }

        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, int vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
    }
}


