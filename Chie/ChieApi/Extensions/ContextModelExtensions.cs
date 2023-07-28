using ChieApi.Interfaces;
using ChieApi.Models;

namespace ChieApi.Extensions
{
    public static class ContextModelExtensions
    {
        public static void RemoveTemporary(this LlamaContextModel model)
        {
            foreach (ITokenCollection tokenCollection in model.Messages.ToList())
            {
                if (tokenCollection.Type == LlamaTokenType.Temporary)
                {
                    model.Messages.Remove(tokenCollection);
                }
            }
        }
    }
}