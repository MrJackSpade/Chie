using ChieApi.Interfaces;
using ChieApi.Shared.Entities;
using Loxifi.Extensions;
using System.Data.SqlClient;

namespace ChieApi.Services
{
    public class LogitService
    {
        private readonly string _connectionString;

        private readonly SemaphoreSlim _semaphore = new(1);

        private double?[] _cache = Array.Empty<double?>();

        public LogitService(IHasConnectionString connectionString)
        {
            _connectionString = connectionString.ConnectionString;
        }

        public IEnumerable<Logit> GetLogits(bool includeZeros = false)
        {
            using SqlConnection connection = new(_connectionString);

            string query = "select * from logit ";

            if (!includeZeros)
            {
                query += "where bias != 0";
            }

            return connection.Query<Logit>(query).ToList();
        }

        public void Identify(int id, string value, string displayValue)
        {
            bool exists = false;

            _semaphore.Wait();

            if (_cache.Length == 0)
            {
                List<Logit> existing = this.GetLogits(true).ToList();

                if (existing.Count > 0)
                {
                    long maxId = existing.Max(l => l.Id);

                    _cache = new double?[maxId + 1];

                    foreach (Logit logit in existing)
                    {
                        _cache[logit.Id] = logit.Bias;
                    }
                }
            }

            if (_cache.Length <= id)
            {
                double?[] tempcache = new double?[id + 1];

                for (int i = 0; i < _cache.Length; i++)
                {
                    tempcache[i] = _cache[i];
                }

                _cache = tempcache;
            }
            else
            {
                exists = _cache[id].HasValue;
            }

            if (!exists)
            {
                _cache[id] = 0;

                this.Save(new Logit()
                {
                    Bias = 0,
                    DisplayValue = displayValue,
                    Id = id,
                    Value = value,
                });
            }
            else
            {
                using SqlConnection connection = new(_connectionString);
                connection.Execute($"update logit set LastEncountered = GetDate(), Encountered = Encountered + 1 where id = {id}");
            }

            _semaphore.Release();
        }

        public void Save(Logit chatEntry)
        {
            try
            {
                using SqlConnection connection = new(_connectionString);

                connection.Insert(chatEntry);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Failed to save Logit {chatEntry.Id}: {e.Message}");
            }
        }
    }
}