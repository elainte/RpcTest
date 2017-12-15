using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.ServiceModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DotNetty.Codecs;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using Grpc.Core;
using Helloworld;
using Orleans;
using Orleans.Runtime;
using Orleans.Runtime.Configuration;
using RpcTest_OrleansServer.Interfaces;
using Thrift.Protocol;
using Thrift.Transport;
using ChannelOption = Grpc.Core.ChannelOption;
using RpcTest_OrleansServer.Interfaces;

namespace RpcTest
{
    class Program
    {
        private static readonly int _count = Convert.ToInt32(ConfigurationManager.AppSettings["count"]);
        private static readonly int _threadcount = Convert.ToInt32(ConfigurationManager.AppSettings["threadcount"]);

        static void Main(string[] args)
        {
            RunGrpc();

            RunWcfHttp();

            RunWcfTcp();

            RunThriftBio();

            RunHttpClient();

            RunHttpWebRequest();

            //RunNetty();  并不是同层级的东西，不做对比。

            RunOrleans();



            //TODO：可以测试一下异步，多线程

            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }

        static void RunGrpc()
        {
            Channel channel = new Channel("127.0.0.1:50051", ChannelCredentials.Insecure);
            String user = "world";
            CodeTimerAdvance.TimeByConsole("gRpc", _count, a =>
            {
                var client = new Greeter.GreeterClient(channel);
                var reply = client.SayHello(new Helloworld.HelloRequest { Name = user + a });
            }, _threadcount);
            channel.ShutdownAsync().Wait();
        }

        static void RunWcfHttp()
        {
            string address = "http://localhost:8009/";
            ChannelFactory<RpcTest_WCFServer.IHelloword> channel =
                new ChannelFactory<RpcTest_WCFServer.IHelloword>(
                    new BasicHttpBinding(),
                    new EndpointAddress(
                        new Uri(address)));
            RpcTest_WCFServer.IHelloword proxy = channel.CreateChannel();
            String user = "world";
            CodeTimerAdvance.TimeByConsole("wcf http", _count, a =>
            {
                var reply = proxy.SayHello(new RpcTest_WCFServer.HelloRequest { Name = user }).Result;
            }, _threadcount);
        }

        static void RunWcfTcp()
        {
            string address = "net.tcp://localhost:8002/";
            ChannelFactory<RpcTest_WCFServer.IHelloword> channel =
                new ChannelFactory<RpcTest_WCFServer.IHelloword>(
                    new NetTcpBinding(),
                    new EndpointAddress(
                        new Uri(address)));
            RpcTest_WCFServer.IHelloword proxy = channel.CreateChannel();
            String user = "world";
            CodeTimerAdvance.TimeByConsole("wcf tcp", _count, a =>
            {
                var reply = proxy.SayHello(new RpcTest_WCFServer.HelloRequest { Name = user }).Result;
            }, _threadcount);
        }

        static void RunThriftBio()
        {
            //TTransport transport = new TSocket("localhost", 7911);
            //TProtocol protocol = new TBinaryProtocol(transport);
            //Helloword.Client client = new Helloword.Client(protocol);
            //transport.Open();
            //String user = "world";
            //CodeTimerAdvance.TimeByConsole("thrift bio", _count, a =>
            //{
            //    var reply = client.SayHello(new HelloRequest { Name = user });
            //}, _threadcount);
            //transport.Close();


            //多线程不能复用TSocket故需每次实例化。
            String user = "world";
            CodeTimerAdvance.TimeByConsole("thrift bio", _count, a =>
            {
                TTransport transport = new TSocket("localhost", 7911);
                transport.Open();
                TProtocol protocol = new TBinaryProtocol(transport);
                using (Helloword.Client client = new Helloword.Client(protocol))
                {
                    var reply = client.SayHello(new HelloRequest { Name = user });
                }
                transport.Close();
            }, _threadcount);

        }

        static void RunHttpClient()
        {
            var httpClient = new HttpClient();
            CodeTimerAdvance.TimeByConsole("httpclient", _count, a =>
            {
                var response = httpClient.PostAsync("http://localhost:23455/api/Values/SayHello", new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    {"Name", "world"},
                }));
                var res = response.Result.Content.ReadAsStringAsync().Result;
            }, _threadcount);
        }

        static void RunHttpWebRequest()
        {
            string url = "http://localhost:23455/api/Values/SayHello";
            CodeTimerAdvance.TimeByConsole("HttpWebRequest", _count, a =>
            {
                HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(new Uri(url));
                webRequest.Method = "post";
                webRequest.ContentType = "application/x-www-form-urlencoded";
                string postData = "Name=world";
                byte[] byteArray = Encoding.UTF8.GetBytes(postData);
                webRequest.ContentLength = byteArray.Length;
                System.IO.Stream newStream = webRequest.GetRequestStream();
                newStream.Write(byteArray, 0, byteArray.Length);
                newStream.Close();
                HttpWebResponse response = (HttpWebResponse)webRequest.GetResponse();
                string res = new System.IO.StreamReader(response.GetResponseStream(), Encoding.GetEncoding("utf-8")).ReadToEnd();
            }, _threadcount);
        }

        static async void RunNetty()
        {
            var group = new MultithreadEventLoopGroup();
            string targetHost = null;
            try
            {
                var bootstrap = new Bootstrap();
                bootstrap
                    .Group(group)
                    .Channel<TcpSocketChannel>()
                    .Option(DotNetty.Transport.Channels.ChannelOption.TcpNodelay, true)
                    .Handler(new ActionChannelInitializer<ISocketChannel>(channel =>
                    {
                        IChannelPipeline pipeline = channel.Pipeline;
                        //pipeline.AddLast(new LoggingHandler());
                        pipeline.AddLast("framing-enc", new LengthFieldPrepender(2));
                        pipeline.AddLast("framing-dec", new LengthFieldBasedFrameDecoder(ushort.MaxValue, 0, 2, 0, 2));

                        pipeline.AddLast("echo", new EchoClientHandler());
                    }));

                IChannel clientChannel = await bootstrap.ConnectAsync(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 8007));

                Console.ReadLine();

                await clientChannel.CloseAsync();
            }
            finally
            {
                await group.ShutdownGracefullyAsync(TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(1));
            }
        }

        static void RunOrleans()
        {
            var config = ClientConfiguration.LocalhostSilo(26224);
            try
            {
                GrainClient.Initialize(config);
                Console.WriteLine("Client successfully connect to silo host");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Orleans client initialization failed failed due to {ex}");

                Console.ReadLine();
                return;
            }

            var friend = GrainClient.GrainFactory.GetGrain<IHello>(0);
            var name = "world";
            CodeTimerAdvance.TimeByConsole("orleans", _count, (i) =>
            {
                var response = friend.SayHello(new RpcTest_OrleansServer.Interfaces.HelloRequest
                {
                    Name = name
                }).Result;
            }, _threadcount);

            Console.WriteLine("Press Enter to terminate...");
            Console.ReadLine();
        }
    }
}
