using Loxifi;
using System.Text;

namespace StableClient
{
	public class StableClient
	{
		public StableClient()
		{
		}

		public async Task<byte[]> Generate(StableClientSettings settings)
		{
			StringBuilder resultString = new();

			string tempDir = Path.Combine(settings.OutDir, Guid.NewGuid().ToString());

			if (!Directory.Exists(tempDir))
			{
				Directory.CreateDirectory(tempDir);
			}

			string arguments = this.BuildArguments(settings, tempDir);

			uint result = await Loxifi.ProcessRunner.StartAsync(new ProcessSettings(settings.PythonPath)
			{
				Arguments = arguments,
				StdOutWrite = (o, s) => resultString.Append(s)
			});

			string oFile = Directory.EnumerateFiles(tempDir).Single();

			byte[] resultData = File.ReadAllBytes(oFile);

			File.Delete(oFile);

			Directory.Delete(tempDir);

			return resultData;
		}

		private string BuildArguments(StableClientSettings settings, string tempDir)
		{
			StringBuilder argsString = new();

			void AddArg(string name, object value, bool quotes = false)
			{
				argsString.Append(" --");
				argsString.Append(name);
				argsString.Append(' ');
				if (quotes)
				{
					argsString.Append('"');
				}

				argsString.Append(value);
				if (quotes)
				{
					argsString.Append('"');
				}
			}

			argsString.Append(settings.Text2ImagePath);
			argsString.Append(' ');
			AddArg("prompt", settings.Prompt, true);
			AddArg("ckpt", settings.Model, true);
			AddArg("outdir", tempDir, true);
			AddArg("ddim_steps", settings.Steps);
			AddArg("n_samples", settings.Samples);
			AddArg("H", settings.Height);
			AddArg("W", settings.Width);
			AddArg("scale", settings.Scale);
			AddArg("seed", new Random().Next(0, int.MaxValue));

			return argsString.ToString();
		}
	}
}