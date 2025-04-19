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

        //[HttpPost("borrow/{bookId}")]
        //[Authorize]
        //public async Task<IActionResult> BorrowBook(int bookId)
        //{
        //    var userId = (User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
        //    var book = await _context.Books.FindAsync(bookId);

        //    if (book == null || book.TotalCopies == 1)
        //        return BadRequest("Book is not available.");

        //    var borrowedBook = new BorrowedBook
        //    {
        //        UserId = userId.ToString(),
        //        BookId = bookId,
        //        ReturnDate = DateTime.UtcNow.AddDays(14) //2 heftelik icaze
        //    };

        //    book.AvailableCopies -= 1;
        //    _context.BorrowedBooks.Add(borrowedBook);
        //    await _context.SaveChangesAsync();

        //    return Ok(new { message = "Book borrowed successfully", dueDate = borrowedBook.ReturnDate });
        //}
        [HttpPost("borrow/{bookId}")]
        [Authorize]
        public async Task<IActionResult> BorrowBook(int bookId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var book = await _context.Books.FindAsync(bookId);

            if (book == null || book.AvailableCopies <= 0)
                return BadRequest("Book is not available.");

            // ❗ Check if user already has this book borrowed and not yet returned
            var alreadyBorrowed = await _context.BorrowedBooks
                .AnyAsync(bb => bb.UserId == userId && bb.BookId == bookId && bb.Status != "returned");

            if (alreadyBorrowed)
                return BadRequest("You have already borrowed this book. Please return it before borrowing again.");

            var borrowedBook = new BorrowedBook
            {
                UserId = userId,
                BookId = bookId,
                ReturnDate = DateTime.UtcNow.AddDays(14),
                Status = "borrowed" // ← Make sure you set this on creation
            };

            book.AvailableCopies -= 1;
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
            book.AvailableCopies+= 1;
            borrowedBook.Status = "returned";

            await _context.SaveChangesAsync();
            return Ok(new { message = "Book returned successfully" });
        }
        [HttpGet("user/{userId}/all")]
        [Authorize]
        public async Task<IActionResult> GetAllBorrowedBooksByUser(string userId)
        {
            var borrowedBooks = await _context.BorrowedBooks
                .Include(bb => bb.Book)
                .Where(bb => bb.UserId == userId)
                .Select(bb => new
                {
                    bb.BorrowId,
                    bb.BookId,
                    bb.Book.Title,
                    bb.Book.Author,
                    bb.Book.Genre,
                    bb.ReturnDate,
                    bb.Status
                })
                .ToListAsync();

            if (!borrowedBooks.Any())
                return NotFound("No borrowed books found for this user.");

            return Ok(borrowedBooks);
        }
        [HttpGet("user/{userId}/active")]
        [Authorize]
        public async Task<IActionResult> GetActiveBorrowedBooksByUser(string userId)
        {
            var borrowedBooks = await _context.BorrowedBooks
                .Include(bb => bb.Book)
                .Where(bb => bb.UserId == userId && bb.Status != "returned")
                .Select(bb => new
                {
                    bb.BorrowId,
                    bb.BookId,
                    bb.Book.Title,
                    bb.Book.Author,
                    bb.Book.Genre,
                    bb.ReturnDate,
                    bb.Status
                })
                .ToListAsync();

            if (!borrowedBooks.Any())
                return NotFound("No active borrowed books found for this user.");

            return Ok(borrowedBooks);
        }



    }
}
