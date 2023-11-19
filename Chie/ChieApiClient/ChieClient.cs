using ChieApi.Client.Extensions;
using ChieApi.Shared.Entities;
using ChieApi.Shared.Interfaces;
using ChieApi.Shared.Models;

namespace ChieApi.Client
{
    public class ChieClient : IChieClient
    {
        private const int PORT = 5000;

        private readonly HttpClient _client;

        public ChieClient()
        {
            this._client = new HttpClient();
        }

        public async Task<ContinueRequestResponse> ContinueRequest(string channelName) => await this._client.GetJsonAsync<ContinueRequestResponse>($"http://127.0.0.1:{PORT}/Chie/ContinueRequest/{channelName}");

        public async Task<ChatEntry> GetReply(long originalMessageId)
        {
            do
            {
                ChatEntry response = await this._client.GetJsonAsync<ChatEntry>($"http://127.0.0.1:{PORT}/Chie/GetReply?id={originalMessageId}");

                if (response.Id != 0)
                {
                    return response;
                }

                await Task.Delay(2000);
            } while (true);
        }

        public async Task<ChatEntry[]> GetResponses(string channelId, long after) => await this._client.GetJsonAsync<ChatEntry[]>($"http://127.0.0.1:{PORT}/Chie/GetResponses?channelId={channelId}&after={after}");

        public async Task<IsTypingResponse> IsTyping(string channelName) => await this._client.GetJsonAsync<IsTypingResponse>($"http://127.0.0.1:{PORT}/Chie/IsTyping/{channelName}");

        public async Task<MessageSendResponse> Send(ChatEntry chatEntry) => await this.Send(new ChatEntry[] { chatEntry });

        public async Task<MessageSendResponse> Send(ChatEntry[] chatEntry) => await this._client.PostJsonAsync<MessageSendResponse>($"http://127.0.0.1:{PORT}/Chie/Send", chatEntry);

        public async Task<StartVisibleResponse> StartVisible() => await this._client.GetJsonAsync<StartVisibleResponse>($"http://127.0.0.1:{PORT}/Chie/StartVisible");
    }
}