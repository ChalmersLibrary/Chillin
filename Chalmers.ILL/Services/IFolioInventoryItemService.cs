using Chalmers.ILL.Models;

namespace Chalmers.ILL.Services
{
    public interface IFolioInventoryItemService
    {
        Item Post(InventoryItemBasic item, bool readOnlyAtLibrary);
    }
}