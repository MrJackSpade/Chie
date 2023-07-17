using Llama.Data.Collections;
using Llama.Data.Enums;
using Llama.Data.Extensions;
using Llama.Data.Interfaces;
using Llama.Data.Models;
using Llama.Data.Scheduler;
using Llama.Extensions;

namespace Llama.Core
{
    public class ContextEvaluator : IDisposable
    {
        private readonly Dictionary<int, float> _logitBias = new();

        /// <summary>
        /// Please refer `LlamaModelSettings` to find the meanings of each arg. Be sure to have set the `n_gpu_layers`, otherwise it will
        /// load 20 layers to gpu by default.
        /// </summary>
        /// <param name="llamaModelSettings">The LlamaModel params</param>
        /// <param name="name">Model name</param>
        /// <param name="verbose">Whether to output the detailed info.</param>
        /// <param name="encoding"></param>
        /// <exception cref="LlamaCppRuntimeError"></exception>
        public unsafe ContextEvaluator(IContext context, string name = "", bool verbose = false)
        {
            this.Name = name;
            this.Verbose = verbose;
            this.Context = context;
        }

        public IContext Context { get; private set; }

        public string Name { get; set; }

        public bool Verbose { get; set; }

        public void ClearBias() => this._logitBias.Clear();

        public void Dispose() => this.Context.Dispose();

        public int Evaluate(ExecutionPriority priority) => this.Context.Evaluate(priority);

        /// <summary>
        /// Call the model to run inference.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="encoding"></param>
        /// <returns></returns>
        /// <exception cref="LlamaCppRuntimeError"></exception>
        public LlamaToken Predict(ExecutionPriority priority, Dictionary<int, float> logitBias)
        {
            this.Context.Evaluate(priority);

            Dictionary<int, float> bias = new()
            {
                this._logitBias
            };

            bias.AddOrUpdate(logitBias);

            return this.Context.SampleNext(bias, priority);
        }

        public void SetBias(int token, float bias)
        {
            if (!this._logitBias.ContainsKey(token))
            {
                this._logitBias.Add(token, bias);
            }
            else
            {
                this._logitBias[token] = bias;
            }
        }

        public LlamaTokenCollection Tokenize(string toTokenize, LlamaTokenType tokenType) => this.Context.Tokenize(toTokenize, tokenType);

        public LlamaTokenCollection Tokenize(string toTokenize) => this.Context.Tokenize(toTokenize, LlamaTokenType.Undefined);

        public void Write(params string[] inputText) => this.Write(inputText.Select(t => new InputText(t)).ToArray());

        public void Write(params InputText[] text) => this.ProcessInputText(text);

        public void Write(LlamaTokenCollection text) => this.Context.Write(text);

        private void ProcessInputText(params InputText[] inputTexts)
        {
            foreach (InputText inputText in inputTexts)
            {
                string text = inputText.Content;

                Console.Write(text);

                if (text.Length > 1)
                {
                    LlamaTokenCollection line_inp = this.Context.Tokenize(text, inputText.TokenType);

                    this.Context.Write(line_inp);
                }
            }
        }
    }
}