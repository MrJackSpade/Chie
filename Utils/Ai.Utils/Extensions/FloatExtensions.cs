namespace Ai.Utils.Extensions
{
    public static class FloatExtensions
    {
        public static float[,] LoadFromFile(string filePath)
        {
            using BinaryReader reader = new(File.Open(filePath, FileMode.Open));

            int rows = reader.ReadInt32();
            int columns = reader.ReadInt32();

            float[,] data = new float[rows, columns];

            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < columns; j++)
                {
                    data[i, j] = reader.ReadSingle();
                }
            }

            return data;
        }

        public static void Save(this float[,] data, string filePath)
        {
            using BinaryWriter writer = new(File.Open(filePath, FileMode.Create));
            // Save dimensions
            writer.Write(data.GetLength(0)); // rows
            writer.Write(data.GetLength(1)); // columns

            for (int i = 0; i < data.GetLength(0); i++)
            {
                for (int j = 0; j < data.GetLength(1); j++)
                {
                    writer.Write(data[i, j]);
                }
            }
        }
    }
}