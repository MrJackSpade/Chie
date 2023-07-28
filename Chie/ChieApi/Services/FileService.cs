namespace ChieApi.Services
{
    public static class FileService
    {
        public static string GetStringOrContent(string pathOrContent)
        {
            if (File.Exists(pathOrContent))
            {
                return File.ReadAllText(pathOrContent);
            }
            else
            {
                return pathOrContent;
            }
        }
    }
}