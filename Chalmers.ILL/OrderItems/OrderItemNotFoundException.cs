using System;

namespace Chalmers.ILL.OrderItems
{
    public class OrderItemNotFoundException : Exception
    {
        public OrderItemNotFoundException(string msg) : base(msg) { }
    }
}