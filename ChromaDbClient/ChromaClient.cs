using ChromaDbClient.Exceptions;
using ChromaDbClient.Models;
using System.Net.Http.Json;
using System.Text.Json;

namespace ChromaDbClient
{
	public class ChromaClient
	{
		private readonly string _basePath;

		private readonly HttpClient _httpClient = new();

		private readonly JsonSerializerOptions _jsonSerializerOptions = new()
		{
			DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
		};

		public ChromaClient(string basePath)
		{
			this._basePath = basePath;
		}

		public async Task<AddResponse> Add(CollectionInformation collectionInformation, AddEmbedding embeddings)
		{
			string response = await this.Post<string>($"/api/v1/collections/{collectionInformation.Id}/add", embeddings);

			return new AddResponse()
			{
				Added = bool.Parse(response)
			};
		}

		public async Task<int> Count(CollectionInformation collectionInformation) => int.Parse(await this.Get<string>($"/api/v1/collections/{collectionInformation.Id}/count"));

		public async Task<CollectionInformation> CreateCollection(CreateCollection createCollection) => await this.Post<CollectionInformation>("/api/v1/collections", createCollection);

		public async Task<AddResponse> CreateIndex(CollectionInformation collectionInformation) => await this.Post<AddResponse>($"/api/v1/collections/{collectionInformation.Id}/create_index", null);

		public async Task<DeletedResponse> Delete(CollectionInformation collectionInformation, DeleteEmbedding embeddings)
		{
			List<string> ids = await this.Post<List<string>>($"/api/v1/collections/{collectionInformation.Id}/delete", embeddings);

			return new DeletedResponse()
			{
				Ids = ids.ToList()
			};
		}

		public async Task<AddResponse> DeleteCollection(string collectionName) => await this.Delete<AddResponse>($"/api/v1/collections/{collectionName}");

		public async Task<AddResponse> DeleteCollection(CollectionInformation collectionInformation) => await this.Delete<AddResponse>($"/api/v1/collections/{collectionInformation.Name}");

		public T Deserialize<T>(string json) where T : class
		{
			BaseResponse r = null;
			T toReturn = null;

			if (!typeof(BaseResponse).IsAssignableFrom(typeof(T)))
			{
				try
				{
					if (this.IsObject(json))
					{
						r = JsonSerializer.Deserialize<BaseResponse>(json);
					}
				}
				catch (Exception ex)
				{
				}

				if (typeof(T) == typeof(string))
				{
					toReturn = (T)(object)json;
				}
				else
				{
					toReturn = JsonSerializer.Deserialize<T>(json);
				}
			}
			else
			{
				toReturn = JsonSerializer.Deserialize<T>(json);

				r = (BaseResponse)(object)toReturn;
			}

			if (r is BaseResponse b)
			{
				if (!b.IsSuccess)
				{
					if (b.Details.Any())
					{
						throw new ChromaDbValidationException(b.Details);
					}
					else
					{
						throw new UnhandledChromaDbException(b.Error, b.Message);
					}
				}
			}

			return toReturn;
		}

		public async Task<GetResponse> Get(CollectionInformation collectionInformation, GetEmbedding embeddings) => await this.Post<GetResponse>($"/api/v1/collections/{collectionInformation.Id}/get", embeddings);

		public async Task<CollectionInformation> GetCollection(string collectionName) => await this.Get<CollectionInformation>($"/api/v1/collections/{collectionName}");

		public string GetUrl(string path)
		{
			path = "/" + path.Trim('/');

			return this._basePath.Trim('/') + path;
		}

		public async Task<List<CollectionInformation>> ListCollections() => await this.Get<List<CollectionInformation>>("/api/v1/collections");

		public async Task<BaseResponse> Persist() => await this.Post<BaseResponse>($"/api/v1/persist", null);

		public async Task<QueryResponse> Query(CollectionInformation collectionInformation, QueryTexts embeddings) => await this.Post<QueryResponse>($"/api/v1/collections/{collectionInformation.Id}/query", embeddings);

		public async Task<QueryResponse> Query(CollectionInformation collectionInformation, QueryEmbedding embeddings) => await this.Post<QueryResponse>($"/api/v1/collections/{collectionInformation.Id}/query", embeddings);

		public async Task<BaseResponse> RawSql(CollectionInformation collectionInformation, RawSql embeddings) => await this.Post<BaseResponse>($"/api/v1/collections/{collectionInformation.Id}/rawsql", embeddings);

		public async Task<BaseResponse> Reset() => await this.Post<BaseResponse>($"/api/v1/reset", null);

		public async Task<UpdateResponse> Update(CollectionInformation collectionInformation, UpdateEmbedding embeddings)
		{
			string response = await this.Post<string>($"/api/v1/collections/{collectionInformation.Id}/update", embeddings);

			return new UpdateResponse() { Updated = bool.Parse(response) };
		}

		public async Task<AddResponse> UpdateCollection(CollectionInformation collectionInformation, UpdateCollection embeddings) => await this.Put<AddResponse>($"/api/v1/collections/{collectionInformation.Id}", embeddings);

		public async Task<AddResponse> Upsert(CollectionInformation collectionInformation, AddEmbedding embeddings) => await this.Post<AddResponse>($"/api/v1/collections/{collectionInformation.Id}/upsert", embeddings);

		private async Task<T> Delete<T>(string url) where T : class
		{
			HttpResponseMessage response = await this._httpClient.DeleteAsync(this.GetUrl(url));
			string responseString = await response.Content.ReadAsStringAsync();
			return this.Deserialize<T>(responseString);
		}

		private async Task<T> Get<T>(string url) where T : class
		{
			HttpResponseMessage response = await this._httpClient.GetAsync(this.GetUrl(url));
			string responseString = await response.Content.ReadAsStringAsync();
			return this.Deserialize<T>(responseString);
		}

		private bool IsObject(string json)
		{
			if (string.IsNullOrWhiteSpace(json))
			{
				return false;
			}

			json = json.Trim();

			if (json.StartsWith("{") && json.EndsWith("}"))
			{
				return true;
			}

			return false;
		}

		private async Task<T?> Post<T>(string url, object obj) where T : class
		{
			HttpResponseMessage response;
			if (obj != null)
			{
				response = await this._httpClient.PostAsync(this.GetUrl(url), JsonContent.Create(obj, options: _jsonSerializerOptions));
			}
			else
			{
				response = await this._httpClient.PostAsync(this.GetUrl(url), new StringContent(string.Empty));
			}

			string responseString = await response.Content.ReadAsStringAsync();
			return this.Deserialize<T>(responseString);
		}

		private async Task<T?> Put<T>(string url, object obj) where T : class
		{
			HttpResponseMessage response;
			if (obj != null)
			{
				response = await this._httpClient.PutAsync(this.GetUrl(url), JsonContent.Create(obj, options: _jsonSerializerOptions));
			}
			else
			{
				response = await this._httpClient.PutAsync(this.GetUrl(url), new StringContent(string.Empty));
			}

			string responseString = await response.Content.ReadAsStringAsync();
			return this.Deserialize<T>(responseString);
		}
	}
}