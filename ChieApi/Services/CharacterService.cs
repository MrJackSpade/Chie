﻿using ChieApi.Extensions;
using ChieApi.Interfaces;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace ChieApi.Services
{
	public class CharacterService : ICharacterFactory
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

		public async Task<CharacterConfiguration> Build()
		{
			lock (_characterLock)
			{
				_characterConfiguration ??= this.Load(this._settings.DefaultModel);
			}

			_characterConfiguration.MainPath = this._settings.LlamaMainExe;
			_characterConfiguration.Threads = System.Environment.ProcessorCount / 2;
			_characterConfiguration.Prompt = await GetTransformedPromptPath(_characterConfiguration.Prompt, "prompt.temp");
			_characterConfiguration.Start = await GetTransformedPromptPath(_characterConfiguration.Start, "start.temp");
			return _characterConfiguration;
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

				foreach (KeyValuePair<string, JsonNode?> property in cObject)
				{
					jObject[property.Key] = property.Value.CopyNode();
				}
			}

			string combinedString = jObject.ToString();

			return JsonSerializer.Deserialize<CharacterConfiguration>(combinedString, this.Options);
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

		public CharacterConfiguration Load(string characterName)
		{
			string characterDirectory = Path.Combine(AppContext.BaseDirectory, ROOT_PATH, characterName);

			CharacterConfiguration characterConfiguration = this.BuildJson(characterDirectory);
			characterConfiguration.Prompt = this.FindFiles(characterDirectory, "Prompt.txt").First();
			characterConfiguration.Start = this.FindFiles(characterDirectory, "Start.txt").First();

			return characterConfiguration;
		}

		private static async Task<string> GetTransformedPromptPath(string promptPath, string fName)
		{
			string transformedPrompt = await MacroService.TransformFile(promptPath);
			string transformedPromptDirectory = Path.Combine(AppContext.BaseDirectory, "Temp");

			string tempPromptFile = Path.Combine(transformedPromptDirectory, fName);

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