using Chalmers.ILL.Models;

namespace Chalmers.ILL.Services
{
    public interface IFolioInstanceService
    {
        Instance Post(InstanceBasic item);
    }
}