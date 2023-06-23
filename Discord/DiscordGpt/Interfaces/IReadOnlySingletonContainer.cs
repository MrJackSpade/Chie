namespace DiscordGpt.Interfaces
{
    public interface IReadOnlySingletonContainer<T>
    {
        T? Value { get; }
    }
}