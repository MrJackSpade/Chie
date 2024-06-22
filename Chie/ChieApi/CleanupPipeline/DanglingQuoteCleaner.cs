using LlamaApi.Shared.Interfaces;

namespace ChieApi.CleanupPipeline
{
    public class DanglingQuoteCleaner : ITextCleaner
    {
        private const string END_CHARS = ".!?";

        public IEnumerable<string> Clean(IEnumerable<string> contents)
        {
            foreach (string rcontent in contents)
            {
                string content = rcontent.Replace("”", "\"");

                int q_count = content.Count(c => c == '\"');

                if (q_count % 2 == 1 && this.HasValidEnd(content))
                {
                    yield return content.Trim('\"');
                    continue;
                }

                yield return content;
            }
        }

        private bool HasValidEnd(string content)
        {
            foreach (char c in END_CHARS)
            {
                if (content.EndsWith($"{c}\""))
                {
                    return true;
                }
            }

            return false;
        }
    }
}