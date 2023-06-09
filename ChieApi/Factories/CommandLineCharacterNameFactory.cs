using ChieApi.Interfaces;

namespace ChieApi.Factories
{
	public class CommandLineCharacterNameFactory : ICharacterNameFactory
	{
		private readonly string _name;

		public CommandLineCharacterNameFactory(string arg)
		{
			this._name = arg;
		}

		public string GetName() => this._name;
	}
}