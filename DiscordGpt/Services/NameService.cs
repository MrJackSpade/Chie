using Ai.Utils.Extensions;

namespace DiscordGpt.Services
{
	public class NameService
	{
		public int CalculateNamePriority(string name)
		{
			bool integers = false;
			bool special = false;

			foreach (char c in name)
			{
				if (char.IsDigit(c))
				{
					integers = true;
				}
				else if (!char.IsLetter(c))
				{
					special = true;
				}
			}

			if (special)
			{
				return 0;
			}

			if (integers)
			{
				return 1;
			}

			return 2;
		}

		public string CleanUserName(string toClean)
		{
			string[] parts = toClean.CleanSplit(' ').ToArray();

			string[] orderedParts = parts.OrderByDescending(this.CalculateNamePriority).ThenByDescending(s => s.Length).ToArray();

			return orderedParts.First();
		}
	}
}