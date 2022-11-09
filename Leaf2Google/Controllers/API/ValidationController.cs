using Microsoft.AspNetCore.Mvc;

namespace Leaf2Google.Controllers.API;

public class ValidationController : BaseAPIController
{
    private readonly LeafContext _leafContext;

    public ValidationController(BaseStorageManager storageManager, ICarSessionManager sessionManager, LeafContext leafContext)
        : base(storageManager, sessionManager)
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