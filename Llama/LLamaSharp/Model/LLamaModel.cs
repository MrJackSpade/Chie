using Llama.Collections;
using Llama.Constants;
using Llama.Context;
using Llama.Context.Extensions;
using Llama.Context.Interfaces;
using Llama.Data;
using Llama.Events;
using Llama.Exceptions;
using Llama.Extensions;
using Llama.Native;
using Llama.Pipeline.Interfaces;
using Llama.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace Llama.Model
{
    public class LlamaModel : IDisposable
    {
        private readonly IContext _context;

        private readonly LlamaContextSettings _contextSettings;

        private readonly LlamaTokenQueue _evaluationQueue = new();

        private readonly AutoResetEvent _inferenceGate = new(true);

        private Thread _cleanupThread;

        private LlamaTokenCollection _prompt;

        private readonly ITextSanitizer _textSanitizer;

        /// <summary>
        /// Please refer `LlamaModelSettings` to find the meanings of each arg. Be sure to have set the `n_gpu_layers`, otherwise it will
        /// load 20 layers to gpu by default.
        /// </summary>
        /// <param name="llamaModelSettings">The LlamaModel params</param>
        /// <param name="name">Model name</param>
        /// <param name="verbose">Whether to output the detailed info.</param>
        /// <param name="encoding"></param>
        /// <exception cref="RuntimeError"></exception>
        public unsafe LlamaModel(IContext context, ITextSanitizer textSanitizer, LlamaModelSettings llamaModelSettings, LlamaContextSettings contextSettings, string name = "", bool verbose = false)
        {
            if (contextSettings.Encoding == Encoding.Unicode)
            {
                throw new ArgumentException("Unicode not supported. Did you mean UTF8?");
            }

            this._textSanitizer = textSanitizer;

            this._contextSettings = contextSettings;

            this.Name = name;
            this.Verbose = verbose;
            this._context = context;

            context.OnContextModification += (s, o) => this.OnContextModification?.Invoke(o);

            foreach (KeyValuePair<int, float> kvp in contextSettings.LogitBias)
            {
                string toLog = $"Logit Bias: {Utils.PtrToStringUTF8(NativeApi.llama_token_to_str(this._context.Handle, kvp.Key))} -> {kvp.Value}";
                LlamaLogger.Default.Info(toLog);
            }

            this.TryLoad();
        }

        public bool IsNewSession { get; private set; } = true;

        public string Name { get; set; }

        public Action<ContextModificationEventArgs> OnContextModification { get; set; }

        public bool Verbose { get; set; }

        public IEnumerable<LlamaToken> Call(params string[] inputText) => this.Call(inputText.Select(t => new InputText(t)).ToArray());

        /// <summary>
        /// Call the model to run inference.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="encoding"></param>
        /// <returns></returns>
        /// <exception cref="RuntimeError"></exception>
        public IEnumerable<LlamaToken> Call(params InputText[] text)
        {
            this._inferenceGate.WaitOne();

            LlamaTokenCollection thisCall = new();

            this.ProcessInputText(text);

            this._evaluationQueue.Ensure();

            bool breakAfterEval = false;

            do
            {
                if (this._evaluationQueue.Count > 0)
                {
                    this._context.Write(this._evaluationQueue);
                    Console.Title = $"{this._context.AvailableBuffer}";
                    this._evaluationQueue.Clear();
                }

                this._context.Evaluate();

                if (breakAfterEval)
                {
                    this.Cleanup();
                    yield break;
                }

                if (this._evaluationQueue.Count == 0)
                {
                    SampleResult sample = this._context.SampleNext(thisCall);

                    this._evaluationQueue.Append(sample.Tokens);
                    thisCall.Append(sample.Tokens);

                    Console.Write(sample.Tokens.ToString());

                    foreach (LlamaToken token in sample.Tokens)
                    {
                        yield return token;
                    }

                    breakAfterEval = sample.IsFinal;
                }
            } while (true);
        }

        public void Dispose() => this._context.Dispose();

        public void Load(string fileName)
        {
            this._evaluationQueue.Clear();
            this._context.Load(fileName);
            this.IsNewSession = false;
        }

        public void LoadLatest()
        {
            FileInfo? latest = Directory.EnumerateFiles(this._contextSettings.SavePath).Select(f => new FileInfo(f)).OrderByDescending(f => f.LastWriteTime).FirstOrDefault() ?? throw new FileNotFoundException("No file found to load");

            this.Load(latest.FullName);
        }

        public void Save()
        {
            string path = new DirectoryInfo(this._contextSettings.SavePath).FullName;

            string fileName = Path.Combine(path, $"{this._contextSettings.RootSaveName}.{DateTime.Now.Ticks}.dat");

            this.Save(fileName);
        }

        public void Save(string fileName) => this._context.Save(fileName);

        /// <summary>
        /// Tokenize a string.
        /// </summary>
        /// <param name="text">The utf-8 encoded string to tokenize.</param>
        /// <returns>A list of tokens.</returns>
        /// <exception cref="RuntimeError">If the tokenization failed.</exception>
        public LlamaTokenCollection Tokenize(string text, string tag = null) => this._context.Tokenize(text, tag);

        public bool TryGetLatest(out FileInfo? latest)
        {
            if (!Directory.Exists(this._contextSettings.SavePath))
            {
                latest = null;
                return false;
            }

            latest = Directory.EnumerateFiles(this._contextSettings.SavePath, this._contextSettings.RootSaveName + ".*").Select(f => new FileInfo(f)).OrderByDescending(f => f.LastWriteTime).FirstOrDefault();
            return latest != null;
        }

        /// <summary>
        /// Apply a prompt to the model.
        /// </summary>
        /// <param name="prompt"></param>
        /// <param name="encoding"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public LlamaModel WithPrompt(string prompt)
        {
            prompt = this._textSanitizer.Sanitize(prompt);

            if (!prompt.StartsWith(" ") && char.IsLetter(prompt[0]))
            {
                LlamaLogger.Default.Warn("Input prompt does not start with space and may have issues as a result");
            }

            this._evaluationQueue.Append(this._context.Tokenize(prompt, LlamaTokenTags.PROMPT, true));

            this._prompt = this._context.Tokenize(prompt, LlamaTokenTags.PROMPT);

            if (this._evaluationQueue.Count > this._context.Size - 4)
            {
                throw new ArgumentException($"prompt is too long ({this._evaluationQueue.Count} tokens, max {this._context.Size - 4})");
            }

            if (this._evaluationQueue.Count > this._context.Size - 4)
            {
                throw new ArgumentException($"prompt is too long ({this._evaluationQueue.Count} tokens, max {this._context.Size - 4})");
            }

            return this;
        }

        private void Cleanup()
        {
            LlamaTokenCollection checkMe = new(this._context.Evaluated.Where(t => t.Id != 0));

            checkMe.Ensure();

            this._cleanupThread = new Thread(() =>
            {
                this._context.PostProcess();

                if (this._contextSettings.AutoSave)
                {
                    this._context.Evaluated.Ensure();
                    this.Save();
                }

                this._inferenceGate.Set();
            });

            this._cleanupThread.Start();
        }

        private void ProcessInputText(params InputText[] inputTexts)
        {
            foreach (InputText inputText in inputTexts)
            {
                string text = this._textSanitizer.Sanitize(inputText.Content);

                Console.Write(text);

                if (text.Length > 1)
                {
                    LlamaTokenCollection line_inp = this._context.Tokenize(text, inputText.Tag);

                    this._evaluationQueue.Append(line_inp);
                }
            }
        }

        private void TryLoad()
        {
            if (this._contextSettings.AutoLoad)
            {
                Console.WriteLine("Trying to load context...");

                if (this.TryGetLatest(out FileInfo latest))
                {
                    Console.WriteLine("Context found: " + latest);
                    this._context.Load(latest.FullName);
                    this.IsNewSession = false;
                    string cleanedPrompt = this._textSanitizer.Sanitize(this._contextSettings.Prompt);

                    this._prompt = this._context.Tokenize(cleanedPrompt, LlamaTokenTags.PROMPT);
                    this._prompt.Ensure();
                    return;
                }
                else
                {
                    Console.WriteLine("No context found. Prompting.");
                }
            }

            this.WithPrompt(this._contextSettings.Prompt);
        }
    }
}