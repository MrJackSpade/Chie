namespace LlamaApi.Models
{
    public class JobFailure
    {
        public JobFailure(Exception ex)
        {
            this.Message = ex.Message;
            this.StackTrace = ex.StackTrace;

            if (ex.InnerException != null)
            {
                this.InnerFailure = new JobFailure(ex.InnerException);
            }
        }

        public JobFailure InnerFailure { get; set; }

        public string Message { get; set; }

        public string StackTrace { get; set; }
    }
}