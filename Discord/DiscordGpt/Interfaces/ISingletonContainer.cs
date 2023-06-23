namespace DiscordGpt.Interfaces
{
    public interface ISingletonContainer<T> : IReadOnlySingletonContainer<T>
    {
        void Clear();

        public void SetValue(T value);
    }
}