namespace Chalmers.ILL.Services
{
    public interface IJsonService
    {
        T DeserializeObject<T>(string obj);
        string SerializeObject<T>(T obj);
    }
}