using BTL_WEB_MVC.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ShopMobile.Data;
using ShopMobile.Models;
using ShopMobile.Services;
using System.Collections.Generic;
using System.Drawing.Printing;
using System.Security.Cryptography;

namespace ShopMobile.Controllers
{
    public class ProductController : Controller
    {
        private ShopShoseDbContext db;
        public static int limit { get; } = 5;

        public ProductController(ShopShoseDbContext db)
        {
            this.db = db;
        }

        public IActionResult Index(int page = 1)
        {

            int count = db.Products.Count();
            int totalPage = (int)Math.Ceiling((decimal)count / limit);
            int offset = (page - 1) * limit;
            IEnumerable<Product> products = db.Products.OrderBy(p => p.ProductId).Include(c => c.Category).Skip(offset).Take(limit).ToList();

            this.ViewBag.totalPage = totalPage;
            this.ViewBag.CurrentPage = page;
            return View(products);
        }

        public IActionResult GetAllPanigationProduct(int page = 1)
        {
     
            int offset = (page - 1) * limit;    
            IEnumerable<Product> products = db.Products.OrderBy(p => p.ProductId).Include(c => c.Category).Skip(offset).Take(limit).ToList();

            int count = db.Products.Count();
            int totalPage = (int)Math.Ceiling((decimal)count / limit);

            this.ViewBag.totalPage = totalPage;
            this.ViewBag.CurrentPage = page;
            return PartialView("ListProductTable", products);
        }

        public IActionResult ProductFilter(int? categoryId, string? keyword, int? pageIndex)
        {
            IQueryable<Product> products = db.Products.OrderBy(p => p.ProductId); 

            int page = (int)(pageIndex == null || pageIndex <= 0 ? 1 : pageIndex);

            if (categoryId != null && categoryId != 0)
            {
              
                products = products.Where(p => p.CategoryId == categoryId);
                ViewBag.categoryId = categoryId;
            }

            if (keyword != null)
            {
                products = products.Where(l => l.title.ToLower().Contains(keyword.ToLower().Trim()));
                ViewBag.keyword = keyword;
            }

            int count = products.Count(); 
            int totalPage = (int)Math.Ceiling((decimal)count / limit);

            int offset = (page - 1) * limit;

            this.ViewBag.totalPage = totalPage;
            this.ViewBag.CurrentPage = page;

    
            var result = products.Skip(offset).Take(limit).Include(p => p.Category).ToList();

            return PartialView("ListProductTable", result);
        }



        public IActionResult Create()
        {
            ViewBag.CategoryId = new SelectList(db.Categories, "CategoryId", "CategoryName");

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Product product)
        {
            User user = AuthController.CheckAuthentication(db, HttpContext);
            if (user == null)
            {
                TempData["error"] = "Create product deny permission";
                return View("Create");

            }

            product.UserId = user.UserId;

            if(ModelState.IsValid)
            {
                db.Products.Add(product);
                db.SaveChanges();
                TempData["success"] = "Create a new product successfully";

                return RedirectToAction("Index");
            } else
            {
                ViewBag.CategoryId = new SelectList(db.Categories, "CategoryId", "CategoryName");

                return View("Create");
            }
                


            
          /*  TempData["error"] = "Create a new product false";
            ViewBag.CategoryId = new SelectList(db.Categories, "CategoryId", "CategoryName");
            return View("Create");*/
        }

       


        public IActionResult Edit(int id) 
        {
            if(id == null || db.Products == null)
            {
                return NotFound();
            } 
            
             Product product = ProductService.getOneById(db, id);
            if (product == null)
            {
                return NotFound();
            }
            ViewBag.CategoryId = new SelectList(db.Categories, "CategoryId", "CategoryName");
            return View(product);
                      
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Product product)
        {
            User user = AuthController.CheckAuthentication(db, HttpContext);
            if (user == null)
            {
                TempData["error"] = "You need to login";
                ViewBag.CategoryId = new SelectList(db.Categories, "CategoryId", "CategoryName");
                return View("Edit");
            }

            if (product.ProductId == null) return NotFound();
            
                try
                {
                    product.UserId = user.UserId;
                    db.Update(product);
                    db.SaveChanges();
                } catch (DbUpdateConcurrencyException)
                {
                    return  NotFound(); 
                }
            return RedirectToAction("Index");
            


        }


        [HttpPost]
        public IActionResult Delete(int id)
        {
            
            Product product = db.Products.Where(i => i.ProductId == id).FirstOrDefault();
            if (product == null)
            {
                return Json ( new { success = false} );
            }
            List<Invoice_Products> ips = db.Invoice_Products.Where(ip => ip.ProductId == product.ProductId).ToList();

            foreach (var ip in ips)
            {
                db.Invoice_Products.Remove(ip);
            }

            List<int> invoiceIdsToDelete = ips.Select(ip => ip.InvoiceId).Distinct().ToList();

            foreach (var invoiceId in invoiceIdsToDelete)
            {
                if (db.Invoice_Products.Where(i => i.InvoiceId == invoiceId).Count() == 0)
                {
                    Invoice invoice = db.Invoices.Where(i => i.InvoiceId == invoiceId).FirstOrDefault();

                    db.Invoices.Remove(invoice);
                }

            }




            db.Products.Remove(product);
            db.SaveChanges();

            return Json(new { success = true });


        }

        [HttpPost]
        public IActionResult Delete1(int id)
        {
            Product product = db.Products.Where(i => i.ProductId == id).FirstOrDefault();
            if (product == null)
            {
                return Json(new { success = false });
            }

            List<Invoice_Products> ips = db.Invoice_Products.Where(ip => ip.ProductId == product.ProductId).ToList();


            foreach (var ip in ips)
            {
                db.Invoice_Products.Remove(ip);
            }
                db.SaveChanges();

            

            List<int> invoiceIdsToDelete = ips.Select(ip => ip.InvoiceId).Distinct().ToList();

            foreach (var invoiceId in invoiceIdsToDelete)
            {

                if(db.Invoice_Products.Where(i => i.InvoiceId == invoiceId).Count() == 0)
                {
                Invoice invoice = db.Invoices.Where(i => i.InvoiceId == invoiceId).FirstOrDefault();

                    db.Invoices.Remove(invoice);
                }
                
            }

            // Xóa sản phẩm
            db.Products.Remove(product);
            db.SaveChanges();

            return Json(new { success = true });
        }


    }
}
