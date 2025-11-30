using System;
using System.ServiceModel;
using TrucoServer.Services;

namespace HostServer
{
    internal static class Program
    {
        static void Main(string[] args)
        {
            using (ServiceHost host = new ServiceHost(typeof(TrucoServer.Services.TrucoServer)))
            {
                host.Open();

                Console.WriteLine("Servidor iniciado en net.tcp://localhost:8091/TrucoServiceBase  http://localhost:8080/TrucoServiceBase");
                Console.ReadLine();
            }
        }
    }
}

