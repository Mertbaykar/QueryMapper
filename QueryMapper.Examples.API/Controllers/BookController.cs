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
            var watch = System.Diagnostics.Stopwatch.StartNew();

            var result = BookRepository.Get();
            watch.Stop();
            var elapsedMs = watch.ElapsedMilliseconds;
            //watch.Reset();

            //watch.Start();
            //var resultautomapper = BookRepository.GetByAutoMapper();
            //watch.Stop();
            //var elapsedMsAutomapper = watch.ElapsedMilliseconds;

            return Ok(elapsedMs);
            //return Ok(new { elapsedMs, elapsedMsAutomapper });
            //return Ok(result);
        }

        [HttpGet]
        public IActionResult GetByAutoMapper()
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();

            var result = BookRepository.GetByAutoMapper();

            watch.Stop();
            var elapsedMs = watch.ElapsedMilliseconds;

            return Ok(elapsedMs);
            //return Ok(result);
        }

        [HttpGet]
        public IActionResult GetExpression()
        {
            var result = BookRepository.GetExpression();
            return Ok(result);
        }
    }
}
