namespace Llama.Data.Scheduler
{
    public enum ExecutionPriority : byte
    {
        Immediate = 0,

        High = 10,

        Medium = 20,

        Low = 30,

        Background = 100
    }
}