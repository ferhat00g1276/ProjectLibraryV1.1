using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectLibrary.DataAccess.Data;
using ProjectLibrary.Entities.Concrete;
using System.Security.Claims;

namespace ProjectLibrary.Server.Controllers
{
    [Route("api/borrow")]
    [ApiController]
    public class BorrowController : ControllerBase
    {
        private readonly LibraryDbContext _context;

        public BorrowController(LibraryDbContext context)
        {
            _context = context;
        }

        [HttpPost("borrow/{bookId}")]
        [Authorize]


        public async Task<IActionResult> BorrowBook(int bookId)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var book = await _context.Books.FindAsync(bookId);

            if (book == null || book.TotalCopies == 1)
                return BadRequest("Book is not available.");

            var borrowedBook = new BorrowedBook
            {
                UserId = userId.ToString(),
                BookId = bookId,
                ReturnDate = DateTime.UtcNow.AddDays(14) //2 heftelik icaze
            };

            book.TotalCopies -= 1;
            _context.BorrowedBooks.Add(borrowedBook);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Book borrowed successfully", dueDate = borrowedBook.ReturnDate });
        }

        [HttpPost("return/{borrowId}")]
        [Authorize]
        public async Task<IActionResult> ReturnBook(int borrowId)
        {
            var borrowedBook = await _context.BorrowedBooks.FindAsync(borrowId);

            if (borrowedBook == null || borrowedBook.Status=="returned")
                return BadRequest("Invalid return request.");

            var book = await _context.Books.FindAsync(borrowedBook.BookId);
            book.TotalCopies += 1;
            borrowedBook.Status = "returned";

            await _context.SaveChangesAsync();
            return Ok(new { message = "Book returned successfully" });
        }


    }
}
