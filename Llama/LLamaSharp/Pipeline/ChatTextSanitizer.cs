using Llama.Pipeline.Interfaces;

namespace Llama.Pipeline
{
    public class ChatTextSanitizer : ITextSanitizer
    {
        public string Sanitize(string text)
        {
            while (text.Contains("\r\n"))
            {
                text = text.Replace("\r\n", "\n");
            }

            while (text.Contains('\r'))
            {
                text = text.Replace("\r", "\n");
            }

            while (text.Contains("\n\n"))
            {
                text = text.Replace("\n\n", "\n");
            }

            return text;
        }
    }
}