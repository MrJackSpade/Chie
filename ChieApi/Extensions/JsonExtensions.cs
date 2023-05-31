using System.Text.Json;
using System.Text.Json.Nodes;

namespace ChieApi.Extensions
{
	public static partial class JsonExtensions
	{
		public static TNode? CopyNode<TNode>(this TNode? node) where TNode : JsonNode => node?.Deserialize<TNode>();

		public static JsonNode? MoveNode(this JsonArray array, int id, JsonObject newParent, string name)
		{
			JsonNode? node = array[id];
			array.RemoveAt(id);
			return newParent[name] = node;
		}

		public static JsonNode? MoveNode(this JsonObject parent, string oldName, JsonObject newParent, string name)
		{
			parent.Remove(oldName, out JsonNode? node);
			return newParent[name] = node;
		}

		public static TNode ThrowOnNull<TNode>(this TNode? value) where TNode : JsonNode => value ?? throw new JsonException("Null JSON value");
	}
}