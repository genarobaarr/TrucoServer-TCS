using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrucoServer.Tests
{
    [TestClass]
    public class TrucoServerTests
    {
        private TrucoServer server;
        [TestInitialize]
        public void Setup()
        {
            server = new TrucoServer();
        }
        //Para el email
        [TestMethod]
        public void RequestEmailVerification_ReturnTrue()
        {
            var result = server.RequestEmailVerification("test@example.com", "en");
            Assert.IsTrue(result);
        }

        /*[TestMethod]
        public void ConfirmEmailVerification_WithCorrectCode_ReturnTrue()
        {
        
        }
        */
        [TestMethod]
        public void ConfirmEmailVerification_WithWrongCode_ReturnFalse()
        {
            server.RequestEmailVerification("test@example.com", "en");
            var result = server.ConfirmEmailVerification("test@example.com", "000000");
            Assert.IsFalse(result);
        }
        //Validar usuario y email
        [TestMethod]
        public void UsernameExists_ReturnFalse()
        {
            var result = server.UsernameExists("");
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void EmailExists_ReturnFalse()
        {
            var result = server.EmailExists("");
            Assert.IsFalse(result);
        }
        //Guardar perfil de usuario
        [TestMethod]
        public void SaveNullUserProfile_ReturnFalse()
        {
            var result = server.SaveUserProfile(null);
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void SaveUserProfile_EmptyEmail_ReturnFalse()
        {
            var profile = new UserProfileData { Email = "" };
            var result = server.SaveUserProfile(profile);
            Assert.IsFalse(result);
        }
        //Guardar avatar actualizado


        //Cambiar contraseña


        //Resto de métodos de prueba
    }
}
