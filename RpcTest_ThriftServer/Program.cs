using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Thrift.Protocol;
using Thrift.Server;
using Thrift.Transport;

namespace RpcTest_ThriftServer
{
    class Program
    {
        static void Main(string[] args)
        {
            TServerSocket serverTransport = new TServerSocket(7911, 0, false);
            Helloword.Processor processor = new Helloword.Processor(new HellowordThrift());
            //TServer server = new TSimpleServer(processor, serverTransport);
            //TServer server = new TThreadedServer(processor, serverTransport);
            TServer server = new TThreadPoolServer(processor, serverTransport);
            Console.WriteLine("Starting server on port 7911 ...");
            server.Serve();



            


            Console.ReadKey();
        }
    }
}
