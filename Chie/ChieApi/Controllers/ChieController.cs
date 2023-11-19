using ChieApi.Extensions;
using ChieApi.Interfaces;
using ChieApi.Models;
using ChieApi.Services;
using ChieApi.Shared.Entities;
using ChieApi.Shared.Interfaces;
using ChieApi.Shared.Models;
using Microsoft.AspNetCore.Mvc;

namespace ChieApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ChieController : ControllerBase, IChieClient
    {
        private readonly CharacterConfiguration _characterConfiguration;

        private readonly LlamaService _llamaService;

        private readonly ILogger _logger;

        private readonly List<IRequestPipeline> _pipelines;

        public ChieController(IEnumerable<IRequestPipeline> pipelines, CharacterConfiguration characterConfiguration, LlamaService llamaService, ILogger logger)
        {
            this._logger = logger;
            this._llamaService = llamaService;
            this._pipelines = pipelines.ToList();
            this._characterConfiguration = characterConfiguration;
        }

        [HttpGet("ContinueRequest/{channelId}")]
        public Task<ContinueRequestResponse> ContinueRequest(string channelId)
        {
            return Task.FromResult(new ContinueRequestResponse()
            {
                MessageId = this._llamaService.ReturnControl(false, true, channelId)
            });
        }

        [HttpGet("GetReply")]
        public Task<ChatEntry?> GetReply(long id)
        {
            if (this._llamaService.TryGetReply(id, out ChatEntry? ce))
            {
                return Task.FromResult(ce);
            }
            else
            {
                return Task.FromResult<ChatEntry?>(new ChatEntry() { });
            }
        }

        [HttpGet("GetResponses")]
        public Task<ChatEntry[]> GetResponses(string channelId, long after) => Task.FromResult(this._llamaService.GetResponses(channelId, after));

        [HttpGet("IsTyping/{channel}")]
        public Task<IsTypingResponse> IsTyping(string channel)
        {
            LlamaClientResponseState clientResponse = this._llamaService.CheckIfResponding(channel);

            return Task.FromResult(new IsTypingResponse()
            {
                IsTyping = clientResponse.IsTyping,
                Content = clientResponse.Content,
            });
        }

        [HttpPost("Send")]
        public async Task<MessageSendResponse> Send(ChatEntry[] chatEntries)
        {
            await this._llamaService.Initialization;

            List<ChatEntry> processedEntries = await this._pipelines.Process(chatEntries);

            if (processedEntries.Count == 0)
            {
                return new MessageSendResponse()
                {
                    MessageId = 0
                };
            }

            return new MessageSendResponse()
            {
                MessageId = await this._llamaService.Send(processedEntries.ToArray())
            };
        }

        [HttpGet("StartVisible")]
        public Task<StartVisibleResponse> StartVisible()
        {
            return Task.FromResult(new StartVisibleResponse()
            {
                StartVisible = this._characterConfiguration.StartVisible
            });
        }
    }
}