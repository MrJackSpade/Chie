using Loxifi;
using System.Text;

namespace ImageRecognition
{
    public class EmbeddingClient
    {
        private const string MASTER_FILE = "Master.txt";

        private const string TEMP_DIRECTORY = "Temp";

        private const string LOG_DIRECTORY = "Logs";

        private readonly EmbeddingClientSettings _settings;

        public EmbeddingClient(EmbeddingClientSettings settings)
        {
            if(!Directory.Exists(LOG_DIRECTORY))
            {
                Directory.CreateDirectory(LOG_DIRECTORY);
            }

			if (!Directory.Exists(TEMP_DIRECTORY))
			{
				Directory.CreateDirectory(TEMP_DIRECTORY);
			}

			this._settings = settings;
        }

        private static void WriteString(string source, string toWrite)
        {
            if (!string.IsNullOrWhiteSpace(toWrite))
            {
                File.WriteAllText($"{DateTime.Now.Ticks}_{source}.log", toWrite);
            }
        }

		public async Task<float[][]> Generate(string[] data)
        {
            foreach (string file in Directory.EnumerateFiles(TEMP_DIRECTORY))
            {
                File.Delete(file);
            }

            string master = Path.Combine(TEMP_DIRECTORY, MASTER_FILE);

            int i = 0;

            List<string> masterLines = new();
            foreach (string embedding in data)
            {
                string embeddingFile = Path.Combine(Directory.GetCurrentDirectory(), TEMP_DIRECTORY, $"{i++}");
                File.WriteAllText(embeddingFile, embedding);
                masterLines.Add(embeddingFile);
            }

            File.WriteAllLines(master, masterLines);

            StringBuilder resultBuilder = new();
            StringBuilder errorBuilder = new();

            int tries = 0;

            do
            {
                try
                {
                    ProcessSettings settings = new(this._settings.PythonPath)
                    {
                        Arguments = $"{this._settings.EmbeddingPath} {this._settings.ModelName} {Path.Combine(Directory.GetCurrentDirectory(), master)}",
                        StdOutWrite = (s, e) => resultBuilder.Append(e),
                        StdErrWrite = (s, e) => errorBuilder.Append(e),
                        WorkingDirectory = new FileInfo(this._settings.EmbeddingPath).DirectoryName
                    };

                    uint r = await ProcessRunner.StartAsync(settings);

                    string result = resultBuilder.ToString();
                    string error = errorBuilder.ToString(); 

                    WriteString("STDOUT", result);
                    WriteString("STDERR", errorBuilder.ToString());

                    List<float[]> toReturn = new();
                    for (int e = 0; e < data.Length; e++)
                    {
                        string embeddingFile = Path.Combine(Directory.GetCurrentDirectory(), TEMP_DIRECTORY, $"{e}.embedding");
                        toReturn.Add(this.ReadEmbeddings(embeddingFile).ToArray());
                    }

                    foreach (float[] embeddings in toReturn)
                    {
                        if (embeddings.Length == 0)
                        {
                            throw new Exception(error);
                        }
                    }

                    return toReturn.ToArray();
                }
                catch (Exception ex) when (tries++ < 3)
                {
                    resultBuilder.Clear();
                }
            } while (true);
        }

        public IEnumerable<float> ReadEmbeddings(string filename)
        {
            string content = File.ReadAllText(filename);
            content = content.Trim('[').Trim(']');
            foreach (string c in content.Split(' '))
            {
                if (string.IsNullOrWhiteSpace(c))
                {
                    continue;
                }

                yield return float.Parse(c);
            }
        }
    }
}