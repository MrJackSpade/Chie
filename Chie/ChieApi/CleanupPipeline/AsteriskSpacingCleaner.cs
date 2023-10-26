using LlamaApi.Shared.Interfaces;

namespace ChieApi.CleanupPipeline
{
    public class AsteriskSpacingCleaner : ITextCleaner
    {
        public static string AdjustAsteriskSpacing(string input)
        {
            if (!input.Contains('*'))
            {
                return input;
            }

            // Split the string by asterisk
            string[] parts = input.Split('*');

            // If the number of parts is odd (meaning there are even number of asterisks)
            if (parts.Length % 2 == 0)
            {
                return input;  // Early exit if there's an odd number of asterisks or none
            }

            for (int i = 1; i < parts.Length; i += 2)
            {
                // Trim the spaces of the inside content
                parts[i] = parts[i].Trim();
            }

            // Join the parts back using asterisk
            return string.Join("*", parts);
        }

        public string Clean(string content) => AdjustAsteriskSpacing(content);
    }
}