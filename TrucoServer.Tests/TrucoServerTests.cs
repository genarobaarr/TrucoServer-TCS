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
        public void RequestEmailVerificationReturnTrue()
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
        public void ConfirmEmailVerificationWithWrongCodeReturnFalse()
        {
            server.RequestEmailVerification("test@example.com", "en");
            var result = server.ConfirmEmailVerification("test@example.com", "000000");
            Assert.IsFalse(result);
        }
        //Validar usuario y email
        [TestMethod]
        public void UsernameExistsReturnFalse()
        {
            var result = server.UsernameExists("");
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void EmailExistsReturnFalse()
        {
            var result = server.EmailExists("");
            Assert.IsFalse(result);
        }
        //Guardar perfil de usuario
        [TestMethod]
        public void SaveNullUserProfileReturnFalse()
        {
            var result = server.SaveUserProfile(null);
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void SaveUserProfileEmptyEmailReturnFalse()
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
