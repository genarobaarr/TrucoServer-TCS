using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using System.Text;
using TrucoServer.Data.DTOs;
using TrucoServer.Utilities;

namespace TrucoServer.Helpers.Mapping
{
    public class UserMapper : IUserMapper
    {
        private const string DEFAULT_AVATAR_ID = "avatar_aaa_default";
        private const string DEFAULT_LANG_CODE = "es-MX";

        public UserProfileData MapUserToProfileData(User user)
        {
            SocialLinks links = new SocialLinks();

            if (user.UserProfile?.socialLinksJson != null)
            {
                string json = Encoding.UTF8.GetString(user.UserProfile.socialLinksJson);
                links = JsonConvert.DeserializeObject<SocialLinks>(json) ?? new SocialLinks();
            }

            return new UserProfileData
            {
                Username = user.username,
                Email = user.email,
                AvatarId = user.UserProfile?.avatarID ?? DEFAULT_AVATAR_ID,
                NameChangeCount = user.nameChangeCount,
                FacebookHandle = links?.FacebookHandle ?? "",
                XHandle = links?.XHandle ?? "",
                InstagramHandle = links?.InstagramHandle ?? "",
                LanguageCode = user.UserProfile?.languageCode ?? DEFAULT_LANG_CODE,
                IsMusicMuted = user.UserProfile?.isMusicMuted ?? false
            };
        }

        public List<MatchScore> FetchLastMatchesForUser(baseDatosTrucoEntities context, int userID)
        {
            return context.MatchPlayer
                .Where(mp => mp.userID == userID)
                .Select(mp => new { MatchPlayer = mp, Match = mp.Match })
                .Where(join => join.Match.status == "Finished" && join.Match.endedAt.HasValue)
                .OrderByDescending(join => join.Match.endedAt)
                .Take(5)
                .Select(join => new MatchScore
                {
                    MatchID = join.Match.matchID.ToString(),
                    EndedAt = join.Match.endedAt.Value,
                    IsWin = join.MatchPlayer.isWinner,
                    FinalScore = join.MatchPlayer.score
                })
                .ToList();
        }
    }
}
