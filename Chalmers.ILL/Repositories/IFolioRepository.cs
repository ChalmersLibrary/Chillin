namespace Chalmers.ILL.Repositories
{
    public interface IFolioRepository
    {
        string ByQuery(string path);
        string Post(string path, string body);
        string Put(string path, string body);
    }
}