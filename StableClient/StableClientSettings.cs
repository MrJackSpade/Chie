namespace StableClient
{
	public enum Sampler
	{
		Euler
	}

	public class StableClientSettings
	{
		public int Height { get; set; } = 512;

		public string Model { get; set; }

		public string OutDir { get; set; }

		public string Prompt { get; set; }

		public string PythonPath { get; set; }

		public Sampler Sampler { get; set; }

		public int Samples { get; set; } = 1;

		public float Scale { get; set; } = 7.5f;

		public int Steps { get; set; } = 50;

		public string Text2ImagePath { get; set; }

		public int Width { get; set; } = 512;
	}
}