using System.Collections.Generic;
using System.Threading.Tasks;

namespace RpcTest_OrleansServer.Interfaces
{
    /// <summary>
    /// Orleans grain communication interface that will save all greetings
    /// </summary>
    public interface IHelloArchive : Orleans.IGrainWithIntegerKey
    {
        Task<string> SayHello(string greeting);

        Task<IEnumerable<string>> GetGreetings();
    }
}
