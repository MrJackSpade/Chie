using Llama.Constants;

namespace Llama.Data
{
    public class InputText
    {
        public InputText(string content, string? tag = null)
        {
            this.Tag = tag ?? LlamaTokenTags.INPUT;
            this.Content = content;
        }

        public string Content { get; set; }

        public string Tag { get; set; }
    }
}