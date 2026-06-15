using Microsoft.AspNetCore.Mvc;

namespace mcnylo.dev.Contact.Controllers
{
    public class ContactController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
