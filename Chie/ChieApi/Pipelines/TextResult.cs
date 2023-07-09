namespace ChieApi.Pipelines
{
    public partial class UserDataPipeline
    {
        private class TextResult
        {
            public TextResult()
            {
            }

            public TextResult(string content, string tag)
            {
                this.HasValue = !string.IsNullOrWhiteSpace(content);
                this.Tag = tag;
                this.Content = content;
            }

            public string Content { get; set; } = string.Empty;

            public bool HasValue { get; set; }

            public string Tag { get; set; } = string.Empty;
        }
    }
}