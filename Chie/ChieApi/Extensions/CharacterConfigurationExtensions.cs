namespace ChieApi.Extensions
{
    public static class CharacterConfigurationExtensions
    {
        public static string GetHeaderForUser(this CharacterConfiguration characterConfiguration, string user)
        {
            return $"{characterConfiguration.StartHeaderToken}{user}{characterConfiguration.EndHeaderToken}";
        }

        public static string GetHeaderForBot(this CharacterConfiguration characterConfiguration)
        {
            return $"{characterConfiguration.StartHeaderToken}{characterConfiguration.CharacterName}{characterConfiguration.EndHeaderToken}";
        }
    }
}
