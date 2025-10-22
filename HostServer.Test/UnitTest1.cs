using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace HostServer.Test
{
    [TestClass]
    public class HostServerTests
    {
        [TestMethod]
        public void ObtenerMensajeServidorTrue()
        {
            string esperado = "Servidor iniciado en net.tcp://localhost:8091/TrucoServiceBase  http://localhost:8080/TrucoServiceBase";
            string resultado = Program.ObtenerMensajeServidor();
            Assert.AreEqual(esperado, resultado);
        }
    }
}
