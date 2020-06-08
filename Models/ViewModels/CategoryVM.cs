using Checkitlink.Models.Data;
using System.ComponentModel;

namespace Checkitlink.Models.ViewModels
{
    //Модель Категорий
    public class CategoryVM
    {
        public int CategoryId { get; set; }

        [DisplayName("Категория")]
        public string CategoryName { get; set; }

        public int UserCategory { get; set; }

        public CategoryVM(CategoriesDTO row)
        {
            CategoryId = row.CategoryId;
            CategoryName = row.CategoryName;
            UserCategory = row.UserCategory;
        }

        public CategoryVM() { }
    }
}