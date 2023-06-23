namespace ChieApi
{
    /// <summary>
    /// Minimalistic lifetime that does nothing.
    /// </summary>
    internal sealed class NullLifetime : IHostLifetime
    {
        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public Task WaitForStartAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}