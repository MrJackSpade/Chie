using ChieApi.Interfaces;
using System.Text.RegularExpressions;

namespace ChieApi.CleanupPipeline
{
    public class CommaPadCleaner : IResponseCleaner
    {
        public static string PadCommas(string input) => Regex.Replace(input, @"([a-zA-Z]{2}),([a-zA-Z])", "$1, $2");
        public string Clean(string content) => PadCommas(content);
    }
}
