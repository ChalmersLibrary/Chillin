using System;

namespace Chalmers.ILL.Exceptions
{
    public class FolioUserException : Exception
    {
        public FolioUserException() : base() { }
        public FolioUserException(string msg) : base(msg) { }
        public FolioUserException(string msg, Exception inner) : base(msg, inner) { }
    }
}