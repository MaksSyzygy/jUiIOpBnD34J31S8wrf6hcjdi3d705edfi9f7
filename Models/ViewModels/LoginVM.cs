using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Checkitlink.Models.ViewModels
{
    //логин
    public class LoginVM
    {
        [Required(ErrorMessage = "Введите логин")]
        [DisplayName("Логин")]
        public string UserName { get; set; }

        [Required(ErrorMessage = "Введите пароль")]
        [DisplayName("Пароль")]
        public string Password { get; set; }

        [DisplayName("Оставаться в системе")]
        public bool RememberMy { get; set; }
    }
}