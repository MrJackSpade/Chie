using Accord.MachineLearning;
using ChieApi.Shared.Entities;
using ChieApi.Shared.Services;
using Llama.Collections;
using Llama.Context;
using Llama.Context.Extensions;
using Llama.Data;
using Llama.Extensions;
using Microsoft.Extensions.Logging;
using System.Data.SqlClient;
using System.Text;

namespace UserSummarizer
{
    public class UserSummarizer
    {
        private readonly ChatService _chatService;

        private readonly ILogger _logger;

        private readonly UserSummarizerSettings _settings;

        private readonly UserDataService _userDataService;

        private ContextEvaluator _evaluator;

        public UserSummarizer(ILogger logger, ChatService chatService, UserDataService userDataService, UserSummarizerSettings settings)
        {
            this._chatService = chatService;
            this._userDataService = userDataService;
            this._settings = settings;
            this._logger = logger;
        }

        public static void LerpEmbeddings(List<Embedding> chatEmbeddings)
        {
            DateTime minDate = chatEmbeddings.Min(c => c.DateCreated);
            DateTime currentDate = DateTime.Now;

            // Calculate the mean embedding.
            int embeddingLength = chatEmbeddings[0].Data.Length;
            double[] meanEmbedding = new double[embeddingLength];
            foreach (Embedding embedding in chatEmbeddings)
            {
                for (int i = 0; i < embeddingLength; i++)
                {
                    meanEmbedding[i] += embedding.Data[i];
                }
            }

            for (int i = 0; i < embeddingLength; i++)
            {
                meanEmbedding[i] /= chatEmbeddings.Count;
            }

            // Perform Lerp for each embedding.
            foreach (Embedding embedding in chatEmbeddings)
            {
                double t = (embedding.DateCreated - minDate).TotalSeconds / (currentDate - minDate).TotalSeconds;
                for (int i = 0; i < embeddingLength; i++)
                {
                    embedding.Data[i] = HermiteLerp(embedding.Data[i], meanEmbedding[i], 1 - t);
                }
            }
        }

        // Lerp function.
        public static double Lerp(double a, double b, double t) => (double)((1 - t) * a + t * b);

        public static double HermiteLerp(double a, double b, double t)
        {
            // Apply ease in-out function.
            t = t * t * (3 - 2 * t);
            return (double)((1 - t) * a + t * b);
        }

        public static void FreeMemory()
        {
            // Force an immediate garbage collection of all generations
            GC.Collect();

            // Suspend the current thread until the garbage collector 
            // has finished its work (which includes running finalizers)
            GC.WaitForPendingFinalizers();

            // It is a good practice to call GC.Collect() again after 
            // GC.WaitForPendingFinalizers() to clean up any objects 
            // that were in the process of being finalized
            GC.Collect();
        }

        public async Task Execute()
        {
            Dictionary<UserData, List<string>> userMessages = new();

            foreach (string userId in this._chatService.GetUserIds())
            {
                FreeMemory();

                Console.WriteLine(userId);

                ChatEntry lastMessage = this._chatService.GetLastMessage(userId);

                UserData userData = await this._userDataService.GetOrCreate(userId);

                if (userData.IsBot)
                {
                    Console.WriteLine($"\tSkipping.");
                    continue;
                }

                if (lastMessage is null || lastMessage.Id < userData.LastChatId + 10)
                {
                    Console.WriteLine($"\tSkipping.");
                    continue;
                }

                Console.WriteLine($"\tCalculating...");

                List<string> messages = this.GetMessages(userId);

                userMessages.Add(userData, messages);
            }

            FreeMemory();

            Console.WriteLine("Loading Model...");

            this._evaluator = new ContextEvaluatorBuilder(this._settings).BuildUserSummaryEvaluator();

            foreach (KeyValuePair<UserData, List<string>> kvp in userMessages)
            {
                UserData userData = kvp.Key;

                List<string> messages = kvp.Value;

                ChatEntry lastMessage = this._chatService.GetLastMessage(userData.UserId);

                this._evaluator.Context.Clear();

                string displayName = this.GetDisplayName(userData);

                LlamaTokenCollection summarizeSuffix = this._evaluator.Tokenize($"\nHuman: Please describe {displayName}\n\nAssistant:");

                this._logger.LogInformation("Summarizing User: " + userData.UserId);

                LlamaTokenCollection llamaTokens = new();

                LlamaTokenCollection summaryTokens = this.TryGetTokens(userData.UserSummary);

                llamaTokens.Append(LlamaToken.Bos);
                llamaTokens.Append(summaryTokens);

                if (messages.Count > 10)
                {
                    foreach (string message in messages)
                    {
                        string content = message.Replace("\r", " ").Replace("\n", " ");

                        while (content.Contains("  "))
                        {
                            content = content.Replace("  ", " ");
                        }

                        content = content.Trim();

                        llamaTokens.Append(this._evaluator.Context.Tokenize($"{displayName}: {content}", "", false));
                        llamaTokens.Append(new LlamaToken(13, IntPtr.Zero, ""));
                    }

                    llamaTokens.Append(summarizeSuffix);

                    LlamaTokenCollection result = new();

                    foreach (LlamaToken resultToken in this._evaluator.Call(llamaTokens))
                    {
                        result.Append(resultToken);
                    }

                    userData.UserSummary = result.ToString().Replace("User:", "", StringComparison.OrdinalIgnoreCase).Trim();

                    userData.LastChatId = lastMessage.Id;

                    await this._userDataService.Save(userData);
                }
            }

            Console.WriteLine("Completed");
        }

        public List<string> GetMessages(string userId)
        {
            int groups = 25;

            UserEmbeddings userEmbeddings = this.GetEmbeddings(userId);

            if (userEmbeddings.Embeddings.Count == 0)
            {
                return new List<string>();
            }

            Console.WriteLine("Rolling up: " + userEmbeddings.UserName);

            userEmbeddings.Embeddings = PreProcess(userEmbeddings.Embeddings);

            List<Embedding> toWrite = new();

            List<string> lines = new();

            if (userEmbeddings.Embeddings.Count == 0)
            {
                return new List<string>();
            }

            if (userEmbeddings.Embeddings.Count <= groups)
            {
                toWrite.AddRange(userEmbeddings.Embeddings);
            }
            else
            {
                LerpEmbeddings(userEmbeddings.Embeddings);

                List<EmbeddingCluster> clusters = ClusterEmbeddings(userEmbeddings.Embeddings, groups);

                foreach (EmbeddingCluster cluster in clusters)
                {
                    Embedding groupCenter = FindMostRepresentativeEmbedding(cluster);

                    toWrite.Add(groupCenter);
                }
            }

            foreach (Embedding e in toWrite)
            {
                lines.Add(e.Content);
            }

            return lines;
        }

        private static List<EmbeddingDistance> Calculate(List<Embedding> embeddings)
        {
            // Assume all vectors have the same length as the first one
            double[] averageEmbedding = new double[embeddings[0].Data.Length];

            foreach (double[] embedding in embeddings.Select(e => e.Data))
            {
                for (int i = 0; i < embedding.Length; i++)
                {
                    averageEmbedding[i] += embedding[i];
                }
            }

            for (int i = 0; i < averageEmbedding.Length; i++)
            {
                averageEmbedding[i] /= embeddings.Count;
            }

            // Key: index, Value: similarity to average
            List<EmbeddingDistance> similarities = new();

            for (int i = 0; i < embeddings.Count; i++)
            {
                double similarity = CosineSimilarity(averageEmbedding, embeddings[i].Data);
                similarities.Add(new EmbeddingDistance()
                {
                    Distance = similarity,
                    Embedding = embeddings[i]
                });
            }

            return similarities.OrderByDescending(s => s.Distance).ToList();
        }

        private static List<EmbeddingCluster> ClusterEmbeddings(List<Embedding> embeddings, int numberOfClusters = 10)
        {
            List<double[]> embeddingValues = embeddings.Select(e => e.Data).ToList();

            // Convert to double as the k-means algorithm in Accord.NET works with double array.
            double[][] inputs = embeddingValues.Select(x => Array.ConvertAll(x, y => (double)y)).ToArray();

            Accord.Math.Random.Generator.Seed = 0;

            // Create a new K-Means algorithm
            KMeans kmeans = new(numberOfClusters);

            // Compute and retrieve the data centroids
            KMeansClusterCollection clusters = kmeans.Learn(inputs);

            // Use the centroids to parition all the data
            int[] labels = clusters.Decide(inputs);

            // Prepare the result. Each item in the result is a cluster containing the indices of the embeddings that belong to it.
            List<int[]> result = new(numberOfClusters);

            for (int i = 0; i < numberOfClusters; i++)
            {
                result.Add(labels.Select((v, j) => new { Index = j, Label = v })
                                 .Where(x => x.Label == i)
                                 .Select(x => x.Index)
                                 .ToArray());
            }

            List<EmbeddingCluster> toReturn = new(numberOfClusters);

            for (int i = 0; i < numberOfClusters; i++)
            {
                int[] clabels = result[i];
                EmbeddingCluster embeddingCluster = new()
                {
                    Embeddings = clabels.Select(embeddings.ElementAt).ToArray(),
                    Centeroid = clusters.Centroids[i]
                };

                toReturn.Add(embeddingCluster);
            }

            return toReturn;
        }

        private static double CosineSimilarity(double[] vector1, double[] vector2)
        {
            if (vector1.Length != vector2.Length)
            {
                throw new ArgumentException("Vectors must be the same length");
            }

            double dotProduct = vector1.Zip(vector2, (a, b) => a * b).Sum();
            double magnitude1 = Math.Sqrt(vector1.Sum(a => a * a));
            double magnitude2 = Math.Sqrt(vector2.Sum(a => a * a));

            return dotProduct / (magnitude1 * magnitude2);
        }

        private static Embedding FindMostRepresentativeEmbedding(EmbeddingCluster cluster)
        {
            // Convert centroid to double array as the k-means algorithm in Accord.NET works with double array.

            double minDistance = double.MaxValue;
            Embedding? minDistanceEmbedding = null;

            foreach (Embedding e in cluster.Embeddings)
            {
                double distance = Accord.Math.Distance.Euclidean(cluster.Centeroid, e.Data);

                if (distance < minDistance)
                {
                    minDistance = distance;
                    minDistanceEmbedding = e;
                }
            }

            return minDistanceEmbedding;
        }

        private static List<Embedding> PreProcess(List<Embedding> embeddings)
        {
            List<EmbeddingDistance> similarities = Calculate(embeddings);

            foreach (EmbeddingDistance ed in similarities)
            {
                if (ed.Distance is > .9 or < .1)
                {
                    embeddings.Remove(ed.Embedding);
                }
            }

            return embeddings;
        }

        private string GetDisplayName(UserData userData)
        {
            string displayName = userData.DisplayName;

            if (string.IsNullOrWhiteSpace(displayName))
            {
                displayName = userData.UserId;
            }

            StringBuilder toReturn = new();

            foreach (char c in displayName)
            {
                if (char.IsLetterOrDigit(c))
                {
                    toReturn.Append(c);
                }
            }

            return toReturn.ToString();
        }

        private UserEmbeddings GetEmbeddings(string userId)
        {
            string queryString = $"select content, data, datecreated from chatentryembedding left outer join chatentry on ChatEntry.Id = ChatEntryEmbedding.ChatEntryId where len(content) > 30 and userId = '{userId}'";

            using SqlConnection connection = new(this._settings.ConnectionString);

            SqlCommand command = new(queryString, connection);
            connection.Open();

            SqlDataReader reader = command.ExecuteReader();

            List<Embedding> embeddings = new();

            while (reader.Read())
            {
                byte[] data = (byte[])reader[1];
                double[] floats = new double[data.Length / 4];

                for (int i = 0; i < data.Length; i += 4)
                {
                    floats[i / 4] = BitConverter.ToSingle(data, i);
                }

                embeddings.Add(new Embedding()
                {
                    Content = reader[0].ToString(),
                    DateCreated = DateTime.Parse(reader[2].ToString()),
                    Data = floats
                });
            }

            reader.Close();

            return new UserEmbeddings()
            {
                Embeddings = embeddings,
                UserName = userId
            };
        }

        private LlamaTokenCollection TryGetTokens(string s)
        {
            if (string.IsNullOrWhiteSpace(s))
            {
                return new LlamaTokenCollection();
            }

            return this._evaluator.Tokenize(s);
        }
    }
}