using McMaster.Extensions.CommandLineUtils;
using System.Threading.Tasks;

namespace tmxtract
{

    class Program
    {
        static async Task Main(string[] args)
        {
            await CommandLineApplication.ExecuteAsync<Cli>(args);
        }
    }
}
