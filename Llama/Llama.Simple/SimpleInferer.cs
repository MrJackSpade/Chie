using Llama.Data;
using Llama.Data.Collections;
using Llama.Data.Models;
using Llama.Data.Native;
using Llama.Native;

namespace Llama.Simple
{
    public class SimpleInferer : IDisposable
    {
        private PointerArray<LlamaToken> _buffer;

        private KvCacheState<LlamaToken> _cache;

        private SafeLlamaContextHandle _context;

        private LlamaContextSettings _contextSettings;

        private LlamaModel _model;

        private LlamaModelSettings _modelSettings;

        private List<LlamaToken> _prompt = new();

        private bool disposedValue;

        public float RepetitionPenalty { get; set; }

        public float Temperature { get; set; }

        public float Tfs { get; set; }

        public int TopK { get; set; }

        public float TopP { get; set; }

        public float TypicalP { get; set; }

        private List<List<LlamaToken>> StopPrompts { get; set; } = new();

        public void AddStop(string text) => StopPrompts.Add(this.Tokenize(text).ToList());

        public void AddStop(int llamaToken) => StopPrompts.Add(new List<LlamaToken>() { new LlamaToken(llamaToken, this.TokenToPiece(llamaToken)) });

        public bool CheckStop()
        {
            foreach (List<LlamaToken> stop in StopPrompts)
            {
                int c = stop.Count - 1;

                for (uint i = _buffer.Pointer - 1; i > 0; i--)
                {
                    if (stop[c] != _buffer[i])
                    {
                        break;
                    }

                    c--;

                    if (c < 0)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            this.Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public async Task Evaluate()
        {
            Task t = Task.Run(() =>
            {
                KvCacheShifter shifter = new(this._contextSettings.ThreadCount, this._contextSettings.BatchSize, this._context, this._model.Handle);
                PointerArraySynchronizer<LlamaToken> pointerArraySynchronizer = new(shifter, new LlamaToken(-1, null));
                pointerArraySynchronizer.Sync(this._cache, this._buffer);
            });

            await t;
        }

        public Span<float> GetLogits()
        {
            int n_vocab = NativeApi.NVocab(this._model.Handle);

            Span<float> logits = NativeApi.GetLogits(this._context, n_vocab);

            return logits;
        }

        public async Task LoadContext(LlamaContextSettings llamaContextSettings, string prompt = null)
        {
            await Task.Run(() =>
            {
                _context = NativeApi.LoadContext(_model.Handle, llamaContextSettings);
                _cache = new KvCacheState<LlamaToken>(llamaContextSettings.ContextSize, new LlamaToken(-1, null));
                _buffer = new PointerArray<LlamaToken>(llamaContextSettings.ContextSize);
                _contextSettings = llamaContextSettings;

                if (!string.IsNullOrWhiteSpace(prompt))
                {
                    this._prompt = this.Tokenize(prompt).ToList();

                    this.Write(this._prompt);
                }
            }
            );
        }

        public async Task LoadModel(LlamaModelSettings modelSettings)
        {
            await Task.Run(() =>
            {
                _model = NativeApi.LoadModel(modelSettings);
                _modelSettings = modelSettings;
            });
        }

        public async IAsyncEnumerable<LlamaToken> Predict()
        {
            do
            {
                await this.Evaluate();

                LlamaTokenDataArray array = new(this.GetLogits());

                int id = -1;

                if (this.TopK != 0)
                {
                    SamplingApi.TopK(array, this.TopK, 1);
                    SamplingApi.SoftMax(array);
                }

                if (this.Tfs > 0)
                {
                    SamplingApi.TailFree(array, this.Tfs, 1);
                    SamplingApi.SoftMax(array);
                }

                if (this.TypicalP > 0)
                {
                    SamplingApi.Typical(this._context, array, this.Tfs, 1);
                    SamplingApi.SoftMax(array);
                }

                if (this.TopP > 0)
                {
                    SamplingApi.TopP(this._context, array, this.TopP, 1);
                    SamplingApi.SoftMax(array);
                }

                if (this.Temperature > 0)
                {
                    SamplingApi.Temperature(array, this.Temperature);
                    SamplingApi.SoftMax(array);
                }

                if (this.Temperature < 0)
                {
                    id = SamplingApi.TokenGreedy(array);
                }
                else
                {
                    id = SamplingApi.Token(this._context, array);
                }

                this.Write(id);

                yield return new LlamaToken(id, this.TokenToPiece(id));

                if (this.CheckStop())
                {
                    break;
                }
            } while (true);
        }

        public IEnumerable<LlamaToken> Tokenize(string text)
            => NativeApi.LlamaTokenize(this._model.Handle, text, false, false)
                        .Select(s => new LlamaToken(s, this.TokenToPiece(s)));

        public string TokenToPiece(int tokenId)
            => NativeApi.TokenToPiece(this._model.Handle, tokenId);

        public void Write(string text)
        {
            List<int> tokens = NativeApi.LlamaTokenize(_model.Handle, text, false, false);

            foreach (int token in tokens)
            {
                this.Write(token);
            }
        }

        public void Write(IEnumerable<int> tokens)
        {
            foreach (int token in tokens)
            {
                this.Write(token);
            }
        }

        public void Write(IEnumerable<LlamaToken> tokens)
        {
            foreach (LlamaToken token in tokens)
            {
                this.Write(token);
            }
        }

        public void Write(int tokenId)
        {
            LlamaToken token = new(tokenId, NativeApi.TokenToPiece(this._model.Handle, tokenId));

            this.Write(token);
        }

        public void Write(LlamaToken token)
        {
            if (_buffer.Count == _buffer.Length - 1)
            {
                _buffer.Slide(1);
            }

            _buffer.Write(token);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _model.Handle.Dispose();
                    _context.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }
    }
}