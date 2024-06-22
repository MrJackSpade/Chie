using ChieApi.Extensions;
using LlamaApi.Shared.Interfaces;
using System.Text;

namespace ChieApi.CleanupPipeline
{
    /// <summary>
    /// Splits long bot responses into multiple messages, to prevent the bot from "learning" to give long responses.
    /// </summary>
    public class ResponseSplitCleaner : ITextCleaner
    {
        private readonly CharacterConfiguration _characterConfiguration;

        public ResponseSplitCleaner(CharacterConfiguration characterConfiguration)
        {
            _characterConfiguration = characterConfiguration;
        }

        public IEnumerable<string> Clean(IEnumerable<string> contents)
        {
            foreach (string content in contents)
            {
                StringBuilder toReturn = new();

                string[] chunks = content.Split("\n\n").Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();

                for (int i = 0; i < chunks.Length; i++)
                {
                    if (i > 0)
                    {
                        toReturn.AppendLine(_characterConfiguration.EndOfTextToken);
                        toReturn.Append(_characterConfiguration.GetHeaderForBot());
                    }

                    toReturn.Append(chunks[i].Trim());
                }

                yield return toReturn.ToString();
            }
        }
    }
}