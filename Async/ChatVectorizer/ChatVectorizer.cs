using Ai.Utils.Extensions;
using ChieApi.Shared.Entities;
using ChieApi.Shared.Models;
using ChieApi.Shared.Services;
using Embedding;
using Embedding.Models;
using Microsoft.Extensions.Logging;
using System.Text;

namespace ChatVectorizer
{
    internal class ChatVectorizer
    {
        private readonly ChatService _chatService;

        private readonly EmbeddingApiClient _embeddingApiClient;

        private readonly ILogger _logger;

        private readonly ModelService _modelService;

        private readonly ChatVectorizerSettings _settings;

        public ChatVectorizer(EmbeddingApiClient embeddingApiClient, ILogger logger, ModelService modelService, ChatService chatService, ChatVectorizerSettings settings)
        {
            this._embeddingApiClient = embeddingApiClient;
            this._modelService = modelService;
            this._chatService = chatService;
            this._settings = settings;
            this._logger = logger;
        }

        public static string StripNonASCII(string input)
        {
            StringBuilder sb = new();
            foreach (char c in input)
            {
                if (c is >= (char)0 and <= (char)127) // ASCII range
                {
                    sb.Append(c);
                }
            }

            return sb.ToString();
        }

        public async Task Execute()
        {
            List<EmbeddingsJob> jobs = new();

            Model m = this._modelService.GetModel(this._settings.DefaultModel);

            foreach (ChatEntry ce in this._chatService.GetMissingEmbeddings(m))
            {
                jobs.Add(new EmbeddingsJob(ce.Id, ce.Content));
            }

            while (jobs.Count > 0)
            {
                List<EmbeddingsJob> tryTook = jobs.TryTake(250);

                Console.WriteLine($"Jobs Remaining: {jobs.Count}");

                string[] toVectorize = tryTook.Select(j => StripNonASCII(j.Content)).ToArray();

                EmbeddingResponse result = await this._embeddingApiClient.Generate(toVectorize);

                Console.WriteLine($"Writing to database...");

                Parallel.For(0, toVectorize.Length, i =>
                {
                    EmbeddingsJob job = tryTook[i];

                    float[] embeddings = result.Content[i];

                    this._chatService.SaveEmbeddings(m, job.ChatEntryId, embeddings);
                });
            }
        }
    }
}