using MongoDB.Driver;
//database handling
public class MongoDbService
{
    private readonly IMongoCollection<User> _users;

    public MongoDbService(IMongoClient mongoClient)
    {
        var database = mongoClient.GetDatabase("pokesharpC");
        _users = database.GetCollection<User>("Users");
    }

    public IMongoCollection<User> GetUsersCollection()
    {
        return _users;
    }
}
