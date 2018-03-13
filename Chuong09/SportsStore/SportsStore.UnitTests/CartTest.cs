using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SportsStore.Domain.Entities;
using Moq;
using SportsStore.Domain.Abstract;
using SportsStore.WebUI.Controllers;
using System.Web.Mvc;
using SportsStore.WebUI.Models;
namespace SportsStore.UnitTests
{
    [TestClass]
    public class CartTests
    {
        [TestMethod]
        public void Can_Add_New_Lines()
        {
            // Arrange - create some test products
            Product p1 = new Product { ProductID = 1, Name = "P1" };
            Product p2 = new Product { ProductID = 2, Name = "P2" };
            // Arrange - create a new cart
            Cart target = new Cart();
            // Act
            target.AddItem(p1, 1);
            target.AddItem(p2, 1);
            CartLine[] results = target.Lines.ToArray();
            // Assert
            Assert.AreEqual(results.Length, 2);
            Assert.AreEqual(results[0].Product, p1);
            Assert.AreEqual(results[1].Product, p2);
        }

        [TestMethod]
        public void Can_Add_Quantity_For_Existing_Lines()
        {
            // Arrange - create some test products
            Product p1 = new Product { ProductID = 1, Name = "P1" };
            Product p2 = new Product { ProductID = 2, Name = "P2" };
            // Arrange - create a new cart
            Cart target = new Cart();
            // Act
            target.AddItem(p1, 1);
            target.AddItem(p2, 1);
            target.AddItem(p1, 10);
            CartLine[] results = target.Lines.OrderBy(c => c.Product.ProductID).ToArray();
            // Assert
            Assert.AreEqual(results.Length, 2);
            Assert.AreEqual(results[0].Quantity, 11);
            Assert.AreEqual(results[1].Quantity, 1);
        }

        [TestMethod]
        public void Can_Clear_Contents()
        {
            // Arrange - create some test products
            Product p1 = new Product
            {
                ProductID = 1,
                Name = "P1",
                Price = 100M
            };
            Product p2 = new Product { ProductID = 2, Name = "P2", Price = 50M };
            // Arrange - create a new cart
            Cart target = new Cart();
            // Arrange - add some items
            target.AddItem(p1, 1);
            target.AddItem(p2, 1);
            // Act - reset the cart
            target.Clear();
            // Assert
            Assert.AreEqual(target.Lines.Count(), 0);
        }


        [TestMethod]
        public void Can_Add_To_Cart()
        {
            // Arrange - create the mock repository
            Mock<IProductRepository> mock = new Mock<IProductRepository>();
            mock.Setup(m => m.Products).Returns(new Product[] {
                new Product {ProductID = 1, Name = "P1", Category ="Apples"},}.AsQueryable());
            // Arrange - create a Cart
            Cart cart = new Cart();
            // Arrange - create the controller
            CartController target = new CartController(mock.Object, null);
            // Act - add a product to the cart
            target.AddToCart(cart, 1, null);
            // Assert
            Assert.AreEqual(cart.Lines.Count(), 1);
            Assert.AreEqual(cart.Lines.ToArray()[0].Product.ProductID, 1);
        }
        [TestMethod]
        public void Adding_Product_To_Cart_Goes_To_Cart_Screen()
        {
            // Arrange - create the mock repository
            Mock<IProductRepository> mock = new Mock<IProductRepository>();
            mock.Setup(m => m.Products).Returns(new Product[]
                    {new Product {ProductID = 1, Name = "P1", Category ="Apples"},}.AsQueryable());
            // Arrange - create a Cart
            Cart cart = new Cart();
            // Arrange - create the controller
            CartController target = new CartController(mock.Object, null);
            // Act - add a product to the cartRedirectToRouteResult result = target.AddToCart(cart, 2,
            RedirectToRouteResult result = target.AddToCart(cart, 2, "myUrl");
            // Assert
            Assert.AreEqual(result.RouteValues["action"], "Index");
            Assert.AreEqual(result.RouteValues["returnUrl"], "myUrl");
        }
        [TestMethod]
        public void Can_View_Cart_Contents()
        {
            // Arrange - create a Cart
            Cart cart = new Cart();
            // Arrange - create the controller
            CartController target = new CartController(null, null);
            // Act - call the Index action method
            CartIndexViewModel result =
            (CartIndexViewModel)target.Index(cart, "myUrl").ViewData.Model;
            // Assert
            Assert.AreSame(result.Cart, cart);
            Assert.AreEqual(result.ReturnUrl, "myUrl");
        }

        [TestMethod]
        public void Cannot_Checkout_Empty_Cart()
        {
            // Chuẩn bị – tạo một đơn hàng giả
            Mock<IOrderProcessor> mock = new Mock<IOrderProcessor>();
            // Chuẩn bị – tạo một giỏ hàng trống
            Cart cart = new Cart();
            // Chuẩn bị – tạo các chi tiết vận chuyển
            ShippingDetails shippingDetails = new ShippingDetails();
            // Chuẩn bị – tạo một controller
            CartController target = new CartController(null, mock.Object);
            // Thực hiện
            ViewResult result = target.Checkout(cart, shippingDetails);
            // Xác nhận – kiểm tra đơn hàng không được thông qua vào bộ xử lý
            mock.Verify(m => m.ProcessOrder(It.IsAny<Cart>(),
            It.IsAny<ShippingDetails>()), Times.Never());
            // Xác nhận – kiểm tra xem phương thức nào là trở về trang mặc định
            Assert.AreEqual("", result.ViewName);
            // Xác nhận - kiểm tra rằng tôi chuyển một mô hình hợp lệ sang giao  diện hiển thị
            Assert.AreEqual(false, result.ViewData.ModelState.IsValid);
        }

        [TestMethod]
        public void Cannot_Checkout_Invalid_ShippingDetails()
        {
            // Sắp xếp – tạo một bộ xử lý đơn hàng giả
            Mock<IOrderProcessor> mock = new Mock<IOrderProcessor>();
            // Chuẩn bị – tạo một giỏ hàng với một món hàng
            Cart cart = new Cart();
            cart.AddItem(new Product(), 1);
            // Chuẩn bị - tạo một controller
            CartController target = new CartController(null, mock.Object);
            // Chuẩn bị - thêm một lỗi vào mô hình
            target.ModelState.AddModelError("error", "error");
            // Thực hiện – thử thanh toán
            ViewResult result = target.Checkout(cart, new ShippingDetails());
            // Xác nhận – kiểm tra xem đơn hàng không được chuyển sang bộ xử lý
            mock.Verify(m => m.ProcessOrder(It.IsAny<Cart>(),
            It.IsAny<ShippingDetails>()), Times.Never());
            // Xác nhận – kiểm tra xem phương thức trả về trang mặc định
            Assert.AreEqual("", result.ViewName);
            // Xác nhận – kiểm tra xem tôi chuyển mô hình không hợp lệ sang giao diện hiển
            Assert.AreEqual(false, result.ViewData.ModelState.IsValid);
        }
    }
}