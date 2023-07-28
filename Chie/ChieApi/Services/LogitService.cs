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
            this._connectionString = connectionString.ConnectionString;
        }

        public IEnumerable<Logit> GetLogits(bool includeZeros = false)
        {
            using SqlConnection connection = new(this._connectionString);

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

            this._semaphore.Wait();

            if (this._cache.Length == 0)
            {
                List<Logit> existing = this.GetLogits(true).ToList();

                if (existing.Count > 0)
                {
                    long maxId = existing.Max(l => l.Id);

                    this._cache = new double?[maxId + 1];

                    foreach (Logit logit in existing)
                    {
                        this._cache[logit.Id] = logit.Bias;
                    }
                }
            }

            if (this._cache.Length <= id)
            {
                double?[] tempcache = new double?[id + 1];

                for (int i = 0; i < this._cache.Length; i++)
                {
                    tempcache[i] = this._cache[i];
                }

                _cache = tempcache;
            }
            else
            {
                exists = this._cache[id].HasValue;
            }

            if (!exists)
            {
                this._cache[id] = 0;

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
                using SqlConnection connection = new(this._connectionString);
                connection.Execute($"update logit set LastEncountered = GetDate(), Encountered = Encountered + 1 where id = {id}");
            }

            this._semaphore.Release();
        }

        public void Save(Logit chatEntry)
        {
            using SqlConnection connection = new(this._connectionString);

            connection.Insert(chatEntry);
        }
    }
}