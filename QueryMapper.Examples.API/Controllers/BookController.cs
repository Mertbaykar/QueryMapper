using Microsoft.AspNetCore.Mvc;
using QueryMapper.Examples.Core;

namespace QueryMapper.Examples.API.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class BookController : ControllerBase
    {
        private readonly IBookRepository BookRepository;

        public BookController(IBookRepository bookRepository)
        {
            BookRepository = bookRepository;
        }

        [HttpGet]
        public IActionResult Get()
        {
            var result = BookRepository.Get();
            return Ok(result);
        }

        [HttpGet]
        public IActionResult GetExpression()
        {
            var result = BookRepository.GetExpression();
            return Ok(result);
        }
    }
}
