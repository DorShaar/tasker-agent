using Microsoft.Extensions.Hosting;
using System;
using System.Threading.Tasks;
using TaskerAgent.Infra.Extensions;

namespace TaskerAgent
{
    internal static class Program
    {
        private static async Task Main()
        {
            Console.WriteLine("Hello Tasker Agent!");

            IHostBuilder builder = new HostBuilder().ConfigureServices((_, services) => services.UseDI());

            await builder.RunConsoleAsync().ConfigureAwait(false);
        }
    }
}