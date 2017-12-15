using System;
using System.Threading.Tasks;
using RpcTest_OrleansServer.Interfaces;

namespace RpcTest_OrleansServer.Grains
{
    /// <summary>
    /// Orleans grain implementation class HelloGrain.
    /// </summary>
    public class HelloGrain : Orleans.Grain, IHello
    {
        static int total = 0;

        Task<HelloReply> IHello.SayHello(HelloRequest greeting)
        {
            System.Threading.Interlocked.Increment(ref total);
            if (total == 1999)
                Console.WriteLine(total);

            return Task.FromResult(new HelloReply
            {
                Message = "hello " + greeting.Name
            });
        }
    }
}
