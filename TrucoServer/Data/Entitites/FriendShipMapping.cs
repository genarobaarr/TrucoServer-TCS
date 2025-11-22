using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TrucoServer.Data.Entities
{
    [MetadataType(typeof(FriendShipMapping))]
    public partial class Friendship
    {
    }

    public class FriendShipMapping
    {
        [Column("userID1")]
        public int UserID { get; set; }

        [Column("userID2")]
        public int FriendID { get; set; }
    }
}
