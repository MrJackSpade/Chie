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
        private readonly LogService _databaseService;

        private readonly LlamaService _llamaService;

        private readonly List<IRequestPipeline> _pipelines;

        public ChieController(IEnumerable<IRequestPipeline> pipelines, LlamaService llamaService, LogService databaseService)
        {
            this._databaseService = databaseService;
            this._llamaService = llamaService;
            this._pipelines = pipelines.ToList();
        }

        [HttpGet("ContinueRequest/{channelId}")]
        public Task<ContinueRequestResponse> ContinueRequest(string channelId)
        {
            return Task.FromResult(new ContinueRequestResponse()
            {
                Success = this._llamaService.ReturnControl(false)
            });
        }

        [HttpGet("GetLogsByDate")]
        public Task<List<LogEntry>> GetLogsByDate(string after) => Task.FromResult(this._databaseService.GetLogs(DateTime.Parse(after)));

        [HttpGet("GetLogsById")]
        public Task<List<LogEntry>> GetLogsById(long after) => Task.FromResult(this._databaseService.GetLogs(after));

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

            List<ChatEntry> processedEntries = chatEntries.ToList();

            foreach (IRequestPipeline requestPipeline in this._pipelines)
            {
                processedEntries = await requestPipeline.Process(processedEntries);
            }

            return new MessageSendResponse()
            {
                MessageId = await this._llamaService.Send(processedEntries.ToArray())
            };
        }
    }
}