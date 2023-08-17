using Llama.Data.Collections;
using Llama.Data.Interfaces;
using Llama.Data.Models;
using LlamaApi.Models.Request;
using LlamaApi.Shared.Models.Response;

namespace LlamaApiClient
{
    public partial class InferenceEnumerator
    {
        private readonly Func<RequestLlamaToken, Task> _accept;

        private readonly LlamaTokenCollection _enumerated = new();

        private readonly LogitRuleCollection _logitRuleCollection = new();

        private readonly Func<LogitRuleCollection, Task<ResponseLlamaToken>> _moveNext;

        private int _moveBack = 0;

        public InferenceEnumerator(Func<LogitRuleCollection, Task<ResponseLlamaToken>> moveNext, Func<RequestLlamaToken, Task> accept, LogitRuleCollection ruleCollection)
        {
            this._moveNext = moveNext;
            this._accept = accept;

            foreach (LogitRule rule in ruleCollection)
            {
                this.AddLogitRule(rule);
            }
        }

        public bool Accepted { get; private set; }

        public ResponseLlamaToken Current { get; private set; }

        public IReadOnlyLlamaTokenCollection Enumerated => this._enumerated;

        public async Task Accept(LlamaToken token)
        {
            this.Current = new ResponseLlamaToken()
            {
                Id = token.Id,
                Value = token.Value
            };

            this.Accepted = true;

            await this._accept(new RequestLlamaToken() { TokenId = token.Id });

            this._enumerated.Append(token);

            //Clear out any temp bias from the last run if we didn't move back
            this._logitRuleCollection.Remove(LogitRuleLifetime.Token);
        }

        public async Task Accept()
        {
            if (!this.Accepted)
            {
                this.Accepted = true;

                if (this.Current != null)
                {
                    await this._accept(new RequestLlamaToken()
                    {
                        TokenId = this.Current.Id
                    });

                    this._enumerated.Append(new LlamaToken(this.Current.Id, this.Current.Value));
                }

                //Clear out any temp bias from the last run if we didn't move back
                this._logitRuleCollection.Remove(LogitRuleLifetime.Token);
            }
        }

        public void AddLogitRule(LogitRule rule)
        {
            if (rule.LifeTime == LogitRuleLifetime.Context)
            {
                throw new ArgumentException("Can not add logit rule lifetime of context to inferrence enumerator");
            }

            this._logitRuleCollection.AddOrUpdate(rule);
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;

        public void MoveBack()
        {
            this._moveBack += 1;

            if (this._moveBack > 1)
            {
                throw new NotImplementedException("Can not move back more than one token");
            }
        }

        public async ValueTask<bool> MoveNextAsync()
        {
            if (this._moveBack == 0)
            {
                await this.Accept();
            }
            else
            {
                this._moveBack -= 1;
            }

            this.Accepted = false;

            //apply the lastTemporaryBias and invoke
            this.Current = await this._moveNext.Invoke(this._logitRuleCollection);

            return this.Current.Id > 2;
        }
    }
}