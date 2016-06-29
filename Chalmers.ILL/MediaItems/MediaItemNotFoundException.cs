using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Chalmers.ILL.MediaItems
{
    public class MediaItemNotFoundException : Exception
    {
        public MediaItemNotFoundException(string msg) : base(msg) { }
    }
}