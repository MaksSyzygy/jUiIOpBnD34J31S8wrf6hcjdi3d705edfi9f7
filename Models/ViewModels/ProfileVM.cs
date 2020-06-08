using Checkitlink.Models.Data;
using System.Collections.Generic;
using System.Web.Mvc;
using PagedList;

namespace Checkitlink.Models.ViewModels
{
    //Универсальная модель, благодаря которой есть возможность использовать больше одной модели на странице
    //и использовать ассинхронные методы в частичных представлениях, хотя mvc 5 этого не умеет. Я нашел выход из ситуации
    //такой реализацией
    //Включает в себя как DTO, так и модели
    public class ProfileVM
    {
        public IPagedList<LinkVM> LinkListUserProfile { get; set; }
        public IPagedList<LinkVM> AllLinks { get; set; }
        public List<LinkVM> AllUserLinks { get; set; }
        public List<CategoryVM> CategoryProfileList { get; set; }
        public LinkVM UserLink { get; set; }
        public IEnumerable<SelectListItem> CategoryList { get; set; }
        public LinkLikeStatusVM LinkLikeStatus { get; set; }
        public List<SubscribeOnUserVM> SubscribeOnUserList { get; set; }
        public IPagedList<UserVM> UsersLeadList { get; set; }
        public UserProfileVM UserVM { get; set; }
        public UserVM UserInfo { get; set; }
        public List<LinkVM> MyLinks { get; set; }
        public BlackListVM BlackListUserVM { get; set; }
        public List<BlackListVM> ListBlackListUsersVM { get; set; }
        public List<UserVM> UsersList { get; set; }
        public LoginVM LoginVM { get; set; }
        public List<BannedSiteVM> BannedSiteVM { get; set; }
    }

    public class ProfileDTO
    {
        public UsersDTO UsersProfileDTO { get; set; }
        public List<UsersDTO> UsersListDTO { get; set; }
        public LinkDTO UserLinkDTO { get; set; }
        public List<LinkDTO> LinkListUserProfileDTO { get; set; }
        public List<CategoriesDTO> CategoryProfileListDTO { get; set; }
        public CategoriesDTO UserCategoryDTO { get; set; }
        public LinkLikeStatusDTO LikeStatusDTO { get; set; }
        public List<SubscribeOnUserDTO> SubscribeOnUserListDTO { get; set; }
        public BlackListDTO BlackListUserDTO { get; set; }
        public List<BlackListDTO> ListBlackListUsersDTO { get; set; }
        public List<LinkDTO> AllLinks { get; set; }
        public List<BannedSiteDTO> BannedSiteListDTO { get; set; }
    }
}