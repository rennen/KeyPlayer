using System;
using System.Diagnostics;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Media;
using System.Linq;
using System.Collections.Generic;

namespace Main
{
    public class InterceptKeys
    {
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;

        private const int MAX_LISTEN = 10;

        private static LowLevelKeyboardProc _proc = HookCallback;
        private static IntPtr _hookID = IntPtr.Zero;
        private static readonly Dictionary<Keys, SoundPlayer> _soundPlayers = new Dictionary<Keys, SoundPlayer>();
        private static readonly List<Tuple<Keys[], SoundPlayer>> _patternPlayers = new List<Tuple<Keys[], SoundPlayer>>();

        private static Keys[] _lastEnteredChars = new Keys[MAX_LISTEN];
        private static int _lastEnteredInd = 0;

        public static void Main()
        {
            var badum = new SoundPlayer(typeof(InterceptKeys).Assembly.GetManifestResourceStream("Main.Ba dum tss.wav"));
            _soundPlayers[Keys.Q] = badum;

            var buzz = new SoundPlayer(typeof(InterceptKeys).Assembly.GetManifestResourceStream("Main.Buzzer Wrong Answer.wav"));
            _soundPlayers[Keys.W] = buzz;

            var applause = new SoundPlayer(typeof(InterceptKeys).Assembly.GetManifestResourceStream("Main.applause.wav"));
            _soundPlayers[Keys.E] = applause;

            var aww = new SoundPlayer(typeof(InterceptKeys).Assembly.GetManifestResourceStream("Main.aww.wav"));
            _soundPlayers[Keys.R] = aww;

            AddPattern("git", buzz);
            AddPattern("shit", buzz);
            AddPattern("saadon", applause);
            AddPattern("bootstrap", badum);

            _hookID = SetHook(_proc);
            Application.Run();
            UnhookWindowsHookEx(_hookID);
        }

        private static void AddPattern(string pattern, SoundPlayer player)
        {
            var reversedKeys = pattern.Select(c => (Keys)char.ToUpper(c)).Reverse().ToArray();
            _patternPlayers.Add(new Tuple<Keys[], SoundPlayer>(reversedKeys, player));
        }

        private static IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(WH_KEYBOARD_LL, proc,
                    GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (wParam == (IntPtr)WM_KEYDOWN)
            {
                int vkCode = Marshal.ReadInt32(lParam);
                if ((Control.ModifierKeys & Keys.Control) != 0 && (Control.ModifierKeys & Keys.Alt) != 0)
                {
                    var key = (Keys)vkCode;
                    if (_soundPlayers.TryGetValue(key, out SoundPlayer player))
                    {
                        player.Play();
                    }
                }
                if (vkCode >= (int)Keys.A && vkCode <= (int)Keys.Z)
                {
                    var currInd = _lastEnteredInd;
                    _lastEnteredChars[_lastEnteredInd] = (Keys)vkCode;
                    _lastEnteredInd = (_lastEnteredInd + 1) % MAX_LISTEN;

                    foreach (var pattern in _patternPlayers)
                    {
                        CheckMatch(currInd, pattern.Item1, pattern.Item2);
                    }
                }
            }

            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        private static void CheckMatch(int currInd, Keys[] pattern, SoundPlayer soundPlayer)
        {
            var match = pattern
                .Select((key, ind) => new { key, ind })
                .All(pair => _lastEnteredChars[(currInd - pair.ind + MAX_LISTEN) % MAX_LISTEN] == pair.key);

            if (match)
            {
                soundPlayer.Play();
            }

        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);
    }
}
