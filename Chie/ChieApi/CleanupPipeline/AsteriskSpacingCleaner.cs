using LlamaApi.Shared.Interfaces;

namespace ChieApi.CleanupPipeline
{
    public class AsteriskSpacingCleaner : ITextCleaner
    {
        public static IEnumerable<string> AdjustAsteriskSpacing(IEnumerable<string> inputs)
        {
            foreach (var input in inputs)
            {
                if (!input.Contains('*'))
                {
                    yield return input;
                    continue;
                }

                // Split the string by asterisk
                string[] parts = input.Split('*');

                // If the number of parts is odd (meaning there are even number of asterisks)
                if (parts.Length % 2 == 0)
                {
                    yield return input;  // Early exit if there's an odd number of asterisks or none
                    continue;
                }

                for (int i = 1; i < parts.Length; i += 2)
                {
                    // Trim the spaces of the inside content
                    parts[i] = parts[i].Trim();
                }

                // Join the parts back using asterisk
                yield return string.Join("*", parts);
            }
        }

        public IEnumerable<string> Clean(IEnumerable<string> content) => AdjustAsteriskSpacing(content);
    }
}