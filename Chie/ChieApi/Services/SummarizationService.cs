using Summary.Interfaces;
using Summary.Models;
using System.Diagnostics;
using System.Text;

namespace ChieApi.Services
{
    public partial class SummarizationService
    {
        private const string DIR_SUMMARIZATION = "SummarizationData";

        private const int MAX_IN_TOKENS = 250;

        private readonly ISummaryApiClient _summaryApiClient;

        private readonly Dictionary<string, int> _cachedTokenCount = new();

        public SummarizationService(ISummaryApiClient summaryApiClient)
        {
            if (!Directory.Exists(DIR_SUMMARIZATION))
            {
                Directory.CreateDirectory(DIR_SUMMARIZATION);
            }

            this._summaryApiClient = summaryApiClient;
        }

        public async Task<int> GetTokenCount(string message)
        {
            if(!_cachedTokenCount.TryGetValue(message, out int count))
            {
                TokenizeResponse response = await this._summaryApiClient.Tokenize(message + "\n");

                count = response.Content.Length;

                _cachedTokenCount.Add(message, count);
            }

            return count;
        }

        public async Task<SummaryResponse> Summarize(long firstId, int maxOutTokens, IEnumerable<string> messagesReversed)
        {
            int tokenCount = 0;

            StringBuilder toSummarize = new();

            HashSet<string> distinctResponses = new();

            //Messages go back in time so we're going to keep track of which
            //ones weve tested so we know what we need to keep in the cache for the next run.
            //Since the window will always move forward, there shouldn't be a scenario where 
            //a message isn't checked on one run, and IS checked on the next run
            HashSet<string> checkedMessages = new();

            IEnumerator<string> messages = messagesReversed.GetEnumerator();

            //If no available messages we exit
            if (!messages.MoveNext())
            {
                return new SummaryResponse()
                {
                    FirstId = firstId,
                    Summary = string.Empty
                };
            }

            bool completeSummarization = false;

            string summaryResponse = string.Empty;

            //Outer loop to track response length;
            do
            {
                List<string> toAppend = new();

                //inner loop to ensure request is smaller than
                //summarization max
                do
                {
                    string message = messages.Current;

                    Debug.WriteLine("Requesting Tokens: " + message);

                    tokenCount += await this.GetTokenCount(message);

                    Debug.WriteLine("Token Count: " + tokenCount);

                    if (tokenCount >= MAX_IN_TOKENS && toAppend.Count > 0)
                    {
                        break;
                    }

                    toAppend.Add(message);
                    checkedMessages.Add(message);

                    //If no available messages we exit
                    if (!messages.MoveNext())
                    {
                        completeSummarization = true;
                        break;
                    }
                } while (true);

                toAppend.Reverse();

                foreach (string message in toAppend)
                {
                    toSummarize.AppendLine(message);
                }

                //Append new block to beginning since order is reversed
                string summarized = (await this._summaryApiClient.Summarize(toSummarize.ToString())).Content;

                if (distinctResponses.Add(summarized))
                {
                    summaryResponse = summarized + " " + summaryResponse;

                    //This should be done with the LlamaApi but this is a cheap hack that will mostly work for now
                    int summaryResponseTokens = (await this._summaryApiClient.Tokenize(summaryResponse)).Content.Length;

                    //Once we've exceeded the max response tokens we return
                    if (summaryResponseTokens >= maxOutTokens)
                    {
                        completeSummarization = true;
                    }
                }

                tokenCount = 0;
                toSummarize.Clear();
                toAppend.Clear();
            } while (!completeSummarization);

            //Before we exist we need to check all the cached messages to make sure 
            //we actually used them, and if not, remove them from the cache
            foreach(string key in _cachedTokenCount.Keys.ToList())
            {
                if(!checkedMessages.Contains(key))
                {
                    _cachedTokenCount.Remove(key);
                }
            }

            SummaryResponse summarization = new()
            {
                FirstId = firstId,
                Summary = summaryResponse
            };

            return summarization;
        }
    }

    public class SummaryResponse
    {
        public long FirstId { get; set; }

        public string Summary { get; set; }
    }
}