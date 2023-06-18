using System;
using System.Diagnostics;
using System.IO;

namespace Llama.Utilities
{
    /// <summary>
    /// The logger of LlamaSharp. On default it write to console. User methods of `LlamaLogger.Default` to change the behavior.
    /// </summary>
    public sealed class LlamaLogger
    {
        private static readonly Lazy<LlamaLogger> _instance = new(() => new LlamaLogger());

        private FileStream? _fileStream = null;

        private StreamWriter _fileWriter = null;

        private bool _toConsole = true;

        private bool _toFile = false;

        private LlamaLogger()
        {
        }

        public static LlamaLogger Default => _instance.Value;

        public LlamaLogger DisableConsole()
        {
            this._toConsole = false;
            return this;
        }

        public LlamaLogger DisableFile(string filename)
        {
            if (this._fileWriter is not null)
            {
                this._fileWriter.Close();
                this._fileWriter = null;
            }

            if (this._fileStream is not null)
            {
                this._fileStream.Close();
                this._fileStream = null;
            }

            this._toFile = false;
            return this;
        }

        public LlamaLogger EnableConsole()
        {
            this._toConsole = true;
            return this;
        }

        public LlamaLogger EnableFile(string filename, FileMode mode = FileMode.Append)
        {
            this._fileStream = new FileStream(filename, mode, FileAccess.Write);
            this._fileWriter = new StreamWriter(this._fileStream);
            this._toFile = true;
            return this;
        }

        public void Error(string message)
        {
            message = this.MessageFormat("error", message);
            if (this._toConsole)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(message);
                Console.ResetColor();
            }

            if (this._toFile)
            {
                Debug.Assert(this._fileStream is not null);
                Debug.Assert(this._fileWriter is not null);
                this._fileWriter.WriteLine(message);
            }
        }

        public void Info(string message, bool hideConsole = false)
        {
            message = this.MessageFormat("info", message);
            if (this._toConsole && !hideConsole)
            {
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine(message);
                Console.ResetColor();
            }

            if (this._toFile)
            {
                Debug.Assert(this._fileStream is not null);
                Debug.Assert(this._fileWriter is not null);
                this._fileWriter.WriteLine(message);
            }
        }

        public void Warn(string message, bool hideConsole = false)
        {
            message = this.MessageFormat("warn", message);
            if (this._toConsole && !hideConsole)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine(message);
                Console.ResetColor();
            }

            if (this._toFile)
            {
                Debug.Assert(this._fileStream is not null);
                Debug.Assert(this._fileWriter is not null);
                this._fileWriter.WriteLine(message);
            }
        }

        private string MessageFormat(string level, string message)
        {
            DateTime now = DateTime.Now;
            string formattedDate = now.ToString("yyyy.MM.dd HH:mm:ss");
            return $"[{formattedDate}][{level}]: {message}";
        }
    }
}