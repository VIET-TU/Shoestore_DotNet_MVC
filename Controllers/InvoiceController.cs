using BTL_WEB_MVC.Controllers;
using Humanizer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Routing.Template;
using Microsoft.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using NuGet.Protocol;
using ShopMobile.Data;
using ShopMobile.Models;
using System.ComponentModel.DataAnnotations;
using System.Drawing;
using System.Net;
using System.Net.Mail;

namespace ShopMobile.Controllers
{
    public class InvoiceController : Controller
    {
        private ShopShoseDbContext db;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public InvoiceController(ShopShoseDbContext db, IWebHostEnvironment webHostEnvironment)
        {
            this.db = db;
            _webHostEnvironment = webHostEnvironment;

        }


      

        public IActionResult Index()
        {

            User user = AuthController.CheckAuthentication(db, HttpContext);
            String a = new String("hello");
            String b = new String("hello");

            if (user == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            Invoice latestInvoice = db.Invoices
                               .Where(c => c.UserId == user.UserId && !c.Status)
                               .FirstOrDefault();
            if (latestInvoice == null)
            {
                latestInvoice = this.CreateNewInvoice(user.UserId);
            }

            IEnumerable<Invoice_Products> list_product_invoice = db.Invoice_Products.Where(i => i.InvoiceId == latestInvoice.InvoiceId).Include(i => i.Product).Include(p => p.Product.Category).ToList();
            int totalPrice = 0;
            foreach (var ip in list_product_invoice)
            {
                totalPrice += int.Parse(ip.totalPrice);
            }
            ViewBag.TotalPrice = totalPrice;
            return View(list_product_invoice);
        }

        private Invoice CreateNewInvoice(int userId)
        {
            Invoice newInvoice = new Invoice()
            {
                UserId = userId,
                Status = false,
            };
            db.Invoices.Add(newInvoice);
            db.SaveChanges();

            return newInvoice;
        }

        public IActionResult addToInvoice(int ProductId, int quantity, string size)
        {
            if (ModelState.IsValid)
            {
                User user = AuthController.CheckAuthentication(db, HttpContext);
                if (user == null)
                {
                  return  Redirect("Auth/Login");
                }

                Product info_product = db.Products.Where(p => p.ProductId == ProductId).FirstOrDefault();
                if (info_product == null)
                {
                    TempData["error"] = "Add a product to cart false";
                    return NotFound();
                }

                if (info_product.quantity < quantity)
                {
                    return Json(new { success = false, message = "Products sold through warehouses" });
                }

                Invoice latestInvoice = db.Invoices
                               .Where(c => c.UserId == user.UserId && !c.Status)
                               .FirstOrDefault();
                if (latestInvoice == null)
                {
                    latestInvoice = this.CreateNewInvoice(user.UserId);
                }


                var invoice_detail_exit = db.Invoice_Products
                             .Where(i => i.ProductId == ProductId && i.InvoiceId == latestInvoice.InvoiceId && i.size == size)
                             .FirstOrDefault();


                if (invoice_detail_exit != null)
                {
                    invoice_detail_exit.quantity += quantity;
                    invoice_detail_exit.totalPrice = (int.Parse(info_product.price) * invoice_detail_exit.quantity).ToString();
                }
                else
                {
                    db.Invoice_Products.Add(new Invoice_Products()
                    {
                        InvoiceId = latestInvoice.InvoiceId,
                        ProductId = ProductId,
                        quantity = quantity,
                        size = size,
                        totalPrice = (int.Parse(info_product.price) * quantity).ToString(),
                    });
                }


               



                db.SaveChanges();

                TempData["success"] = "Add a product to cart successfylly";

                return Json(new { success = true });
            }

            TempData["error"] = "Add a product to cart false";  
            return Json(new { success = false });

        }

        public IActionResult CardTotalQuantity(int userId)
        {

            
            Invoice latestInvoice = db.Invoices
                               .Where(c => c.UserId == userId && !c.Status)
                               .FirstOrDefault();
            int totalQuantity = 0;

            if (latestInvoice != null)
            {
                totalQuantity = db.Invoice_Products
                                  .Where(i => i.InvoiceId == latestInvoice.InvoiceId)
                                  .Sum(q => q.quantity);
            }



            return Json(new { totalQuantity }); ;
        }


        public IActionResult UpdateQuantityInvoice_Product(int invoiceId,int productId,string size,int quantity)
        {

             db.Invoice_Products.Where(i => i.InvoiceId == invoiceId && i.ProductId == productId && i.size.Equals(size)).FirstOrDefault();
                        
            return null;
        }


        [HttpPost]
        public IActionResult DeleteItemInvoiceProduct(int id)
        {
            Invoice_Products ip = db.Invoice_Products.Where(ip => ip.Id == id).FirstOrDefault();
            if (ip != null)
            {
                db.Invoice_Products.Remove(ip);
                db.SaveChanges();

                return Json(new { success = true });
            }

            return Json(new { success = false });
        }


        public IActionResult Checkout()

        {
            User user = AuthController.CheckAuthentication(db, HttpContext);

            if (user == null)
            {
                return RedirectToAction("Login", "Auth");
            }
            Invoice latestInvoice = db.Invoices.Where(c => c.UserId == user.UserId && !c.Status).FirstOrDefault();

            double totalPrice = 0;

            IEnumerable<Invoice_Products> ips = db.Invoice_Products.Where(ip => ip.InvoiceId == latestInvoice.InvoiceId).Include(p => p.Product).ToList();
            if(ips.Count() > 0)
            {
                foreach (var ip in ips)
                {
                    totalPrice += double.Parse(ip.totalPrice);
                }
                ViewBag.TotalPrice = totalPrice;
                ViewBag.User = user;
                ViewBag.Invoice = latestInvoice;
                ViewBag.ips = ips;
                return View();
            } 

            TempData["error"] = "Không có sảm phẩm nào trong giỏ hàng";
            return RedirectToAction("Shop", "Home");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Checkout(InforCheckOut inforCheckOut)

        {
            Invoice invoice = db.Invoices.Where(i => i.InvoiceId == inforCheckOut.InvoiceId).FirstOrDefault();

            var ips = db.Invoice_Products.Where(i => i.InvoiceId == invoice.InvoiceId).Include(i => i.Product).ToList();

            if (ModelState.IsValid) { 
              

                var productQuantities = ips.GroupBy(i => i.ProductId)
                                        .Select(g => new
                                        {
                                            ProductId = g.Key,
                                            TotalQuantity = g.Sum(i => i.quantity)
                                        })
                                        .ToList();

                foreach (var x in productQuantities)
                {
                    Product product = db.Products.Where(p => p.ProductId == x.ProductId).FirstOrDefault();
                    if (product.quantity < x.TotalQuantity)
                    {
                        TempData["error"] = "so luong mua " + product.title + " vuot qua so luong ton kho";
                        return RedirectToAction("Index");
                    } else
                    {
                        product.quantity -= x.TotalQuantity;
                        db.SaveChanges();
                    }
                }

                if (invoice != null)
                    {
                        invoice.Status = true;
                        db.SaveChanges();

                        // send email to customer


                        string contentCustomer = "";


                        double totalPrice = 0;

                        foreach (var ip in ips)
                        {
                            totalPrice += int.Parse(ip.totalPrice);
                        }

                        ViewBag.InforCheckOut = inforCheckOut;
                        ViewBag.TotalPrice = totalPrice;

                    contentCustomer = OrderConfirmationEmail(inforCheckOut, ips, totalPrice);


                    SendEmail("ShoesStore", "Đơn hàng", contentCustomer, inforCheckOut.email);

                        return RedirectToAction("ThankYou");
                    
                }
            }

            return RedirectToAction("Index");

        }

        
        public IActionResult ThankYou()
        {
            
            return View();
        }



        private static string email = "dovandaihoc@gmail.com"; 
        private static string password = "hmiozfkqdmhwpufc";

        public static bool SendEmail(string name = "", string subject = "", string content = "", string toMail = "")
        {
            try
            {
                MailMessage message = new MailMessage();
                var smtp = new System.Net.Mail.SmtpClient();
                {
                    smtp.Host = "smtp.gmail.com";
                    smtp.Port = 587;
                    smtp.EnableSsl = true;
                    smtp.DeliveryMethod = System.Net.Mail.SmtpDeliveryMethod.Network;
                    smtp.UseDefaultCredentials = false;
                    smtp.Credentials = new NetworkCredential()
                    {
                        UserName = email, Password = password
                    };
                    smtp.Timeout = 20000;
                }
                MailAddress fromAddress = new MailAddress(email,name);
                message.From = fromAddress;
                message.To.Add(toMail);
                message.Subject = subject;
                message.IsBodyHtml = true;
                message.Body = content;
                smtp.Send(message);
                return true;
            }catch (Exception ex) 
            {
                return false;
            }

        }


        public static string OrderConfirmationEmail(InforCheckOut inforCheckOut, List<Invoice_Products> orderItems, double totalPrice)
        {
            var htmlContent = $@"
            <!DOCTYPE html>
            <html lang='en'>
            <head>
                <meta charset='UTF-8'>
                <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                <title>Đơn đặt hàng thành công</title>
            </head>
            <body>
                <table border='0' cellpadding='0' cellspacing='0' width='100%'>
                    <tr>
                        <td style='padding: 20px 0; text-align: center; background-color: #96588a; color: #ffffff;'>
                            <h1>Cảm ơn bạn đã đặt hàng!</h1>
                        </td>
                    </tr>
                    <tr>
                        <td style='padding: 20px 0;'>
                            <p>Xin chào {inforCheckOut.fristName} {inforCheckOut.lastName},</p>
                            <p>Cảm ơn bạn đã đặt hàng tại cửa hàng chúng tôi. Dưới đây là một số thông tin về đơn hàng của bạn:</p>

                            <h2>Thông tin chi tiết đơn hàng</h2>
                            <table border='1' cellpadding='10' cellspacing='0' style='width: 100%; border-collapse: collapse;'>
                                <thead>
                                    <tr>
                                        <th>Sản phẩm</th>
                                        <th>Số lượng</th>
                                        <th>Tổng tiền</th>
                                    </tr>
                                </thead>
                                <tbody>
                                    {GenerateOrderItemsRows(orderItems)}
                                </tbody>
                                <tfoot>
                                    <tr>
                                        <td colspan='2' style='text-align: right; font-weight: bold;'>Tổng cộng:</td>
                                        <td>{totalPrice} $</td>
                                    </tr>
                                </tfoot>
                            </table>

                            <h2>Thông tin người nhận hàng</h2>
                            <p>Họ và tên: <strong style='color: #ffffff'>{inforCheckOut.fristName} {inforCheckOut.lastName}</strong></p> 
                            <p>Địa chỉ: <strong style='color: #ffffff' >{inforCheckOut.address}</strong> </p>
                            <p>Số điện thoại: <strong style='color: #ffffff'>{inforCheckOut.phone}</strong> </p>
                            <p>Ghi chú: {inforCheckOut.note} </p>
                            <p>Cảm ơn bạn đã lựa chọn cửa hàng chúng tôi. Chúng tôi sẽ liên hệ với bạn để xác nhận đơn hàng của bạn sớm nhất có thể.</p>
                        </td>
                    </tr>
                </table>
            </body>
            </html>
        ";

            return htmlContent;
        }

         private static string GenerateOrderItemsRows(List<Invoice_Products> orderItems)
    {
        var rows = "";
        foreach (var item in orderItems)
        {
            rows += $@"
                <tr>
                    <td>{item.Product.title}</td>
                    <td>{item.quantity}</td>
                    <td>{item.totalPrice} $</td>
                </tr>
            ";
        }
        return rows;
    }




    }


}
