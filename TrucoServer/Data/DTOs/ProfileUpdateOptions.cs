using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrucoServer.Data.DTOs
{
    public class ProfileUpdateOptions
    {
        public UserProfileData ProfileData { get; set; }
        public string DefaultLanguageCode { get; set; }
        public string DefaultAvatarId { get; set; }
    }
}
