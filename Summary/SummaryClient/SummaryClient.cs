using Loxifi;
using System.Text;
using System.Text.RegularExpressions;

namespace ImageRecognition
{
    public class SummaryClient
    {
        private const string TEMP_FILE_NAME = "Temp.txt";

        private readonly SummaryClientSettings _settings;

        public SummaryClient(SummaryClientSettings settings)
        {
            this._settings = settings;
        }

        public async Task<string[]> Tokenize(string data)
        {

            if (File.Exists(TEMP_FILE_NAME))
            {
                File.Delete(TEMP_FILE_NAME);
            }

            File.WriteAllText(TEMP_FILE_NAME, data);

            StringBuilder resultBuilder = new();
            StringBuilder errorBuilder = new();

            int tries = 0;

            do
            {
                try
                {
                    ProcessSettings settings = new(this._settings.PythonPath)
                    {
                        Arguments = $"{this._settings.TokenizePath} {Path.Combine(Directory.GetCurrentDirectory(), TEMP_FILE_NAME)}",
                        StdOutWrite = (s, e) => resultBuilder.Append(e),
                        StdErrWrite = (s, e) => errorBuilder.Append(e),
                        WorkingDirectory = new FileInfo(this._settings.SummaryPath).DirectoryName
                    };

                    uint r = await ProcessRunner.StartAsync(settings);

                    string result = resultBuilder.ToString();

                    if (r != 0)
                    {
                        throw new Exception(errorBuilder.ToString());
                    }

                    return this.ConvertTokenArray(DecodePythonBytes(result)).ToArray();
                }
                catch (Exception ex) when (tries++ < 3)
                {
                    resultBuilder.Clear();
                }
            } while (true);
        }

        public static byte[] HexStringToByteArray(string hex)
        {
            if (hex.Length % 2 != 0)
            {
                throw new ArgumentException("The provided string has an invalid length for a hex string.");
            }

            byte[] bytes = new byte[hex.Length / 2];
            for (int i = 0; i < hex.Length; i += 2)
            {
                string hexByte = hex.Substring(i, 2);
                bytes[i / 2] = Convert.ToByte(hexByte, 16);
            }

            return bytes;
        }

        public static string DecodePythonBytes(string pythonBytes)
        {
            pythonBytes = pythonBytes.Trim();

            // Remove the b' prefix and the trailing single quote
            pythonBytes = pythonBytes[2..^1];

            // Convert escaped byte sequences to actual bytes
            MatchCollection byteMatches = Regex.Matches(pythonBytes, @"(\\x[0-9a-fA-F]{2})+");
            foreach (Match match in byteMatches)
            {
                byte[] bytes = HexStringToByteArray(match.Value.Replace("\\x", ""));
                string text = System.Text.Encoding.UTF8.GetString(bytes);
                pythonBytes = pythonBytes.Replace(match.Value, text).ToString();
            }

            // Handle any other escape sequences
            string decodedStr = Regex.Unescape(pythonBytes);

            return decodedStr;
        }

        public string TrimSingleQuotes(string text)
        {
            if (text.StartsWith("'"))
            {
                text = text[1..];
            }

            if (text.EndsWith("'"))
            {
                text = text[..^1];
            }

            return text;
        }

        public IEnumerable<string> ConvertTokenArray(string tokenArrayString)
        {
            foreach(Match m in Regex.Matches(tokenArrayString, "', \"(.*?)\", '"))
            {
                tokenArrayString = tokenArrayString.Replace(m.Value, $"', '{m.Groups[1].Value}', '");
            }

            // Remove brackets and split by comma
            List<string> tokens = tokenArrayString.Trim(new char[] { '[', ']' })
                                         .Split(new string[] { "', '" }, StringSplitOptions.None)
                                         .ToList();

            foreach (string token in tokens)
            {
                string t = token;

                foreach (KeyValuePair<string, string> kvps in this._replacementChars)
                {
                    t = t.Replace(kvps.Key, kvps.Value);
                }

                yield return t;
            }
        }

        readonly Dictionary<string, string> _replacementChars = new()
        {
            ["Ġ"] = " ",
            ["Ċ"] = "\n"
        };

        public async Task<string> Summarize(string data, int maxLength)
        {

            if (File.Exists(TEMP_FILE_NAME))
            {
                File.Delete(TEMP_FILE_NAME);
            }

            File.WriteAllText(TEMP_FILE_NAME, data);

            StringBuilder resultBuilder = new();
            StringBuilder errorBuilder = new();

            int tries = 0;

            do
            {
                try
                {
                    ProcessSettings settings = new(this._settings.PythonPath)
                    {
                        Arguments = $"{this._settings.SummaryPath} {Path.Combine(Directory.GetCurrentDirectory(), TEMP_FILE_NAME)} {maxLength}",
                        StdOutWrite = (s, e) => resultBuilder.Append(e),
                        StdErrWrite = (s, e) => errorBuilder.Append(e),
                        WorkingDirectory = new FileInfo(this._settings.SummaryPath).DirectoryName
                    };

                    uint r = await ProcessRunner.StartAsync(settings);

                    string result = resultBuilder.ToString();

                    return result.Trim();
                }
                catch (Exception ex) when (tries++ < 3)
                {
                    resultBuilder.Clear();
                }
            } while (true);
        }
    }
}