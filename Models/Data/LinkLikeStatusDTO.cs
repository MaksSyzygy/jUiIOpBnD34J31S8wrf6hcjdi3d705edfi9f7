using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Checkitlink.Models.Data
{
    //DTO для лайков/дизлайков
    [Table("tblLinkLikeStatus")]
    public class LinkLikeStatusDTO
    {
        [Key]
        public int LinkListNumber { get; set; }
        public int LinkId { get; set; }
        public bool LikeStatus { get; set; }
        public int LikeAuthorId { get; set; }
    }
}