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
				lock (_lock)
				{
					if (!File.Exists(_fileName))
					{
						return 0;
					}

					if (!_value.HasValue)
					{
						_value = long.Parse(File.ReadAllText(_fileName));
					}

					return _value.Value;
				}
			}
			set
			{
				lock (_lock)
				{
					if (_value.HasValue)
					{
						if (value <= _value.Value)
						{
							throw new InvalidOperationException("Can not set log entry lower than existing value");
						}
					}

					_value = value;

					File.WriteAllText(_fileName, value.ToString());
				}
			}
		}
	}
}