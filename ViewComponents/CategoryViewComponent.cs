using Microsoft.AspNetCore.Mvc;
using ShopMobile.Data;
using ShopMobile.Models;

namespace ShopMobile.ViewComponents
{
    public class CategoryViewComponent : ViewComponent
    {
        private ShopShoseDbContext db;
        IEnumerable<Category> categories;

        public CategoryViewComponent(ShopShoseDbContext db)
        {
            this.db = db;
            categories = db.Categories.ToList();
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            return View("RenderCategory", categories);
        }
    }
}
