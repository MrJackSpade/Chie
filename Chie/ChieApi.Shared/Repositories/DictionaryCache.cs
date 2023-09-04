using ChieApi.Interfaces;
using ChieApi.Shared.Models;
using Loxifi.Extensions;
using System.Data.SqlClient;

namespace ChieApi.Shared.Services
{
	public class DictionaryCache
    {
        private readonly string _connectionString;

        private readonly Dictionary<string, List<DictionaryEntry>> _fingerprintCache = new(StringComparer.OrdinalIgnoreCase);

        private readonly Dictionary<string, DictionaryEntry> _wordCache = new(StringComparer.OrdinalIgnoreCase);

        private readonly object _wordsLock = new();

        private string[] _words = null;

        public DictionaryCache(IHasConnectionString hasConnectionString)
        {
            this._connectionString = hasConnectionString.ConnectionString;
        }

        public DictionaryEntry? GetWord(string word)
        {
            if (this._wordCache.TryGetValue(word, out DictionaryEntry entry))
            {
                return entry;
            }

            using SqlConnection sqlConnection = new(this._connectionString);

            string pWord = word.Replace("'", "''");

            entry = sqlConnection.Query<DictionaryEntry>($"select * from Dictionary where word = '{pWord}'").FirstOrDefault();

            this._wordCache.Add(word, entry);

            return entry;
        }

        public string[] GetWords()
        {
            lock (this._wordsLock)
            {
                if (this._words == null)
                {
                    using SqlConnection sqlConnection = new(this._connectionString);

                    IEnumerable<DictionaryEntry> entries = sqlConnection.Query<DictionaryEntry>($"select * from Dictionary");

                    this._words = entries.Select(d => d.Word).ToArray();
                }

                return this._words;
            }
        }

        public List<DictionaryEntry> GetWordsByFingerprint(string fingerprint)
        {
            if (this._fingerprintCache.TryGetValue(fingerprint, out List<DictionaryEntry> entries))
            {
                return entries;
            }

            using SqlConnection sqlConnection = new(this._connectionString);

            string pFingerprint = fingerprint.Replace("'", "''");

            entries = sqlConnection.Query<DictionaryEntry>($"select * from Dictionary where fingerprint = '{pFingerprint}'").ToList();

            this._fingerprintCache.Add(fingerprint, entries);

            return entries;
        }

        public bool IsWord(string word) => this.GetWord(word) != null;
    }
}