using BTL_WEB_MVC.Controllers;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ShopMobile.Data;
using ShopMobile.Models;
using ShopMobile.Services;
using System.Collections.Generic;
using System.Diagnostics;

namespace ShopMobile.Controllers
{
    public class HomeController : Controller
    {
        public static int limit = 6;


        private ShopShoseDbContext db;

        public HomeController(ShopShoseDbContext db)
        {
            this.db = db;
        }
        public IActionResult Index()
        {
            IEnumerable<Product> products = db.Products.Where(p => p.quantity > 0).OrderByDescending(p => p.ProductId).Take(limit).ToList();
            return View(products);
        }

        public IActionResult GetLatestProductWithPanigation(int cursorId, int totalProductCurrent)
        {
            int totalProduct = db.Products.Count();
            if (totalProductCurrent < totalProduct)
            {
                IEnumerable<Product> products = db.Products
                                         .OrderByDescending(p => p.ProductId)
                                         .Where(p =>  p.ProductId <= cursorId && p.quantity > 0)
                                         .Take(limit)
                                         .ToList();

                return PartialView("ListLatestProduc", products);
            }



            return Json(new { HideLoadMore = true });
            
        }

        public IActionResult Shop(int page = 1)
        {
            IEnumerable<Product> products = db.Products.ToList();
        
            int offset = (page - 1) * limit;

            int count = products.Count();

            int totalPage = (int)Math.Ceiling((decimal)count / limit);

            products = products.Where(p => p.quantity > 0).Skip(offset).Take(limit).ToList();

            ViewBag.TotoalCountProduct = count;

            ViewBag.Page_active = page;

            ViewBag.TotalPage = totalPage;
            
            return View(products);
        }

        public IActionResult GetAllProductPanigation( int page )
        {

            IEnumerable<Product> products = db.Products.ToList();



            int offset = (page - 1) * limit;
            int count = products.Count();
            int totalPage = (int)Math.Ceiling((decimal)count / limit);
             products = db.Products.Where(p => p.quantity > 0).Skip(offset).Take(limit).ToList();
            ViewBag.TotalPage = totalPage;
            ViewBag.Page_active = page;
            return PartialView("ListProduct", products);
        }


            
   


        public IActionResult ShopSingle(int id)
        {
            Product product = db.Products.Where(p => p.ProductId == id).Include(p => p.Category).FirstOrDefault();
            return View(product);
        }

        public IActionResult ListSimilarProduct(int productId,int category)
        {
            IEnumerable<Product> products = db.Products.Where(p =>  p.ProductId != productId && p.quantity > 0 && p.CategoryId == category).OrderBy(r => Guid.NewGuid()).Take(3).ToList();

            return PartialView("ListSimilarProducts", products);
        }




    }

    public class CategoryData
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; }
        public int ProductCount { get; set; }
    }
}


