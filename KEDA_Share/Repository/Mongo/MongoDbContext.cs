using KEDA_Share.Entity;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KEDA_Share.Repository.Mongo;
public class MongoDbContext
{
	private readonly IMongoClient _client;

	public MongoDbContext(string connectionString)
	{
		var mongoUrl = new MongoUrl(connectionString);	
		_client = new MongoClient(mongoUrl);
	}

	public IMongoCollection<T> GetCollection<T> (string databaseName, string collectionName)
	{
		var db = _client.GetDatabase(databaseName);
		return db.GetCollection<T>(collectionName);
	}
}
