using System.Runtime.Serialization;

namespace Llama.Data.Scheduler.Exceptions
{
    public class AsynchronousExecutionException : Exception
    {
        public AsynchronousExecutionException()
        {
        }

        public AsynchronousExecutionException(string? message) : base(message)
        {
        }

        public AsynchronousExecutionException(string? message, Exception? innerException) : base(message, innerException)
        {
        }

        protected AsynchronousExecutionException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}