using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChieApi.Shared.Entities
{
    public class ChatEntryEmbedding
    {
        public long ChatEntryId { get; set; }
        public byte[] Data { get; set; } = Array.Empty<byte>();
    }
}
