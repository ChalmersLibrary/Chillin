using Chalmers.ILL.Models;

namespace Chalmers.ILL.Services
{
    public interface IFolioUserService
    {
        FolioUser ByUserName(string userName);
    }
}