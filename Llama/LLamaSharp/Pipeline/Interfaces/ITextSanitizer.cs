namespace Llama.Pipeline.Interfaces
{
    public interface ITextSanitizer
    {
        public string Sanitize(string text);
    }
}