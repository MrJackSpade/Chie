using ChieApi.Extensions;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace ChieApi.Services
{
	public class CharacterService
	{
		public const string ROOT_PATH = "Ai";

		private readonly object _characterLock = new();

		private readonly ChieApiSettings _settings;

		private CharacterConfiguration _characterConfiguration;

		public CharacterService(ChieApiSettings settings)
		{
			this._settings = settings;
		}

		public IEnumerable<string> Characters => Directory.EnumerateDirectories(ROOT_PATH).Select(d => new DirectoryInfo(d).Name);

		private JsonSerializerOptions Options
		{
			get
			{
				JsonSerializerOptions options = new();
				options.Converters.Add(new JsonStringEnumConverter());
				return options;
			}
		}

		public CharacterConfiguration Build()
		{
			lock (this._characterLock)
			{
				this._characterConfiguration ??= this.Load(this._settings.DefaultModel);
			}

			if (this._characterConfiguration.Threads <= 0)
			{
				this._characterConfiguration.Threads = (uint)(System.Environment.ProcessorCount / 2);
			}

			this._characterConfiguration.AssistantBlock = GetTransformedPromptPath(this._characterConfiguration.AssistantBlock, "assistant.temp");
			this._characterConfiguration.InstructionBlock = GetTransformedPromptPath(this._characterConfiguration.InstructionBlock, "instruction.temp");
			this._characterConfiguration.Start = GetTransformedPromptPath(this._characterConfiguration.Start, "start.temp");
			return this._characterConfiguration;
		}

		public CharacterConfiguration BuildJson(string path)
		{
			Stack<string> configPaths = new();

			JsonObject jObject = new();

			foreach (string config in this.FindFiles(path, "Configuration.json"))
			{
				configPaths.Push(config);
			}

			while (configPaths.Any())
			{
				string thisConfigPath = configPaths.Pop();

				string configContent = File.ReadAllText(thisConfigPath);

				JsonObject cObject = (JsonObject)JsonNode.Parse(configContent);

				this.CopyOver(cObject, jObject);
			}

			string combinedString = jObject.ToString();

			return JsonSerializer.Deserialize<CharacterConfiguration>(combinedString, this.Options);
		}

		private void CopyOver(JsonObject source, JsonObject destination)
		{
			foreach (KeyValuePair<string, JsonNode?> property in source)
			{
				if (property.Value is JsonObject cSource && destination[property.Key] is JsonObject cDest)
				{
					this.CopyOver(cSource, cDest);
				}
				else
				{
					destination[property.Key] = property.Value.CopyNode();
				}
			}
		}

		public IEnumerable<string> FindFiles(string path, string fileName)
		{
			DirectoryInfo directory = new(path);

			bool found = false;
			do
			{
				string toCheck = Path.Combine(directory.FullName, fileName);

				if (File.Exists(toCheck))
				{
					found = true;
					yield return toCheck;
				}

				directory = directory.Parent;
			} while (directory != null);

			if (!found)
			{
				throw new FileNotFoundException($"File {fileName} not found in {path} or any parent directory");
			}
		}

		public CharacterConfiguration Load(string configurationName)
		{
			string characterDirectory = Path.Combine(AppContext.BaseDirectory, ROOT_PATH, configurationName);

			CharacterConfiguration characterConfiguration = this.BuildJson(characterDirectory);
			characterConfiguration.InstructionBlock = this.FindFiles(characterDirectory, "Instruction.txt").First();
			characterConfiguration.AssistantBlock = this.FindFiles(characterDirectory, "Assistant.txt").First();
			characterConfiguration.Start = this.FindFiles(characterDirectory, "Start.txt").First();

			return characterConfiguration;
		}

		private static string GetTransformedPromptPath(string promptPath, string fName)
		{
			string transformedPrompt = MacroService.TransformFile(promptPath);

			string transformedPromptDirectory = Path.Combine(AppContext.BaseDirectory, "Temp");

			string tempPromptFile = Path.Combine(transformedPromptDirectory, Guid.NewGuid().ToString() + "." + fName);

			if (!Directory.Exists(transformedPromptDirectory))
			{
				Directory.CreateDirectory(transformedPromptDirectory);
			}

			if (File.Exists(tempPromptFile))
			{
				File.Delete(tempPromptFile);
			}

			File.WriteAllText(tempPromptFile, transformedPrompt);

			return tempPromptFile;
		}
	}
}