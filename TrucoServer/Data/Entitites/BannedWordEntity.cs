using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TrucoServer.Data.Entities
{
    [Table("BannedWord")]
    public class BannedWordEntity
    {
        [Key]
        [Column("wordID")]
        public int WordID { get; set; }

        [Required]
        [Column("word")]
        [StringLength(50)]
        public string Word { get; set; }
    }
}
