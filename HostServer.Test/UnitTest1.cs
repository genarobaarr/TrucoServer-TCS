using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace HostServer.Test
{
    [TestClass]
    public class HostServerTests
    {
        // Mientras el metodo GetServerMessage sea static no se puede testear este metodo
        /*
        [TestMethod]
        public void GetServerMessageTrue()
        {
            string expected = "Servidor iniciado en net.tcp://localhost:8091/TrucoServiceBase  http://localhost:8080/TrucoServiceBase";
            string result = Program.GetServerMessage();
            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void GetServerMessageFalse()
        {
            string unexpected = "Servidor iniciado en net.tcp://localhost:8091/TrucoServiceBase";
            string result = Program.GetServerMessage();
            Assert.AreNotEqual(unexpected, result,  "El mensaje no debería coincidir");
        }*/
    }
}
