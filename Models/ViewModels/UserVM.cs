using Checkitlink.Models.Data;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Checkitlink.Models.ViewModels
{
    //модель юзера
    public class UserVM
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

        [Required(ErrorMessage = "Укажите Ваш пароль")]
        [DataType(DataType.Password)]
        [DisplayName("Пароль")]
        [StringLength(20, MinimumLength = 3, ErrorMessage = "Минимальная длина пароля 3 символа, максимальная 20")]
        public string Password { get; set; }

        [Required(ErrorMessage = "Введите подтверждение пароля")]
        [Compare("Password", ErrorMessage = "Пароли не совпадают")]
        [DataType(DataType.Password)]
        [DisplayName("Подтверждение пароля")]
        public string ConfirmPassword { get; set; }

        [DisplayName("Аватар")]
        public string AvatarName { get; set; }

        [DisplayName("Кол-во ссылок")]
        public int LinksCount { get; set; }

        [DisplayName("Статус")]
        public bool BanStatus { get; set; } = false;

        [DisplayName("Роль")]
        public string Role { get; set; }

        public UserVM(UsersDTO row)
        {
            UserId = row.UserId;
            Login = row.Login;
            Email = row.Email;
            Password = row.Password;
            AvatarName = row.AvatarName;
            LinksCount = row.LinksCount;
            BanStatus = row.BanStatus;
            Role = row.Role;
        }

        public UserVM() { }

        public string SearchInfo()
        {
            return $"{Login} {Email}";
        }
    }
}