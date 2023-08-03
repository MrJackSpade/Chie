using Summary.Interfaces;
using Summary.Models;
using System.Text;

namespace ChieApi.Services
{
    public class SummaryResponse
    {
        public string Summary { get; set; }

        public long FirstId { get; set; }
    }
    public partial class SummarizationService
    {
        private const string DIR_SUMMARIZATION = "SummarizationData";

        private const int MAX_TOKENS = 4064;

        private readonly ISummaryApiClient _summaryApiClient;
        public SummarizationService(ISummaryApiClient summaryApiClient)
        {
            if (!Directory.Exists(DIR_SUMMARIZATION))
            {
                Directory.CreateDirectory(DIR_SUMMARIZATION);
            }

            this._summaryApiClient = summaryApiClient;
        }

        public async Task<SummaryResponse> Summarize(string previousSummary, long firstId, IEnumerable<string> messagesReversed)
        {
            int tokenCount = 0;

            StringBuilder toSummarize = new();

            if (!string.IsNullOrWhiteSpace(previousSummary))
            {
                TokenizeResponse previousResponse = await this._summaryApiClient.Tokenize(previousSummary + "\n");

                tokenCount += previousResponse.Content.Length;

                toSummarize.AppendLine(previousSummary);
            }

            IEnumerator<string> messages = messagesReversed.GetEnumerator();

            do
            {
                if (!messages.MoveNext())
                {
                    break;
                }

                string message = messages.Current;

                TokenizeResponse response = await this._summaryApiClient.Tokenize(message + "\n");

                tokenCount += response.Content.Length;

                if (tokenCount == MAX_TOKENS)
                {
                    break;
                }

                toSummarize.AppendLine(message);

            } while (true);

            string summaryResponse = (await this._summaryApiClient.Summarize(toSummarize.ToString())).Content;

            SummaryResponse summarization = new()
            {
                FirstId = firstId,
                Summary = summaryResponse
            };

            return summarization;
        }
    }
}