using System.Runtime.InteropServices;

namespace Ai.Utils.Services
{
    public static class ConsoleControl
    {
        // A delegate type to be used as the handler routine
        // for SetConsoleCtrlHandler.
        public delegate bool HandlerRoutine(CtrlTypes ctrlType);

        public enum CtrlTypes
        {
            CTRL_C_EVENT = 0,

            CTRL_BREAK_EVENT,

            CTRL_CLOSE_EVENT,

            CTRL_LOGOFF_EVENT = 5,

            CTRL_SHUTDOWN_EVENT
        }

        public static void DisableSelfKill() => Console.CancelKeyPress += new ConsoleCancelEventHandler(MyHandler);

        public static void EnableSelfKill() => _ = SetConsoleCtrlHandler(null, false);

        [DllImport("Kernel32.dll")]
        public static extern bool GenerateConsoleCtrlEvent(CtrlTypes dwCtrlEvent, int dwProcessGroupId);

        public static void GenerateKillSignal()
        {
            bool fResult = GenerateConsoleCtrlEvent(CtrlTypes.CTRL_C_EVENT, 0);
            if (!fResult)
            {
                Console.WriteLine("GenerateConsoleCtrlEvent failed with error" + Marshal.GetLastWin32Error());
            }
        }

        private static void MyHandler(object? sender, ConsoleCancelEventArgs e) => e.Cancel = true;

        private static bool ParentConsoleCtrlCheck(CtrlTypes sig) => true;

        [DllImport("Kernel32", SetLastError = true)]
        private static extern bool SetConsoleCtrlHandler(HandlerRoutine handler, bool add);
    }
}