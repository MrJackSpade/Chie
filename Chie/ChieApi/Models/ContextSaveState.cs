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

        public List<LlamaTokenState> Instruct { get; set; } = new();

        public List<TokenBlockState> MessageStates { get; set; } = new();

        public List<LlamaTokenState> Summary { get; set; } = new();

        public async Task LoadFrom(LlamaContextModel model)
        {
            if (model.Instruction is not null)
            {
                this.Instruct = await model.Instruction.ToStateList();
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
                    blockState.UserName = await lm.UserName.ToStateList();
                    blockState.Content = await lm.Content.ToStateList();
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

            this.Instruct = state.Instruct;
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

            if (this.Instruct != null)
            {
                toReturn.Instruction = new LlamaTokenBlock(this.Instruct.ToCollection(), LlamaTokenType.Undefined);
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
                            message.UserName.ToCollection(),
                            message.Content.ToCollection(),
                            message.Type,
                            cache));
                        break;

                    case TokenBlockType.Block:
                        toReturn.Messages.Add(new LlamaTokenBlock(
                            message.Content.ToCollection(),
                            message.Type));
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

        public long Id { get; set; }

        public TokenBlockType TokenBlockType { get; set; }

        public LlamaTokenType Type { get; set; }

        public List<LlamaTokenState> UserName { get; set; } = new();
    }
}