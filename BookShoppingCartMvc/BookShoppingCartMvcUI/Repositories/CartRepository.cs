using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BookShoppingCartMvcUI.Repositories
{
    public class CartRepository:ICartRepository
    {
        private readonly ApplicationDbContext _applicationDbContext;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IHttpContextAccessor _httpContextAccessor;
        public CartRepository(ApplicationDbContext applicationDbContext, UserManager<IdentityUser> userManager, IHttpContextAccessor httpContextAccessor)
        {
            _applicationDbContext = applicationDbContext;
            _userManager = userManager;
            _httpContextAccessor = httpContextAccessor;

        }

        public async Task<int> AddItem(int bookId, int qty)
        {
            string userId = GetUserId();
            using var transaction = _applicationDbContext.Database.BeginTransaction();
            try
            {
                if (string.IsNullOrEmpty(userId))
                    throw new Exception("user is not logged-in");
                var cart = await GetCart(userId);
                if (cart is null)
                {
                    cart = new ShoppingCart
                    {
                        UserId = userId
                    };
                    _applicationDbContext.ShoppingCarts.Add(cart);
                }
                _applicationDbContext.SaveChanges();
                // cart detail section
                var cartItem = _applicationDbContext.CartDetails
                                  .FirstOrDefault(a => a.ShoppingCartId == cart.Id && a.BookId == bookId);
                if (cartItem is not null)
                {
                    cartItem.Quantity += qty;
                }
                else
                {
                    var book = _applicationDbContext.Books.Find(bookId);
                    cartItem = new CartDetail
                    {
                        BookId = bookId,
                        ShoppingCartId = cart.Id,
                        Quantity = qty,
                        UnitPrice = book.Price  // it is a new line after update
                    };
                    _applicationDbContext.CartDetails.Add(cartItem);
                }
                _applicationDbContext.SaveChanges();
                transaction.Commit();
            }
            catch (Exception ex)
            {
            }
            var cartItemCount = await GetCartItemCount(userId);
            return cartItemCount;
        }

        public async Task<int> RemoveItem(int bookId)
        {
            //using var transaction = _db.Database.BeginTransaction();
            string userId = GetUserId();
            try
            {
                if (string.IsNullOrEmpty(userId))
                    throw new Exception("user is not logged-in");
                var cart = await GetCart(userId);
                if (cart is null)
                    throw new Exception("Invalid cart");
                // cart detail section
                var cartItem = _applicationDbContext.CartDetails
                                  .FirstOrDefault(a => a.ShoppingCartId == cart.Id && a.BookId == bookId);
                if (cartItem is null)
                    throw new Exception("Not items in cart");
                else if (cartItem.Quantity == 1)
                    _applicationDbContext.CartDetails.Remove(cartItem);
                else
                    cartItem.Quantity = cartItem.Quantity - 1;
                _applicationDbContext.SaveChanges();
            }
            catch (Exception ex)
            {

            }
            var cartItemCount = await GetCartItemCount(userId);
            return cartItemCount;
        }

        public async Task<ShoppingCart> GetUserCart()
        {
            var userId= GetUserId();
            if(userId == null)
            
                throw new Exception("Invalid userid");

            
            var shoppingCart = await _applicationDbContext.ShoppingCarts
                                .Include(a=> a.CartDetails)
                                .ThenInclude(a=>a.Book)
                                .ThenInclude(a=>a.Genre)
                                .Where(a=>a.UserId == userId).FirstOrDefaultAsync();
            return shoppingCart;
        }

        public async Task<ShoppingCart>  GetCart(string userId)
        {
            var cart = await _applicationDbContext.ShoppingCarts.FirstOrDefaultAsync(x => x.UserId == userId);
            return cart;
        }

        public async Task<int> GetCartItemCount(string userId = "")
        {
            if (!string.IsNullOrEmpty(userId))
            {
                userId = GetUserId();

            }
            var data = await (from cart in _applicationDbContext.ShoppingCarts
                              join CartDetail in _applicationDbContext.CartDetails
                              on cart.Id equals CartDetail.ShoppingCartId
                              select new {CartDetail.Id}).ToListAsync();
            return data.Count;
        }
        public async Task<bool> DoCheckout()
        {
            using var transaction = _applicationDbContext.Database.BeginTransaction();
            try
            {
                // logic
                // move data from cartDetail to order and order detail then remove cart detail
                var userId = GetUserId();
                if (string.IsNullOrEmpty(userId))
                    throw new Exception("User is not logged-in");
                var cart = await GetCart(userId);
                if (cart is null)
                    throw new Exception("Invalid cart");
                var cartDetail = _applicationDbContext.CartDetails
                                    .Where(a => a.ShoppingCartId == cart.Id).ToList();
                if (cartDetail.Count == 0)
                    throw new Exception("Cart is empty");
                var order = new Order
                {
                    UserId = userId,
                    CreateDate = DateTime.UtcNow,
                    OrderStatusId = 1//pending
                };
                _applicationDbContext.Orders.Add(order);
                _applicationDbContext.SaveChanges();
                foreach (var item in cartDetail)
                {
                    var orderDetail = new OrderDetail
                    {
                        BookId = item.BookId,
                        OrderId = order.Id,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice
                    };
                    _applicationDbContext.OrderDetails.Add(orderDetail);
                }
                _applicationDbContext.SaveChanges();

                // removing the cartdetails
                _applicationDbContext.CartDetails.RemoveRange(cartDetail);
                _applicationDbContext.SaveChanges();
                transaction.Commit();
                return true;
            }
            catch (Exception)
            {

                return false;
            }
        }

        private string GetUserId()
        {
            var principal = _httpContextAccessor.HttpContext.User;
            string userId = _userManager.GetUserId(principal);
            return userId;
        }
    }
}
