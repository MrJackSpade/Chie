using ChieApi.Shared.Entities;
using ChieApi.Shared.Services;
using Llama.Collections;
using Llama.Context.Extensions;
using Llama.Context.Interfaces;
using Llama.Extensions;
using Microsoft.Extensions.Logging;

namespace ChatVectorizer
{
    internal class ChatVectorizer
    {
        private readonly ChatService _chatService;

        private readonly IContext _context;

        private readonly ILogger _logger;

        private readonly ChatVectorizerSettings _settings;

        private readonly UserDataService _userDataService;

        public ChatVectorizer(ILogger logger, IContext context, ChatService chatService, UserDataService userDataService, ChatVectorizerSettings settings)
        {
            this._context = context;
            this._chatService = chatService;
            this._userDataService = userDataService;
            this._settings = settings;
            this._logger = logger;
        }

        public async Task Execute()
        {
            List<EmbeddingsJob> jobs = new();

            foreach (ChatEntry ce in this._chatService.GetMissingEmbeddings())
            {
                jobs.Add(new EmbeddingsJob(ce.Id, ce.Content, this._context.Tokenize(ce.Content, "", true)));
            }

            jobs = jobs.OrderBy(j => j.Tokens.ToString()).ToList();

            while (jobs.Count > 0)
            {
                Console.WriteLine($"Jobs Remaining: {jobs.Count}");

                EmbeddingsJob job = jobs[0];
                jobs.RemoveAt(0);

                Console.WriteLine("\tSize: " + job.Tokens.Count);
                Console.WriteLine("\tContent: " + job.Content);

                this._context.Clear();

                LlamaTokenCollection toEval = new();

                toEval.Append(job.Tokens);

                this._context.Write(toEval);

                this._context.Evaluate();

                float[] embeddings = this._context.GetEmbeddings();

                this._chatService.SaveEmbeddings(job.ChatEntryId, embeddings);
            }
        }
    }
}