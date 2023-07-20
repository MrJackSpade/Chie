namespace ChieApi.Models
{
    public record InputText(string Content, LlamaTokenType TokenType = LlamaTokenType.Undefined);
}