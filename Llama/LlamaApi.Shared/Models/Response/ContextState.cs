namespace LlamaApi.Shared.Models.Response
{
    public class ContextState
    {
        public uint AvailableBuffer { get; set; }

        public Guid Id { get; set; }

        public bool IsLoaded { get; set; }

        public uint Size { get; set; }
    }
}