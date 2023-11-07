namespace LlamaApi.Shared.Models
{
    public enum LlamaStatusCodes
    {
        Success = 200,

        NotReady = 503,

        NoModelLoaded = 441,

        NoContextLoaded = 442
    }
}