using System.Threading.Tasks;

namespace RpcTest_OrleansServer.Interfaces
{
    /// <summary>
    /// Orleans grain communication interface IHello
    /// </summary>
    public interface IHello : Orleans.IGrainWithIntegerKey
    {
        Task<HelloReply> SayHello(HelloRequest greeting);
    }
}
