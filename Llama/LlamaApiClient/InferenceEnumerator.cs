using LlamaApi.Models.Request;
using LlamaApi.Models.Response;

namespace LlamaApiClient
{
    public class InferenceEnumerator
    {
        private readonly Func<RequestLlamaToken, Task> _accept;

        private readonly Func<Task<ResponseLlamaToken>> _moveNext;

        public InferenceEnumerator(Func<Task<ResponseLlamaToken>> moveNext, Func<RequestLlamaToken, Task> accept)
        {
            this._moveNext = moveNext;
            this._accept = accept;
        }

        public ResponseLlamaToken Current { get; private set; }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;

        public async ValueTask<bool> MoveNextAsync()
        {
            if (this.Current != null)
            {
                await _accept(new RequestLlamaToken() {
                    TokenId = this.Current.Id,
                    TokenType = Llama.Data.Enums.LlamaTokenType.Response
                });
            }

            this.Current = await _moveNext.Invoke();

            return this.Current.Id > 2;
        }

        public async ValueTask<bool> Regenerate()
        {
            this.Current = await _moveNext.Invoke();

            return this.Current.Id > 2;
        }
    }
}