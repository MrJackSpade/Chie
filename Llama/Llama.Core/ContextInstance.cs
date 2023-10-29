using Llama.Data.Collections;
using Llama.Data.Interfaces;
using Llama.Data.Models;
using Llama.Data.Scheduler;
using Llama.Extensions;

namespace Llama.Core
{
    public class ContextInstance : IDisposable
    {
        /// <summary>
        /// Please refer `LlamaModelSettings` to find the meanings of each arg. Be sure to have set the `n_gpu_layers`, otherwise it will
        /// load 20 layers to gpu by default.
        /// </summary>
        /// <param name="llamaModelSettings">The LlamaModel params</param>
        /// <param name="name">Model name</param>
        /// <param name="verbose">Whether to output the detailed info.</param>
        /// <param name="encoding"></param>
        /// <exception cref="LlamaCppRuntimeError"></exception>
        public unsafe ContextInstance(IContext context, string name = "", bool verbose = false)
        {
            this.Name = name;
            this.Verbose = verbose;
            this.Context = context;
        }

        public IContext Context { get; private set; }

        public string Name { get; set; }

        public bool Verbose { get; set; }

        public void Dispose() => this.Context.Dispose();

        public void Evaluate(ExecutionPriority priority) => this.Context.Evaluate(priority);

        /// <summary>
        /// Call the model to run inference.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="encoding"></param>
        /// <returns></returns>
        /// <exception cref="LlamaCppRuntimeError"></exception>
        public LlamaToken Predict(ExecutionPriority priority, LogitRuleCollection logitRules)
        {
            this.Context.Evaluate(priority);

            return this.Context.SampleNext(logitRules, priority);
        }

        public LlamaTokenCollection Tokenize(string toTokenize) => this.Context.Tokenize(toTokenize);

        public void Write(params string[] inputText) => this.ProcessInputText(inputText);

        public void Write(LlamaTokenCollection text) => this.Context.Write(text);

        private void ProcessInputText(params string[] inputTexts)
        {
            foreach (string inputText in inputTexts)
            {
                Console.Write(inputText);

                if (inputText.Length > 1)
                {
                    LlamaTokenCollection line_inp = this.Context.Tokenize(inputText);

                    this.Context.Write(line_inp);
                }
            }
        }
    }
}