using System;

namespace Events.Core.Abstraction
{
    public interface IEvent
    {
        event EventHandler<PointEventArgs> Point;
    }
    public class PointEventArgs : EventArgs
    {
        public DateTime GivenDateTime { get; }
    }
}
