using Checkitlink.Models.Data;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Checkitlink.Models.ViewModels
{
    //модель для Черного списка
    public class BlackListVM
    {
        public int Id { get; set; }
        public int UserId { get; set; }

        [DisplayName("Логин")]
        public string Login { get; set; }

        [DisplayName("Email")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Укажите причину блокировки")]
        [DisplayName("Причина блокировки")]
        public string Reason { get; set; }

        public BlackListVM(BlackListDTO row)//сохраняем в DTO, а далее по цепочке данные уходят в БД
        {
            Id = row.Id;
            UserId = row.UserId;
            Login = row.Login;
            Email = row.Email;
            Reason = row.Reason;
        }

        public BlackListVM() { }
    }
}