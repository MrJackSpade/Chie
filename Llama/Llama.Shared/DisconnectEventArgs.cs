namespace Llama.Shared
{
    public class DisconnectEventArgs
    {
        public uint ResultCode { get; set; }

        public string RollOverPrompt { get; set; }
    }
}