using ChromaDbClient.Models;

namespace ChromaDbClient.Tests
{
	[TestClass]
	public class ClientTests
	{
		private const string TEST_COLLECTION_NAME = "unit_test";

		public List<List<float>> DefaultEmbeddings => new() { new List<float>() { 1f, 1f, 1f, 1f } };

		public List<List<float>> UpdateEmbeddings => new() { new List<float>() { 2f, 2f, 2f, 2f } };

		[TestMethod]
		public async Task TestAdd()
		{
			ChromaClient chromaClient = new("http://192.168.0.93:8000");
			CollectionInformation collection = await this.EnsureCollection();
			await this.EnsureDeleted("1");
			AddResponse response = await chromaClient.Add(collection, new Models.AddEmbedding()
			{
				Documents = new List<string> { "testDoc" },
				Embeddings = DefaultEmbeddings,
				Ids = new List<string> { "1" },
				IncrementIndex = true,
				Metadatas = this.DictionaryList("test", 1, "test2", "2")
			});

			Assert.IsTrue(response.Added);
		}

		[TestMethod]
		public async Task TestCount()
		{
			ChromaClient chromaClient = new("http://192.168.0.93:8000");
			CollectionInformation collection = await this.EnsureCollection();
			int c = await chromaClient.Count(collection);
		}

		[TestMethod]
		public async Task TestCreateCollection()
		{
			CollectionInformation response = await this.EnsureCollection();
		}

		[TestMethod]
		public async Task TestCreateIndex()
		{
			ChromaClient chromaClient = new("http://192.168.0.93:8000");
			CollectionInformation collection = await this.EnsureCollection();
			await chromaClient.CreateIndex(collection);
		}

		public async Task<DeletedResponse> EnsureDeleted(string id)
		{
			CollectionInformation collection = await this.EnsureCollection();
			ChromaClient chromaClient = new("http://192.168.0.93:8000");
			return await chromaClient.Delete(collection, new DeleteEmbedding(id));
		}

		[TestMethod]
		public async Task TestDelete()
		{
			BaseResponse response = await this.EnsureDeleted("1");
			Assert.IsTrue(response.IsSuccess);
		}

		[TestMethod]
		public async Task TestDeleteCollection()
		{
			ChromaClient chromaClient = new("http://192.168.0.93:8000");
			CollectionInformation collection = await this.EnsureCollection();
			await chromaClient.DeleteCollection(collection);
		}

		[TestMethod]
		public async Task TestGet()
		{
			ChromaClient chromaClient = new("http://192.168.0.93:8000");
			CollectionInformation collection = await this.EnsureCollection();
			GetResponse response = await chromaClient.Get(collection, new GetEmbedding()
			{
				Ids = new List<string> { "1" }
			});
		}

		[TestMethod]
		public async Task TestGetCollection()
		{
			ChromaClient chromaClient = new("http://192.168.0.93:8000");
			CollectionInformation collection = await this.EnsureCollection();
			BaseResponse response = await chromaClient.GetCollection(collection.Name);
		}

		[TestMethod]
		public async Task TestListCollections()
		{
			ChromaClient chromaClient = new("http://192.168.0.93:8000");
			CollectionInformation collection = await this.EnsureCollection();
			List<CollectionInformation> response = await chromaClient.ListCollections();
			Assert.IsTrue(response.Count > 0);
		}

		[TestMethod]
		public async Task TestPersist()
		{
			ChromaClient chromaClient = new("http://192.168.0.93:8000");
			await chromaClient.Persist();
		}

		[TestMethod]
		public async Task TestQuery()
		{
			ChromaClient chromaClient = new("http://192.168.0.93:8000");
			CollectionInformation collection = await this.EnsureCollection();
			QueryResponse response = await chromaClient.Query(collection, new QueryEmbedding()
			{
				Embeddings = DefaultEmbeddings
			});
		}

		[TestMethod]
		public async Task TestRawSql()
		{
			ChromaClient chromaClient = new("http://192.168.0.93:8000");
			CollectionInformation collection = await this.EnsureCollection();
			BaseResponse response = await chromaClient.RawSql(collection, new RawSql()
			{
				Query = "What?"
			});
		}

		[TestMethod]
		public async Task TestReset()
		{
			throw new NotImplementedException();
			ChromaClient chromaClient = new("http://192.168.0.93:8000");
			await chromaClient.Reset();
		}

		[TestMethod]
		public async Task TestUpdate()
		{
			ChromaClient chromaClient = new("http://192.168.0.93:8000");
			CollectionInformation collection = await this.EnsureCollection();
			await chromaClient.Update(collection, new UpdateEmbedding()
			{
				Documents = new List<string> { "testDoc2" },
				Embeddings = UpdateEmbeddings,
				Ids = new List<string> { "1" },
				IncrementIndex = true,
				Metadatas = this.DictionaryList("test", 1)
			});
		}

		[TestMethod]
		public async Task TestUpdateCollection()
		{
			ChromaClient chromaClient = new("http://192.168.0.93:8000");
			CollectionInformation collection = await this.EnsureCollection();
			await chromaClient.UpdateCollection(collection, new UpdateCollection()
			{
				NewMetaData = new Dictionary<string, object>() { ["testupdate"] = 3 },
				NewName = TEST_COLLECTION_NAME + "_UPDATED"
			});
		}

		[TestMethod]
		public async void TestUpsert()
		{
			ChromaClient chromaClient = new("http://192.168.0.93:8000");
			CollectionInformation collection = await this.EnsureCollection();
			await chromaClient.Upsert(collection, new AddEmbedding()
			{
				Documents = new List<string> { "testDoc2" },
				Embeddings = DefaultEmbeddings,
				Ids = new List<string> { "1" },
				IncrementIndex = true,
				Metadatas = this.DictionaryList("test", 1, "test2", "2")
			});
		}

		private List<Dictionary<string, object>> DictionaryList(string key, object value)
		{
			Dictionary<string, object> innerD = new()
			{
				{ key, value }
			};
			return new List<Dictionary<string, object>> { innerD };
		}

		private List<Dictionary<string, object>> DictionaryList(string key, object value, string key2, object value2)
		{
			Dictionary<string, object> innerD = new()
			{
				{ key, value },
				{ key2, value2 }
			};
			return new List<Dictionary<string, object>> { innerD };
		}

		private List<Dictionary<string, object>> DictionaryList(string key, object value, string key2, object value2, string key3, object value3)
		{
			Dictionary<string, object> innerD = new()
			{
				{ key, value },
				{ key2, value2 },
				{ key3, value3 }
			};
			return new List<Dictionary<string, object>> { innerD };
		}

		private async Task<CollectionInformation> EnsureCollection()
		{
			ChromaClient chromaClient = new("http://192.168.0.93:8000");
			return await chromaClient.CreateCollection(new Models.CreateCollection()
			{
				GetOrCreate = true,
				MetaData = new Dictionary<string, object>() { ["Test"] = 1 },
				Name = TEST_COLLECTION_NAME
			});
		}
	}
}