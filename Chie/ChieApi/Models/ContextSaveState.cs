using ChieApi.Extensions;
using ChieApi.Interfaces;
using System.Text.Json;

namespace ChieApi.Models
{
    public enum TokenBlockType
    {
        Message,

        Block
    }

    public class ContextSaveState
    {
        public ContextSaveState()
        { }

        public List<LlamaTokenState> AssistantBlock { get; set; } = new();

        public List<LlamaTokenState> InstructionBlock { get; set; } = new();

        public List<TokenBlockState> MessageStates { get; set; } = new();

        public List<LlamaTokenState> Summary { get; set; } = new();

        public async Task LoadFrom(LlamaContextModel model)
        {
            if (model.InstructionBlock is not null)
            {
                this.InstructionBlock = await model.InstructionBlock.ToStateList();
            }

            if (model.AssistantBlock is not null)
            {
                this.AssistantBlock = await model.AssistantBlock.ToStateList();
            }

            if (model.Summary is not null)
            {
                this.Summary = await model.Summary.ToStateList();
            }

            foreach (ITokenCollection collection in model.Messages)
            {
                TokenBlockState blockState = new()
                {
                    Type = collection.Type,
                    Id = collection.Id
                };

                if (collection is LlamaMessage lm)
                {
                    blockState.Header = await lm.Header.ToStateList();
                    blockState.Content = await lm.Content.ToStateList();
					blockState.MessageSuffix = await lm.EndOfText.ToStateList();
					blockState.TokenBlockType = TokenBlockType.Message;
                }
                else if (collection is LlamaTokenBlock bl)
                {
                    blockState.Content = await bl.Content.ToStateList();
                    blockState.TokenBlockType = TokenBlockType.Block;
                }
                else
                {
                    throw new NotImplementedException();
                }

                this.MessageStates.Add(blockState);
            }
        }

        public void LoadFrom(string path)
        {
            string content = File.ReadAllText(path);

            ContextSaveState state = JsonSerializer.Deserialize<ContextSaveState>(content);

            this.InstructionBlock = state.InstructionBlock;
            this.AssistantBlock = state.AssistantBlock;
            this.Summary = state.Summary;
            this.MessageStates = state.MessageStates;
        }

        public void SaveTo(string path)
        {
            string content = JsonSerializer.Serialize(this, new JsonSerializerOptions()
            {
                WriteIndented = true,
            });

            File.WriteAllText(path, content);
        }

        public LlamaContextModel ToModel(LlamaTokenCache cache)
        {
            LlamaContextModel toReturn = new(cache);

            if (this.InstructionBlock != null)
            {
                toReturn.InstructionBlock = new LlamaTokenBlock(this.InstructionBlock.ToCollection(), LlamaTokenType.Undefined);
            }

            if (this.AssistantBlock != null)
            {
                toReturn.AssistantBlock = new LlamaTokenBlock(this.AssistantBlock.ToCollection(), LlamaTokenType.Undefined);
            }

            if (this.Summary != null)
            {
                toReturn.Summary = new LlamaTokenBlock(this.Summary.ToCollection(), LlamaTokenType.Undefined);
            }

            foreach (TokenBlockState message in this.MessageStates)
            {
                switch (message.TokenBlockType)
                {
                    case TokenBlockType.Message:
                        toReturn.Messages.Add(new LlamaMessage(
                            message.Header.ToCollection(),
                            message.Content.ToCollection(),
                            message.MessageSuffix.ToCollection(),
                            message.Type,
                            cache)
                        { Id = message.Id });
                        break;

                    case TokenBlockType.Block:
                        toReturn.Messages.Add(new LlamaTokenBlock(
                            message.Content.ToCollection(),
                            message.Type)
                        {
                            Id = message.Id,
                        });
                        break;

                    default:
                        throw new NotImplementedException();
                }
            }

            return toReturn;
        }
    }

    public class LlamaTokenState
    {
        public int Id { get; set; }

        public string? Value { get; set; }
    }

    public class TokenBlockState
    {
        public List<LlamaTokenState> Content { get; set; } = new();

		public List<LlamaTokenState> MessagePrefix { get; set; } = new();

		public List<LlamaTokenState> MessageSuffix { get; set; } = new();

		public long Id { get; set; }

        public TokenBlockType TokenBlockType { get; set; }

        public LlamaTokenType Type { get; set; }

        public List<LlamaTokenState> Header { get; set; } = new();
    }
}