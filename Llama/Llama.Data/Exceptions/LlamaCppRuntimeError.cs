namespace Llama.Data.Exceptions
{
    public class LlamaCppRuntimeError : Exception
    {
        public LlamaCppRuntimeError()
        {
        }

        public LlamaCppRuntimeError(string message) : base(message)
        {
        }
    }
}