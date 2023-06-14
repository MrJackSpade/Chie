using Llama.Constants;

namespace Llama.Models
{
	public class InputText
	{
		public InputText(string content, string? tag = null)
		{
			this.Tag = tag ?? LlamaTokenTags.INPUT;
			this.Content = content;
		}

		public string Tag { get; set; }
		public string Content { get; set; }
	}
}
