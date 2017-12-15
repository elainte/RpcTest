using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace RpcTest_WCFServer
{
    class Program
    {
        static void Main(string[] args)
        {
            using (ServiceHost host = new ServiceHost(typeof(Helloword)))
            {
                System.ServiceModel.Channels.Binding httpBinding = new BasicHttpBinding();
                host.AddServiceEndpoint(typeof(IHelloword), httpBinding, "http://localhost:8009/");

                System.ServiceModel.Channels.Binding netTcpBinding = new NetTcpBinding();
                host.AddServiceEndpoint(typeof(IHelloword), netTcpBinding, "net.tcp://localhost:8002/");


                host.Opened += delegate
                {
                    Console.WriteLine("Service is already open");
                };
                //运行  
                host.Open();
                Console.ReadKey();
                //关闭  
                host.Close();
            }
        }
    }
}
