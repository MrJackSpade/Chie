using ChieApi.Interfaces;

namespace ChieApi.Factories
{
	public class SecretCharacterNameFactory : ICharacterNameFactory
	{
		private readonly ChieApiSettings _settings;

		public SecretCharacterNameFactory(ChieApiSettings settings)
		{
			this._settings = settings;
		}

		public string GetName() => _settings.DefaultModel;
	}
}