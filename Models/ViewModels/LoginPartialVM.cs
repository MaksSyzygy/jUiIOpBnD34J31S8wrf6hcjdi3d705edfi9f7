using System.ComponentModel;

namespace Checkitlink.Models.ViewModels
{
    //для частичного представления в углу экрана
    public class LoginPartialVM
    {
        [DisplayName("Логин")]
        public string Login { get; set; }

        public int UserId { get; set; }

        public string Avatar { get; set; }
    }
}