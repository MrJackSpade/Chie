namespace DiscordGpt
{
    public class SavedId
    {
        private readonly string _fileName;

        private readonly object _lock = new();

        private long? _value;

        public SavedId(string fileName)
        {
            this._fileName = fileName;
        }

        public long Value
        {
            get
            {
                lock (this._lock)
                {
                    if (!File.Exists(this._fileName))
                    {
                        return 0;
                    }

                    if (!this._value.HasValue)
                    {
                        this._value = long.Parse(File.ReadAllText(this._fileName));
                    }

                    return this._value.Value;
                }
            }
            set
            {
                lock (this._lock)
                {
                    if (this._value.HasValue)
                    {
                        if (value <= this._value.Value)
                        {
                            throw new InvalidOperationException("Can not set log entry lower than existing value");
                        }
                    }

                    this._value = value;

                    File.WriteAllText(this._fileName, value.ToString());
                }
            }
        }
    }
}