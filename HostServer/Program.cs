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
        public static string ObtenerMensajeServidor()
        {
            return "Servidor iniciado en net.tcp://localhost:8091/TrucoServiceBase  http://localhost:8080/TrucoServiceBase";
        }

        public static string IniciarServidor()
        {
            using (ServiceHost host = new ServiceHost(typeof(TrucoServer.TrucoServer)))
            {
                host.Open();
                Console.WriteLine(ObtenerMensajeServidor());
                return ObtenerMensajeServidor();
            }
        }

        static void Main(string[] args)
        {
            IniciarServidor();
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