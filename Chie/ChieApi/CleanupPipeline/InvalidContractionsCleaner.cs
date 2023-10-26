using LlamaApi.Shared.Interfaces;

namespace ChieApi.CleanupPipeline
{
    public class InvalidContractionsCleaner : ITextCleaner
    {
        private readonly Dictionary<string, string> _contractions = new()
        {
            ["don;t"] = "don't"
        };

        public string Clean(string content)
        {
            string toReturn = content;

            foreach (KeyValuePair<string, string> pair in this._contractions)
            {
                toReturn = toReturn.Replace(pair.Key, pair.Value, StringComparison.OrdinalIgnoreCase);
            }

            return toReturn;
        }
    }
}