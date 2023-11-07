using Llama.Data;
using Llama.Data.Interfaces;
using Llama.Data.Models;
using LlamaApi.Models.Request;
using LlamaApi.Shared.Models.Request;
using LlamaApi.Shared.Models.Response;

namespace LlamaApiClient
{
    public class LlamaContextClient : LlamaClient
    {
        private readonly LogitRuleCollection _logitRules = new();

        private Guid _contextGuid = Guid.Parse("5e375ced-115d-4b98-9ca3-c80254f90ba1");

        public LlamaContextClient(LlamaClientSettings settings, LlamaContextSettings contextSettings, LlamaModelSettings modelSettings, ContextRequestSettings contextRequestSettings) : base(settings, contextSettings, modelSettings, contextRequestSettings)
        {
        }

        public void AddRule(LogitRule rule)
        {
            if (rule.LifeTime != LogitRuleLifetime.Context)
            {
                throw new InvalidOperationException($"Only context lifetime rules can be added to {nameof(LlamaContextClient)}");
            }
        }

        public void DisposeContext() => this.DisposeContext(this._contextGuid);

        public Task Eval() => this.Eval(this._contextGuid);

        public Task<float[]> GetLogits() => this.GetLogits(this._contextGuid);

        public InferenceEnumerator Infer() => this.Infer(this._contextGuid, this._logitRules);

        public Task<ResponseLlamaToken> Predict(LogitRuleCollection? rules) => this.Predict(this._contextGuid, rules);

        public Task<IReadOnlyLlamaTokenCollection> Tokenize(string s) => this.Tokenize(this._contextGuid, s);

        public Task<ContextState> Write(RequestLlamaToken requestLlamaToken, int startIndex = -1) => this.Write(this._contextGuid, requestLlamaToken, startIndex);

        public Task<ContextState> Write(IEnumerable<RequestLlamaToken> requestLlamaTokens, int startIndex = -1) => this.Write(this._contextGuid, requestLlamaTokens, startIndex);

        public Task<ContextState> Write(string s, int startIndex = -1) => this.Write(this._contextGuid, s, startIndex);

        protected async Task<ContextState> LoadContext(LlamaContextSettings settings)
        {
            ContextState state = await base.LoadContext(settings, _contextGuid);

            return state;
        }
    }
}