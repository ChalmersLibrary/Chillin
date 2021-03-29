using Chalmers.ILL.Models;

namespace Chalmers.ILL.Services
{
    public interface IFolioItemService
    {
        ItemQuery ByQuery(string query);
        Item Post(ItemBasic item);
        Item Put(Item item);
    }
}