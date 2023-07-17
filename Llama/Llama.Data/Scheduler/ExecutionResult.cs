namespace Llama.Data.Scheduler
{
    public class ExecutionResult<TResult>
    {
        public Exception Exception { get; set; }

        public bool IsSuccess => this.Exception is null;

        public TResult Value { get; set; }
    }
}