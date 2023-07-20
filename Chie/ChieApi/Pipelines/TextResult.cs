using ChieApi.Models;

namespace ChieApi.Pipelines
{
    public partial class UserDataPipeline
    {
        private class TextResult
        {
            public TextResult()
            {
            }

            public TextResult(string content, LlamaTokenType type)
            {
                this.HasValue = !string.IsNullOrWhiteSpace(content);
                this.Type = type;
                this.Content = content;
            }

            public string Content { get; set; } = string.Empty;

            public bool HasValue { get; set; }

            public LlamaTokenType Type { get; set; } = LlamaTokenType.Undefined;
        }
    }
}