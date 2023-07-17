using LlamaApi.Models.Request;
using LlamaApi.Models.Response;
using Llama.Data.Extensions;

namespace LlamaApiClient
{
    public enum LogitBiasLifeTime
    {
        Temporary,
        Inferrence
    }

    public class InferenceEnumerator
    {
        private readonly Func<RequestLlamaToken, Task> _accept;

        private readonly Func<Dictionary<int, float>, Task<ResponseLlamaToken>> _moveNext;

        private Dictionary<int, float> _lastTemporaryBias = new();

        private readonly Dictionary<int, float> _temporaryBias = new();

        private readonly Dictionary<int, float> _inferrenceBias = new();

        private int _moveBack = 0;

        public void SetLogit(int tokenId, float value, LogitBiasLifeTime lifeTime)
        {
            switch(lifeTime)
            {
                case LogitBiasLifeTime.Temporary:
                    _temporaryBias.AddOrUpdate(tokenId, value);
                    break;
                case LogitBiasLifeTime.Inferrence:
                    _inferrenceBias.AddOrUpdate(tokenId, value);
                    break;
            }
        }

        private Dictionary<int, float> GetCurrentBias()
        {
            Dictionary<int, float> toReturn = new()
            {
                this._inferrenceBias
            };

            toReturn.AddOrUpdate(this._lastTemporaryBias);

            //temporaryBias must come after lastTemporaryBias
            //to ensure that new bias adjustments are reflected 
            //on regen
            toReturn.AddOrUpdate(this._temporaryBias);

            return toReturn;
        }

        public InferenceEnumerator(Func<Dictionary<int, float>, Task<ResponseLlamaToken>> moveNext, Func<RequestLlamaToken, Task> accept)
        {
            this._moveNext = moveNext;
            this._accept = accept;
        }

        public ResponseLlamaToken Current { get; private set; }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;

        public async ValueTask<bool> MoveNextAsync()
        {

            if (this._moveBack == 0)
            {
                if (this.Current != null)
                {
                    await this._accept(new RequestLlamaToken()
                    {
                        TokenId = this.Current.Id,
                        TokenType = Llama.Data.Enums.LlamaTokenType.Response
                    });
                }

                //Clear out any temp bias from the last run if we didn't move back
                this._lastTemporaryBias = new();
            }
            else
            {
                this._moveBack -= 1;
            }

            //Cache whatever our current bias is incase we need to use it again.
            //rebuilding the list here allows new tokens to be appended from the temporary 
            //collection in case adjustments were made after the last "Move Back"
            this._lastTemporaryBias = this.GetCurrentBias();

            //apply the lastTemporaryBias and invoke
            this.Current = await this._moveNext.Invoke(this._lastTemporaryBias);

            //clear the current temporary, they're in the _lastTemporary if we need to reinvoke
            return this.Current.Id > 2;
        }

        public void MoveBack()
        {
            this._moveBack += 1;

            if (this._moveBack > 1)
            {
                throw new NotImplementedException("Can not move back more than one token");
            }
        }
    }
}