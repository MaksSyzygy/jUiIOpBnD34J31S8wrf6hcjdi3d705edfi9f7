using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Checkitlink.Models.Data
{
    //DTO подписки на пользователя
    [Table("tblSubscribeOnUser")]
    public class SubscribeOnUserDTO
    {
        [Key]
        public int FollowNumber { get; set; }
        public int UserSubscriber { get; set; }
        public int LeadUser { get; set; }
        public bool SubscribeStatus { get; set; } = false;
    }
}