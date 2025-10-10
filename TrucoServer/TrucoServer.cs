using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using TrucoServer;

namespace TrucoServer
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    public partial class TrucoServer : ITrucoUserService
    {
        private static ConcurrentDictionary<string, string> verificationCodes = new ConcurrentDictionary<string, string>();

        public bool RequestEmailVerification(string email)
        {
            try
            {
                string code = new Random().Next(100000, 999999).ToString();
                verificationCodes[email] = code;

                SendVerificationEmail(email, code);
                Console.WriteLine($"Código enviado a {email}: {code}");

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error enviando email: {ex.Message}");
                return false;
            }
        }

        public bool ConfirmEmailVerification(string email, string code)
        {
            if (verificationCodes.TryGetValue(email, out string storedCode))
            {
                if (storedCode == code)
                {
                    verificationCodes.TryRemove(email, out _); // eliminar tras confirmación
                    return true;
                }
            }
            return false;
        }

        private void SendVerificationEmail(string email, string code)
        {
            var fromAddress = new MailAddress("genaelcrack0409@gmail.com", "Truco Argentino");
            var toAddress = new MailAddress(email);
            const string fromPassword = "foos ssth gute ltnb";
            const string subject = "Código de verificación - Truco Argentino";
            string body = $"Tu código de verificación es: {code}";

            var smtp = new SmtpClient
            {
                Host = "smtp.gmail.com",
                Port = 587,
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(fromAddress.Address, fromPassword)
            };

            using (var message = new MailMessage(fromAddress, toAddress)
            {
                Subject = subject,
                Body = body
            })
            {
                smtp.Send(message);
            }
        }
        bool ITrucoUserService.Register(string username, string password, string email)
        {
            try
            {
                using (var context = new baseDatosPruebaEntities())
                {
                    User user = new User
                    {
                        nickname = username,
                        passwordHash = password,
                        email = email
                    };

                    context.User.Add(user);
                    context.SaveChanges();
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error al registrar usuario: " + ex.Message);
                return false;
            }
        }
        bool ITrucoUserService.Login(string username, string password)
        {
            throw new NotImplementedException();
        }
        void ITrucoUserService.Logout(string username)
        {
            throw new NotImplementedException();
        }
        List<PlayerStats> ITrucoUserService.GetGlobalRanking()
        {
            throw new NotImplementedException();
        }
        List<MatchResult> ITrucoUserService.GetLastMatches(string username)
        {
            throw new NotImplementedException();
        }

        List<string> ITrucoUserService.GetOnlinePlayers()
        {
            throw new NotImplementedException();
        }
    }

    public partial class TrucoServer : ITrucoFriendService
    {
        bool ITrucoFriendService.SendFriendRequest(string fromUser, string toUser)
        {
            throw new NotImplementedException();
        }

        void ITrucoFriendService.AcceptFriendRequest(string fromUser, string toUser)
        {
            throw new NotImplementedException();
        }
        List<string> ITrucoFriendService.GetFriends(string username)
        {
            throw new NotImplementedException();
        }
    }

    public partial class TrucoServer : ITrucoMatchService
    {
        string ITrucoMatchService.CreateMatch(string hostPlayer)
        {
            throw new NotImplementedException();
        }

        bool ITrucoMatchService.JoinMatch(string matchCode, string player)
        {
            throw new NotImplementedException();
        }

        void ITrucoMatchService.LeaveMatch(string matchCode, string player)
        {
            throw new NotImplementedException();
        }
        void ITrucoMatchService.PlayCard(string matchCode, string player, string card)
        {
            throw new NotImplementedException();
        }

        void ITrucoMatchService.SendChatMessage(string matchCode, string player, string message)
        {
            throw new NotImplementedException();
        }
    }
}