using Llama.Collections;
using Llama.Constants;
using Llama.Context.Extensions;
using Llama.Context.Interfaces;
using Llama.Data;
using Llama.Events;
using Llama.Exceptions;
using Llama.Extensions;
using Llama.Pipeline.Interfaces;
using Llama.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace Llama.Context
{
    public class ContextEvaluator : IDisposable
    {
        public IContext Context { get; private set; }

        private readonly LlamaContextSettings _contextSettings;

        private readonly LlamaTokenQueue _evaluationQueue = new();

        private readonly AutoResetEvent _inferenceGate = new(true);

        private readonly ITextSanitizer _textSanitizer;

        private readonly IContextRoller _contextRoller;

        private Thread _cleanupThread;

        private LlamaTokenCollection _prompt;

        /// <summary>
        /// Please refer `LlamaModelSettings` to find the meanings of each arg. Be sure to have set the `n_gpu_layers`, otherwise it will
        /// load 20 layers to gpu by default.
        /// </summary>
        /// <param name="llamaModelSettings">The LlamaModel params</param>
        /// <param name="name">Model name</param>
        /// <param name="verbose">Whether to output the detailed info.</param>
        /// <param name="encoding"></param>
        /// <exception cref="RuntimeError"></exception>
        public unsafe ContextEvaluator(IContext context, ITextSanitizer textSanitizer, IContextRoller contextRoller, LlamaContextSettings contextSettings, string name = "", bool verbose = false)
        {
            if (contextRoller is null)
            {
                throw new ArgumentNullException(nameof(contextRoller));
            }

            if (contextSettings.Encoding == Encoding.Unicode)
            {
                throw new ArgumentException("Unicode not supported. Did you mean UTF8?");
            }

            this._textSanitizer = textSanitizer;
            this._contextRoller = contextRoller;
            this._contextSettings = contextSettings;

            this.Name = name;
            this.Verbose = verbose;
            this.Context = context;

            context.OnContextModification += (s, o) => this.OnContextModification?.Invoke(this, o);

            this.TryLoad();
        }

        public event EventHandler<ContextModificationEventArgs> OnContextModification;

        public event EventHandler<LlamaTokenCollection> QueueWritten;

        public bool IsNewSession { get; private set; } = true;

        public string Name { get; set; }

        public bool Verbose { get; set; }

        public IEnumerable<LlamaToken> Call(params string[] inputText) => this.Call(inputText.Select(t => new InputText(t)).ToArray());

        public IEnumerable<LlamaToken> Call(params InputText[] text)
        {
            this._inferenceGate.WaitOne();
            this.ProcessInputText(text);
            return this.Call();
        }

        public IEnumerable<LlamaToken> Call(LlamaTokenCollection text)
        {
            this._inferenceGate.WaitOne();
            this._evaluationQueue.Append(text);
            return this.Call();
        }

        public void Dispose() => this.Context.Dispose();

        public void Load(string fileName)
        {
            this._evaluationQueue.Clear();
            this.Context.Load(fileName);
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

        public void Save(string fileName) => this.Context.Save(fileName);

        /// <summary>
        /// Apply a prompt to the model.
        /// </summary>
        /// <param name="prompt"></param>
        /// <param name="encoding"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public void SetPrompt(string prompt)
        {
            if (string.IsNullOrEmpty(prompt))
            {
                return;
            }

            prompt = this._textSanitizer.Sanitize(prompt);

            if (!prompt.StartsWith(" ") && char.IsLetter(prompt[0]))
            {
                LlamaLogger.Default.Warn("Input prompt does not start with space and may have issues as a result");
            }

            this._evaluationQueue.Append(this.Context.Tokenize(prompt, LlamaTokenTags.PROMPT, true));

            this._prompt = this.Context.Tokenize(prompt, LlamaTokenTags.PROMPT);

            if (this._evaluationQueue.Count > this.Context.Size - 4)
            {
                throw new ArgumentException($"prompt is too long ({this._evaluationQueue.Count} tokens, max {this.Context.Size - 4})");
            }
        }

        public LlamaTokenCollection Tokenize(string toTokenize, string tag) => this.Context.Tokenize(toTokenize, tag);

        public LlamaTokenCollection Tokenize(string toTokenize) => this.Context.Tokenize(toTokenize, LlamaTokenTags.UNMANAGED);

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
        /// Call the model to run inference.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="encoding"></param>
        /// <returns></returns>
        /// <exception cref="RuntimeError"></exception>
        private IEnumerable<LlamaToken> Call()
        {
            LlamaTokenCollection thisCall = new();

            this._evaluationQueue.Ensure();

            bool breakAfterEval = false;

            do
            {
                LlamaTokenCollection queueWritten = new();

                if (this._evaluationQueue.Count > 0)
                {
                    while(this.Context.AvailableBuffer <  this._evaluationQueue.Count + 1)
                    {
                        LlamaTokenCollection newContext = this._contextRoller.GenerateContext(this.Context, this.Tokenize(_textSanitizer.Sanitize(this._contextSettings.Prompt), LlamaTokenTags.PROMPT), this._contextSettings.KeepContextTokenCount);

                        this.Context.SetBuffer(newContext);
                    }

                    this.Context.Write(this._evaluationQueue);
                    queueWritten.Append(this._evaluationQueue);
                    Console.Title = $"{this.Context.AvailableBuffer}";
                    this._evaluationQueue.Clear();
                }

                this.Context.Evaluate();

                if (queueWritten.Count > 0)
                {
                    this.QueueWritten?.Invoke(this, queueWritten);
                }

                if (breakAfterEval)
                {
                    this.Cleanup();
                    yield break;
                }

                if (this._evaluationQueue.Count == 0)
                {
                    SampleResult sample = this.Context.SampleNext(thisCall);

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

        private void Cleanup()
        {
            LlamaTokenCollection checkMe = new(this.Context.Evaluated.Where(t => t.Id != 0));

            checkMe.Ensure();

            this._cleanupThread = new Thread(() =>
            {
                this.Context.PostProcess();

                if (this._contextSettings.AutoSave)
                {
                    this.Context.Evaluated.Ensure();
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
                    LlamaTokenCollection line_inp = this.Context.Tokenize(text, inputText.Tag);

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
                    this.Context.Load(latest.FullName);
                    this.IsNewSession = false;
                    string cleanedPrompt = this._textSanitizer.Sanitize(this._contextSettings.Prompt);

                    this._prompt = this.Context.Tokenize(cleanedPrompt, LlamaTokenTags.PROMPT);
                    this._prompt.Ensure();
                    return;
                }
                else
                {
                    Console.WriteLine("No context found. Prompting.");
                }
            }

            this.SetPrompt(this._contextSettings.Prompt);
        }
    }
}