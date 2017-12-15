using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RpcTest_ThriftServer
{
    public class HellowordThrift : Helloword.Iface
    {
        static int total = 0;
        public HelloReply SayHello(HelloRequest request)
        {
            System.Threading.Interlocked.Increment(ref total);
            if (total == 1999)
                Console.WriteLine(total);

            return new HelloReply { Message = "Hello " + request.Name };
        }
    }
}
