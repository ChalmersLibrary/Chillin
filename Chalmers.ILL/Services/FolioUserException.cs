using System;

namespace Chalmers.ILL.Services
{
    public class FolioUserException : Exception
    {
        public FolioUserException() : base() { }
        public FolioUserException(string msg) : base(msg) { }
        public FolioUserException(string msg, System.Exception inner) : base(msg, inner) { }
    }
}