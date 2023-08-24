using ChieApi.Interfaces;

namespace ChieApi.CleanupPipeline
{
    public class DanglingQuoteCleaner : ITextCleaner
    {
        private const string END_CHARS = ".!?";

        public string Clean(string content)
        {
            content = content.Replace("”", "\"");

            int q_count = content.Count(c => c == '\"');

            if (q_count % 2 == 1 && this.HasValidEnd(content))
            {
                return content.Trim('\"');
            }

            return content;
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