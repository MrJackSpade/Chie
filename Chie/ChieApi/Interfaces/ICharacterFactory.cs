namespace ChieApi.Interfaces
{
    public interface ICharacterFactory
    {
        Task<CharacterConfiguration> Build();
    }
}