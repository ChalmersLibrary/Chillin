using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chalmers.ILL.OrderItems
{
    public interface ISourceFactory
    {
        /// <summary>
        /// Returns all the sources that should be polled for new orders and updates to existing orders.
        /// </summary>
        /// <returns>A list with the sources.</returns>
        List<ISource> Sources();
    }
}
