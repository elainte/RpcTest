using System;
using System.Threading.Tasks;

namespace RpcTest_WCFServer
{
    public class Helloword : IHelloword
    {
        static int total = 0;
        public Task<HelloReply> SayHello(HelloRequest helloRequest)
        {
            System.Threading.Interlocked.Increment(ref total);
            if (total == 1999)
                Console.WriteLine(total);

            return Task.FromResult(new HelloReply { Message = "Hello " + helloRequest.Name });
        }
    }
}
