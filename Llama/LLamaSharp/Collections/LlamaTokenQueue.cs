using Llama.Data;

namespace Llama.Collections
{
    public class LlamaTokenQueue : LlamaTokenCollection
    {
        public LlamaToken Dequeue()
        {
            LlamaToken toReturn = this._tokens[0];
            this._tokens.RemoveAt(0);
            return toReturn;
        }
    }
}