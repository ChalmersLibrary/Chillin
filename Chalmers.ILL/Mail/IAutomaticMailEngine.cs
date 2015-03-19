using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chalmers.ILL.Mail
{
    public interface IAutomaticMailEngine
    {
        void SendAll();
    }
}
