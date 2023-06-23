using ChromaDbClient.Models;
using System.Text;

namespace ChromaDbClient.Exceptions
{
	public class ChromaDbValidationException : ChromaDbException
	{
		public IReadOnlyList<ValidationError> Errors { get; private set; }
		public ChromaDbValidationException(List<ValidationError> errors) : base(GetString(errors))
		{
			this.Errors = errors;
		}

		public static string GetString(List<ValidationError> errors)
		{
			StringBuilder stringBuilder = new();

			foreach (ValidationError item in errors)
			{
				stringBuilder.AppendLine($"{item.Type}@{item.Location}: {item.Message}");
			}

			return stringBuilder.ToString();
		}
	}
}
