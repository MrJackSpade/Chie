namespace Llama.Data.Scheduler
{
    public class ExecutionResult<TResult>
    {
        public TResult Value { get; set; }
        public Exception Exception { get; set; }
        public bool IsSuccess => this.Exception is null;
    }
}
