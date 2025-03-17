using MongoDB.Bson;

public class User
{
    public ObjectId Id { get; set; }  
    public string Username { get; set; } = string.Empty;
    public string UserPass { get; set; } = string.Empty;
}
