namespace Ai.Utils
{
    public static class LoopUtil
    {
        public static async Task Forever()
        {
            do
            {
                await Task.Delay(30_000);
            } while (true);
        }

        public static async Task Loop(Func<Task> toInvoke, int delayMs, Func<Exception, Task>? onError)
        {
            do
            {
                try
                {
                    await toInvoke();
                }
                catch (Exception ex) when (onError != null)
                {
                    await onError.Invoke(ex);
                }

                await Task.Delay(delayMs);
            } while (true);
        }
    }
}