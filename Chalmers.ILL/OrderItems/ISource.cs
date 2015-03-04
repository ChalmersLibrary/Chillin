using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Chalmers.ILL.OrderItems
{
    public interface ISource
    {
        SourcePollingResult Poll();
    }
}