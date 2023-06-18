namespace Ai.Utils.Services
{
    internal static class ConsoleService
    {
        private static readonly object _consoleLock = new();

        private static readonly ConsoleColor _defaultColor = Console.ForegroundColor;

        private static readonly ConsoleColor _errorColor = ConsoleColor.Red;

        static ConsoleService()
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
        }

        public static void Clear() => Console.Clear();

        public static void LogError(string message)
        {
            lock (_consoleLock)
            {
                Console.ForegroundColor = _errorColor;
                Console.WriteLine(message);
                Console.ForegroundColor = _defaultColor;
            }
        }

        public static string Select(List<string> options)
        {
            if (options is null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (options.Count == 0)
            {
                throw new ArgumentException("Select must have at least one option", nameof(options));
            }

            if (options.Count == 1)
            {
                return options[0];
            }

            for (int i = 0; i < options.Count; i++)
            {
                Console.WriteLine($"[{i}] {options[i]}");
            }

            do
            {
                try
                {
                    string entered = Console.ReadLine();
                    int selected = int.Parse(entered);
                    return options[selected];
                }
                catch (Exception e)
                {
                    LogError(e.Message);
                }
            } while (true);
        }
    }
}