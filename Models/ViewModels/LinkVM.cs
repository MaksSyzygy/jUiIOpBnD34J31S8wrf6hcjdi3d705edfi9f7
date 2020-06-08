using Checkitlink.Models.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;

namespace Checkitlink.Models.ViewModels
{
    //модель закладки
    public class LinkVM
    {
        public int LinkID { get; set; }

        [DisplayName("Имя закладки")]
        public string LinkName { get; set; }

        [Required(ErrorMessage = "Вставьте ссылку для создания закладки")]
        [DisplayName("Адрес ссылки")]
        public string LinkAddress { get; set; }

        [DisplayName("Картинка")]
        public string LinkPicture { get; set; }

        [DisplayName("Категория закладки")]
        public int LinkCategoryId { get; set; }

        [DisplayName("Категория")]
        public string LinkCategory { get; set; }

        [DisplayName("Дата создания")]
        public DateTime CreatedAt { get; set; }

        [DisplayName("Описание")]
        public string LinkDescription { get; set; }

        public int UserAuthorId { get; set; }

        [DisplayName("Автор")]
        public string UserAuthor { get; set; }

        public bool PublicStatus { get; set; } = false;
        public bool FavoriteStatus { get; set; } = false;
        public int LikeCount { get; set; }

        public IEnumerable<SelectListItem> CategoryList { get; set; }//для выпадающего списка

        public LinkVM(LinkDTO row)
        {
            LinkID = row.LinkID;
            LinkName = row.LinkName;
            LinkAddress = row.LinkAddress;
            LinkPicture = row.LinkPicture;
            LinkCategoryId = row.LinkCategoryId;
            LinkCategory = row.LinkCategory;
            CreatedAt = row.CreatedAt;
            LinkDescription = row.LinkDescription;
            UserAuthorId = row.UserAuthorId;
            UserAuthor = row.UserAuthor;
            PublicStatus = row.PublicStatus;
            FavoriteStatus = row.FavoriteStatus;
            LikeCount = row.LikeCount;
        }

        public LinkVM() { }

        public string SearchLink()//метод для поиска закладки по данным критериям
        {
            return $"{LinkName} {LinkCategory} {UserAuthor}";
        }
    }
}