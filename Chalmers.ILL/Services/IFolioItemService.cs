using Chalmers.ILL.Models;

namespace Chalmers.ILL.Services
{
    public interface IFolioItemService
    {
        ItemQuery ByQuery(string query);
        Item Put(Item item);
    }
}