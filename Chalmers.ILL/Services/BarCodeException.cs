using System;

namespace Chalmers.ILL.Services
{
    public class BarCodeException : Exception
    {
        public BarCodeException() : base() { }
        public BarCodeException(string msg) : base(msg) { }
        public BarCodeException(string msg, Exception inner) : base(msg, inner) { }
    }
}