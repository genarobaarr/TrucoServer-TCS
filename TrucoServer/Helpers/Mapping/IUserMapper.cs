using TrucoServer.Data.DTOs;
using System.Collections.Generic;

namespace TrucoServer.Helpers.Mapping
{
    public interface IUserMapper
    {
        UserProfileData MapUserToProfileData(User user);
        List<MatchScore> FetchLastMatchesForUser(int userID);
    }
}
