using System.Data.Entity;

namespace Checkitlink.Models.Data
{
    //Контест БД 
    public class ChekitDB : DbContext
    {
        public DbSet<UsersDTO> Users { get; set; } //пользователи
        public DbSet<BlackListDTO> BlackList { get; set; } //черный список юзеров
        public DbSet<RoleDTO> Roles { get; set; } //роли
        public DbSet<UserRoleDTO> UserRoles { get; set; } //список ролей юзеров
        public DbSet<LinkDTO> Links { get; set; } //закладки
        public DbSet<CategoriesDTO> Categories { get; set; } // категории закладок
        public DbSet<LinkLikeStatusDTO> LinkLikeStatus { get; set; } //лайк/дизлайк
        public DbSet<SubscribeOnUserDTO> SubscribeOnUserDTO { get; set; } //подписчики
        public DbSet<BannedSiteDTO> BannedSiteDTO { get; set; } //заперщенные для публикации в публичный доступ ресурсы
    }
}