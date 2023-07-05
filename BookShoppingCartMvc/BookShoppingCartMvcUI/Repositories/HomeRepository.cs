
using BookShoppingCartMvcUI.Models;
using Microsoft.EntityFrameworkCore;

namespace BookShoppingCartMvcUI.Repositories
{
    public class HomeRepository :IHomeRepository
    {
        private readonly ApplicationDbContext _dbContext;
        public HomeRepository(ApplicationDbContext dbContext)
        {

            _dbContext = dbContext;

        }

        public async Task<IEnumerable<Genre>> Genres()
        {
            return await _dbContext.Genres.ToListAsync();
        }
        public async Task<IEnumerable<Book>> GetBooks(string sTerm="", int genreId = 0)
        {
            sTerm = sTerm.ToLower();
            IEnumerable<Book> books= await (from book in _dbContext.Books
                       join genre in _dbContext.Genres
                       on book.GenreId equals genre.Id
                       where string.IsNullOrWhiteSpace(sTerm) || (book!=null && book.BookName.ToLower().StartsWith(sTerm))
                       select new Book
                       {
                           Id=book.Id,
                           Image = book.Image,
                           AuthorName = book.AuthorName,
                           BookName= book.BookName,
                           GenreId=book.GenreId,
                           Price = book.Price,
                           GenreName= book.GenreName

                       }).ToListAsync();
            if (genreId > 0)
            {

                books = books.Where(a => a.GenreId == genreId).ToList();
            }
            return books;

        }
    }
}
