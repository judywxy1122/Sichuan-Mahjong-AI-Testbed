using System;

namespace SichuanMahjong.Core
{
    /// <summary>
    /// Host hooks for side effects the rule engine triggers (Java used
    /// Toolkit.beep() and System.out.println directly). The Unity layer wires
    /// these to an AudioSource and Debug.Log; the console harness wires them
    /// to Console.WriteLine. Defaults are no-ops so the core never needs an
    /// engine reference.
    /// </summary>
    public static class CoreEnv
    {
        public static Action Beep = () => { };
        public static Action<string> Println = _ => { };
    }
}
