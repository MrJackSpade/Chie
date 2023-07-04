using Llama.Collections;
using Llama.Constants;
using Llama.Context;
using Llama.Data;
using Llama.Events;
using Llama.Shared;

namespace Llama
{
    public class LlamaClient
    {
        private static Thread? _inferenceThread;

        private readonly ContextEvaluator _chatEvaluator;

        private readonly List<InputText> _queued = new();

        private readonly Shared.LlamaSettings _settings;

        private bool _killSent;

        private char _lastChar;

        public LlamaClient(Shared.LlamaSettings settings)
        {
            this._settings = settings;

            ContextEvaluatorBuilder builder = new(settings);

            this._chatEvaluator = builder.BuildChatEvaluator();

            this._chatEvaluator.OnContextModification += this.LlamaContext_OnContextModification;
        }

        public event Action<ContextModificationEventArgs> OnContextModification;

        public event EventHandler<DisconnectEventArgs> OnDisconnect;

        public event EventHandler<LlamaTokenCollection>? ResponseReceived;

        public event EventHandler<LlameClientTokenGeneratedEventArgs>? TokenGenerated;

        public bool Connected { get; private set; }

        public bool EndsWithReverse => this._lastChar == this._settings.PrimaryReversePrompt?.Last();

        public bool HasQueuedMessages => this._queued.Count > 0;

        public LlamaSettings Params { get; private set; }

        public async Task Connect()
        {
            if (this._chatEvaluator.IsNewSession)
            {
                if (!string.IsNullOrWhiteSpace(this._settings.Start))
                {
                    string toSend = this._settings.Start;

                    if (File.Exists(this._settings.Start))
                    {
                        toSend = File.ReadAllText(this._settings.Start);
                    }

                    if (!this._settings.Prompt.EndsWith("\n"))
                    {
                        toSend = "\n" + toSend;
                    }

                    this.Send(toSend, LlamaTokenTags.INPUT, false);
                }
            }
        }

        public void Send(string toSend, string tag, bool flush = true, bool validateReversal = true)
        {
            //If the last message was a manual kill, then we're not going to have the
            //expected turnaround token, so we need to make sure we append it to the beginning
            //of the next message so that its available in the right place in the context
            if (validateReversal && this._killSent && this._lastChar != this._settings.PrimaryReversePrompt.Last() && !toSend.StartsWith(this._settings.PrimaryReversePrompt.Last()))
            {
                toSend = $"{this._settings.PrimaryReversePrompt}{toSend}";
            }

            if (!flush)
            {
                toSend += "\n";
            }

            this._queued.Add(new InputText(toSend, tag));

            if (flush)
            {
                this.StartInference(this._queued.ToArray());

                this._queued.Clear();
            }

            this._killSent = false;
        }

        private void LlamaContext_OnContextModification(object sender, ContextModificationEventArgs obj) => this.OnContextModification?.Invoke(obj);

        private void StartInference(params InputText[] data)
        {
            _inferenceThread = new Thread(() =>
            {
                LlamaTokenCollection result = new();

                foreach (LlamaToken chunk in this._chatEvaluator.Call(data))
                {
                    if (string.IsNullOrEmpty(chunk.Value))
                    {
                        continue;
                    }

                    this.TokenGenerated?.Invoke(this, new LlameClientTokenGeneratedEventArgs()
                    {
                        Token = chunk
                    });

                    result.Append(chunk);
                    this._lastChar = chunk.Value[^1];
                }

                string resultStr = result.ToString();

                if (!resultStr.Contains('\n'))
                {
                    Console.Write('\n');
                }

                this.ResponseReceived?.Invoke(this, result);
            });

            _inferenceThread.Start();
        }
    }
}