using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Checkitlink.Models.Data
{
    //роли пользователей сайта
    [Table("tblRoles")]
    public class RoleDTO
    {
        [Key]
        public int Id { get; set; }
        public string RoleName { get; set; }
    }
}