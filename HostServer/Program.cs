using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using TrucoServer;

namespace HostServer
{
    public class Program
    {
        public static string GetServerMessage()
        {
            return "Servidor iniciado en net.tcp://localhost:8091/TrucoServiceBase  http://localhost:8080/TrucoServiceBase";
        }

        public static string StartServer()
        {
            using (ServiceHost host = new ServiceHost(typeof(TrucoServer.TrucoServer)))
            {
                host.Open();
                Console.WriteLine(GetServerMessage());
                return GetServerMessage();
            }
        }

        static void Main(string[] args)
        {
            StartServer();
            Console.ReadLine();
        }
    }
}

/*
 * Así se encontraba antes el server
namespace HostServer
{
    internal class Program // Solamente para prueba se hace public
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
*/