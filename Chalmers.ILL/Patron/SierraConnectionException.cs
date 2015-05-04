using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Chalmers.ILL.Patron
{
    public class SierraConnectionException : Exception
    {
        public SierraConnectionException() {}
        public SierraConnectionException(string message) : base(message) {}
        public SierraConnectionException(string message, Exception inner) {}
    }
}