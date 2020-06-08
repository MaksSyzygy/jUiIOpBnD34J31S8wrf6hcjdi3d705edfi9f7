using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Checkitlink.Models.Data
{
    //Data Transfer Object, далее DTO, для связи с БД
    [Table("tblBlackList")]//тут и далее явно указываем таблицу в БД
    public class BlackListDTO//DTO для заблокированных юзеров
    {
        [Key]//первичный ключ
        public int Id { get; set; }//свойства
        public int UserId { get; set; }
        public string Login { get; set; }
        public string Email { get; set; }
        public string Reason { get; set; }
    }
}