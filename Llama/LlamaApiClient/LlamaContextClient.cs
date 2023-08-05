using Llama.Data;
using Llama.Data.Collections;
using Llama.Data.Interfaces;
using LlamaApi.Models.Request;
using LlamaApi.Shared.Models.Response;

namespace LlamaApiClient
{
    public class LlamaContextClient : LlamaClient
    {
        private Guid _contextGuid = Guid.NewGuid();

        public LlamaContextClient(LlamaClientSettings settings) : base(settings)
        {
        }

        public void DisposeContext() => base.DisposeContext(this._contextGuid);

        public Task Eval() => base.Eval(this._contextGuid);

        public InferenceEnumerator Infer() => base.Infer(this._contextGuid);

        public override async Task<ContextState> LoadContext(LlamaContextSettings settings, Action<ContextRequest> settingsAction)
        {
            ContextState state = await base.LoadContext(settings, settingsAction);

            this._contextGuid = state.Id;

            return state;
        }

        public Task<ResponseLlamaToken> Predict(Dictionary<int, float>? bias = null) => base.Predict(this._contextGuid, bias);

        public Task<IReadOnlyLlamaTokenCollection> Tokenize(string s) => base.Tokenize(this._contextGuid, s);

        public Task<ContextState> Write(RequestLlamaToken requestLlamaToken, int startIndex = -1) => base.Write(this._contextGuid, requestLlamaToken, startIndex);

        public Task<ContextState> Write(IEnumerable<RequestLlamaToken> requestLlamaTokens, int startIndex = -1) => base.Write(this._contextGuid, requestLlamaTokens, startIndex);

        public Task<ContextState> Write(string s, int startIndex = -1) => base.Write(this._contextGuid, s, startIndex);
    }
}