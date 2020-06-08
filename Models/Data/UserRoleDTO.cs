using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Checkitlink.Models.Data
{
    //формирование ролей
    [Table("tblUserRoles")]
    public class UserRoleDTO
    {
        [Key, Column(Order = 0)]
        public int UserId { get; set; }

        [Key, Column(Order = 1)]
        public int RoleId { get; set; }

        [ForeignKey("UserId")]
        public virtual UsersDTO UserKey { get; set; }

        [ForeignKey("RoleId")]
        public virtual RoleDTO RoleUser { get; set; }
    }
}