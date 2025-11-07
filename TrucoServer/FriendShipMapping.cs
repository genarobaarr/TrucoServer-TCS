using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TrucoServer
{
    [MetadataType(typeof(FriendShipMapping))]
    public partial class Friendship
    {
    }

    public class FriendShipMapping
    {
        [Column("userID1")]
        public int userID { get; set; }

        [Column("userID2")]
        public int friendID { get; set; }
    }
}
