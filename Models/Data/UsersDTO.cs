using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Checkitlink.Models.Data
{
    //DTO юзера
    [Table("tblUsers")]
    public class UsersDTO
    {
        [Key]
        public int UserId { get; set; }
        public string Login { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string AvatarName { get; set; }
        public int LinksCount { get; set; }
        public bool BanStatus { get; set; } = false;
        public string Role { get; set; }
    }
}