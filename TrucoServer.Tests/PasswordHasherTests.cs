using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrucoServer.Tests
{
    [TestClass]
    public class PasswordHasherTests
    {
        [TestMethod]
        public void HashReturnDifferentFromPassword()
        {
            string password = "chimalhuacanGOAT!";
            string hashed = PasswordHasher.Hash(password);
            Assert.AreNotEqual(password, hashed, "El hash no debe coincidir con la contraseña original");
        }

        [TestMethod]
        public void HashReturnNullFromPassword()
        {
            string password = "chimalhuacanGOAT!";
            string hashed = PasswordHasher.Hash(password);
            Assert.IsNotNull(hashed, "El hash no debe ser nulo");
        }

        [TestMethod]
        public void VerifyReturnTrueForCorrectPassword()
        {
            string password = "chimalhuacanGOAT!";
            string hash = PasswordHasher.Hash(password);
            bool isValid = PasswordHasher.Verify(password, hash);
            Assert.IsTrue(isValid, "La verificación debería ser verdadera para la contraseña correcta");
        }

        [TestMethod]
        public void VerifyReturnFalseForIncorrectPassword()
        {
            string password = "chimalhuacanGOAT!";
            string hash = PasswordHasher.Hash(password);
            bool isValid = PasswordHasher.Verify("trestristrestragos", hash);

            Assert.IsFalse(isValid, "La verificación debería ser falsa para la contraseña incorrecta");
        }

        [TestMethod]
        public void HashProduceDifferentHashesForSamePassword()
        {
            string password = "chimalhuacanGOAT!";

            string hash1 = PasswordHasher.Hash(password);
            string hash2 = PasswordHasher.Hash(password);
            Assert.AreNotEqual(hash1, hash2, "Cada hash debe ser único incluso para la misma contraseña");
        }

        [TestMethod]
        public void VerifyReturnFalseForMalformedHash()
        {
            string password = "chimalhuacanGOAT!";
            string malformedHash = "ABC123";
            bool isValid = false;
            try
            {
                isValid = PasswordHasher.Verify(password, malformedHash);
            }
            catch (FormatException)
            {
                isValid = false;
            }
            Assert.IsFalse(isValid, "No debe validarse un hash con formato incorrecto");
        }
    }
}
