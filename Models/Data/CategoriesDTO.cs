using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Checkitlink.Models.Data
{
    //DTO для категорий закладок
    [Table("tblCategories")]
    public class CategoriesDTO
    {
        [Key]
        public int CategoryId { get; set; }
        public string CategoryName { get; set; }
        public int UserCategory { get; set; }
    }
}