using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using TrucoServer;

namespace HostServer
{
    internal class Program
    {
        static void Main(string[] args)
        {
            using (ServiceHost host = new ServiceHost(typeof(TrucoServer.TrucoServer)))
            { 
                host.Open();
                Console.WriteLine("Servidor iniciado en net.tcp://localhost:8091/TrucoServiceBase  http://localhost:8080/TrucoServiceBase");
                Console.ReadLine();
            }
        }
    }
}

