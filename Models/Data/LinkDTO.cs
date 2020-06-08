using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Checkitlink.Models.Data
{
    //DTO для закладок
    [Table("tblLink")]
    public class LinkDTO
    {
        [Key]
        public int LinkID { get; set; }
        public string LinkName { get; set; }
        public string LinkAddress { get; set; }
        public string LinkPicture { get; set; }
        public int LinkCategoryId { get; set; }
        public string LinkCategory { get; set; }
        public DateTime CreatedAt { get; set; }
        public string LinkDescription { get; set; }
        public int UserAuthorId { get; set; }
        public string UserAuthor { get; set; }
        public bool PublicStatus { get; set; } = false;
        public bool FavoriteStatus { get; set; } = false;
        public int LikeCount { get; set; }

        [ForeignKey("LinkCategoryId")]
        public virtual CategoriesDTO Categories { get; set; }
    }
}