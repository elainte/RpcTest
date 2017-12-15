using System;
using System.Threading.Tasks;
using Grpc.Core;
using Helloworld;

namespace RpcTest_GrpcServer
{
    class Program
    {
        const int Port = 50051;

        public static void Main(string[] args)
        {
            Server server = new Server
            {
                Services = { Greeter.BindService(new GreeterImpl()) },
                Ports = { new ServerPort("localhost", Port, ServerCredentials.Insecure) }
            };
            server.Start();

            Console.WriteLine("Greeter server listening on port " + Port);
            Console.WriteLine("Press any key to stop the server...");
            Console.ReadKey();

            server.ShutdownAsync().Wait();
        }
    }



    class GreeterImpl : Greeter.GreeterBase
    {
        static int total = 0;

        // Server side handler of the SayHello RPC
        public override Task<HelloReply> SayHello(HelloRequest request, ServerCallContext context)
        {
            System.Threading.Interlocked.Increment(ref total);
            if (total == 1999)
                Console.WriteLine(total);
            return Task.FromResult(new HelloReply { Message = "Hello " + request.Name });
        }

        public override Task<GodbyeReply> SayGodbye(GodbyeRequest request, ServerCallContext context)
        {
            return Task.FromResult(new GodbyeReply { Message = "Hello " + request.Name });
        }
    }
}
