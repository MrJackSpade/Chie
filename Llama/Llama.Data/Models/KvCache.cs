namespace Llama.Data.Models
{
    public class KvCache
    {
        public bool HasShift { get; set; }

        public uint Head { get; set; }

        public uint N { get; set; }

        public uint Size { get; set; }
    }
}