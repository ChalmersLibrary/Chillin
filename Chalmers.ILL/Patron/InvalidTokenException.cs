using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Chalmers.ILL.Patron
{
    public class InvalidTokenException : Exception
    {
        public InvalidTokenException() : base() { }
        public InvalidTokenException(string msg) : base(msg) { }
        public InvalidTokenException(string msg, System.Exception inner) : base(msg, inner) { }
    }
}