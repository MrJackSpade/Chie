using LlamaApi.Shared.Interfaces;

namespace ChieApi.CleanupPipeline
{
    public class InvalidContractionsCleaner : ITextCleaner
    {
        private readonly Dictionary<string, string> _contractions = new()
        {
            ["don;t"] = "don't"
        };

        public IEnumerable<string> Clean(IEnumerable<string> contents)
        {
            foreach (string content in contents)
            {
                string toReturn = content;

                foreach (KeyValuePair<string, string> pair in this._contractions)
                {
                    toReturn = toReturn.Replace(pair.Key, pair.Value, StringComparison.OrdinalIgnoreCase);
                }

                yield return toReturn;
            }
        }
    }
}