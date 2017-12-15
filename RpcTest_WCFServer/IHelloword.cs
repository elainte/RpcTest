using System.ServiceModel;
using System.Threading.Tasks;

namespace RpcTest_WCFServer
{
    [ServiceContract]
    public interface IHelloword
    {
        [OperationContract]
        Task<HelloReply> SayHello(HelloRequest helloRequest);
    }

    public class HelloRequest
    {
        public string Name { get; set; }
    }

    public class HelloReply
    {
        public string Message { get; set; }
    }
}
