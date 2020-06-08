using Checkitlink.Models.Data;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Checkitlink.Models.ViewModels
{
    //модель, для которой не требуется пароль
    public class UserProfileVM
    {
        public int UserId { get; set; }

        [Required(ErrorMessage = "Укажите логин")]
        [StringLength(30, MinimumLength = 3, ErrorMessage = "Минимальная длина 3 символа, максимальная 30")]
        [DisplayName("Логин")]
        public string Login { get; set; }

        [Required(ErrorMessage = "Укажите Email")]
        [RegularExpression(@"[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,4}", ErrorMessage = "Некорректный Email адрес")]
        [DisplayName("Email адрес")]
        public string Email { get; set; }

        [DisplayName("Аватар")]
        public string AvatarName { get; set; }

        public UserProfileVM(UsersDTO row)
        {
            UserId = row.UserId;
            Login = row.Login;
            Email = row.Email;
            AvatarName = row.AvatarName;
        }

        public UserProfileVM() { }
    }
}