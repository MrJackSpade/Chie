using System.Text;
using System.Text.Json;

namespace ContextStateBrowser
{
    internal class Program
    {
        private const string DIRECTORY = "D:\\Chie\\Code\\Chie\\ChieApi\\ContextStates";

        public static void FindAndPrintValueProperties(JsonElement element, StringBuilder sb)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    // Iterate through object properties
                    foreach (JsonProperty property in element.EnumerateObject().OrderBy(p => p.Name == "UserName" ? 0 : 1).ThenBy(p => p.Name))
                    {
                        // Check if the property name is "Value"
                        if (property.Name.Equals("Value", StringComparison.OrdinalIgnoreCase))
                        {
                            // Assuming the "Value" property is a string
                            sb.Append(property.Value.GetString());
                        }

                        // Recursively search in this property
                        FindAndPrintValueProperties(property.Value, sb);

                        if (property.Name == "UserName")
                        {
                            sb.Append(": ");
                        }
                    }

                    break;

                case JsonValueKind.Array:
                    // Iterate through array elements
                    foreach (JsonElement item in element.EnumerateArray())
                    {
                        // Recursively search in this array item
                        FindAndPrintValueProperties(item, sb);

                        if (!item.TryGetProperty("Value", out _))
                        {
                            sb.AppendLine();
                        }
                    }

                    break;
                    // Other types are ignored in this case
            }
        }

        private static void Main(string[] args)
        {
            List<string> states = Directory.EnumerateFiles(DIRECTORY).OrderByDescending(f => new FileInfo(f).LastWriteTime).ToList();

            int index = 0;

            do
            {
                Console.Title = Path.GetFileName(states[index]);

                JsonDocument doc = JsonDocument.Parse(File.ReadAllText(states[index]));

                JsonElement root = doc.RootElement;

                StringBuilder sb = new();

                FindAndPrintValueProperties(root, sb);

                string result = sb.ToString();

                string end = result[^(Console.WindowHeight * Console.WindowWidth)..];

                Console.Clear();

                Console.Write(end);

                ConsoleKey key = Console.ReadKey(true).Key;

                switch (key)
                {
                    case ConsoleKey.Escape:
                        return;

                    case ConsoleKey.DownArrow:
                        index--;
                        break;

                    case ConsoleKey.UpArrow:
                        index++;
                        break;
                }

                if (index == states.Count)
                {
                    index = 0;
                }

                if (index == -1)
                {
                    index = states.Count - 1;
                }
            } while (true);
        }
    }
}