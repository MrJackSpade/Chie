using Llama.Data.Models;

namespace Llama.Data.Collections
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