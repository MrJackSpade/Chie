using Llama.Simple;

namespace TestLlamaSimple
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            SimpleInferer inferrer = new();

            await inferrer.LoadModel(new Llama.Data.LlamaModelSettings()
            {
                Model = "D:\\Chie\\Models\\QWEN-72b-Chat-Q5_K_M.gguf"
            });

            await inferrer.LoadContext(new Llama.Data.LlamaContextSettings());
            
            inferrer.AddStop(2);

            Console.Clear();

            do
            {
                Console.Write("### Human: ");
                inferrer.Write("### Human: ");

                string instruction = Console.ReadLine();

                inferrer.Write(instruction);
                inferrer.Write(System.Environment.NewLine);

                Console.Write("### Assistant:");
                inferrer.Write("### Assistant:");

                await foreach (var token in inferrer.Predict())
                {
                    Console.Write(token.Value);
                }
            } while (true);
        }
    }
}