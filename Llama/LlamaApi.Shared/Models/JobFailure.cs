namespace LlamaApi.Models
{
    public class JobFailure
    {
        public JobFailure(Exception ex)
        {
            this.Message = ex.Message;
            this.StackTrace = ex.StackTrace;
        }

        public string Message { get; set; }
        public string StackTrace { get; set; }
    }
}
