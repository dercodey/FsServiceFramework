using TrendingManager;
using TrendingManager.Contracts;
using System;
using System.ServiceModel;

namespace TestHost
{
    class Program
    {
        static void Main(string[] args)
        {
            var baseAddress = new UriBuilder("net.tcp", "localhost", -1, typeof(ITrendingManager).Name);

            ServiceHost host = new ServiceHost(typeof(TrendingManagerService), baseAddress.Uri);
            var serviceEndpoint = host.AddServiceEndpoint(typeof(ITrendingManager), new NetTcpBinding(), string.Empty);
            host.Open();

            using (var factory = new ChannelFactory<ITrendingManager>(serviceEndpoint))
            {
                var proxy = factory.CreateChannel();
                var proxyObj = proxy as object;
                var result = proxy.GetSeries(0);
                Console.WriteLine("proxy.GetSeries({0}) = {1}", 0, result);
            }

            Console.ReadLine();
            host.Close();
        }
    }
}
