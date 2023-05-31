using ChieApi.Interfaces;

namespace ChieApi.Factories
{
	public class CommandLineCharacterNameFactory : ICharacterNameFactory
	{
		private readonly string _name;

		public CommandLineCharacterNameFactory(string arg)
		{
			_name = arg;
		}

		public string GetName() => _name;
	}
}