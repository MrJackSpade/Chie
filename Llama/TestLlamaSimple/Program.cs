using Llama.Simple;

namespace TestLlamaSimple
{
    internal class Program
    {
        const string PROMPT = """
            ### Human: Write me a sexy limerick
            ### Assistant:
            """;
        static async Task Main(string[] args)
        {
            SimpleInferer inferrer = new();

            await inferrer.LoadModel(new Llama.Data.LlamaModelSettings()
            {
                Model = "D:\\Chie\\Models\\sus-chat-34b.Q5_K_M.gguf"
            });

            await inferrer.LoadContext(new Llama.Data.LlamaContextSettings(), PROMPT);

            inferrer.AddStop("### Human:");
            inferrer.AddStop(2);

            await foreach(var token in inferrer.Predict())
            {
                Console.Write(token.Value);
            }
        }
    }
}