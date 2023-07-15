using Llama.Data.Enums;

namespace Llama.Data.Models
{
    public record InputText(string Content, LlamaTokenType TokenType = LlamaTokenType.Undefined);
}