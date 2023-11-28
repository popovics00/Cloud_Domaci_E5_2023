using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using Model;

namespace Web1.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;

        [BindProperty]
        public RequestForm RequestForm { get; set; }

        public IndexModel(ILogger<IndexModel> logger)
        {
            _logger = logger;
        }

        public void OnGet()
        {

        }

        public async Task<IActionResult> OnPost()
        {
            IValidator proxy = ServiceProxy.Create<IValidator>(new Uri("fabric:/Application1/ValidateService"));

            var result = await proxy.Validate(RequestForm).ConfigureAwait(false);

            TempData["Message"] = result;

            return RedirectToPage();
        }
    }
}