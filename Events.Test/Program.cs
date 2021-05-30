using System;
using System.Threading;
using System.Threading.Tasks;

namespace Events.Test
{
    class Program
    {
        static void Main(string[] args)
        {
            Events.Core.Event events = new Core.Event(TimeSpan.FromSeconds(5));
            events.Point += (sender, e)=> {
                Console.WriteLine("Hit");
            };
            events.Start(false);
            Thread.Sleep(15000);
            events.Stop();
            Console.WriteLine("Stopped");
            Console.ReadLine();
        }
    }
}
