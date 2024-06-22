namespace LlamaApi.Shared.Interfaces
{
    public interface ITextCleaner
    {
        IEnumerable<string> Clean(IEnumerable<string> content);
    }
}