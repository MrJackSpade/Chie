namespace LlamaApi.Shared.Models.Response
{
    public class EvaluationResponse
    {
        public uint AvailableBuffer { get; set; }

        public uint Evaluated { get; set; }

        public Guid Id { get; set; }

        public bool IsLoaded { get; set; }
    }
}