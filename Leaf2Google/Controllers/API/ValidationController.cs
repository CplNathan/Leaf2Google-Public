using Leaf2Google.Contexts;
using Leaf2Google.Dependency.Car;
using Microsoft.AspNetCore.Mvc;

namespace Leaf2Google.Controllers.API
{
    public class ValidationController : BaseAPIController
    {
        private readonly LeafContext _leafContext;

        public ValidationController(ILogger<HomeController> logger, LeafSessionManager sessions, LeafContext leafContext, IConfiguration configuration)
            : base(logger, sessions, configuration)
        {
            _leafContext = leafContext;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public IActionResult UsernameValid(string Username)
        {
            return Json(_leafContext.NissanLeafs.Any(leaf => leaf.NissanUsername == Username && leaf.Deleted == null));
        }

        [HttpPost]
        public IActionResult UsernameUnique(string Username)
        {
            return Json(!_leafContext.NissanLeafs.Any(leaf => leaf.NissanUsername == Username && leaf.Deleted == null));
        }
    }
}