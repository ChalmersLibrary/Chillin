using System;

namespace Chalmers.ILL.Connections
{
    public interface IFolioConnection
    {
        void ClearToken();
        bool NeedNewToken();
        bool IsRefreshTokenOk();
        void SetToken();
        string GetFolioApiBaseAddress();
        string GetToken();
        string GetTenant();
    }
}
