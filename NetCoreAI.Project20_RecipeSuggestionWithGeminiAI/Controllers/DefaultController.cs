using Microsoft.AspNetCore.Mvc;
using NetCoreAI.Project20_RecipeSuggestionWithGeminiAI.Models;

namespace NetCoreAI.Project20_RecipeSuggestionWithGeminiAI.Controllers
{
    public class DefaultController : Controller
    {
        private readonly GeminiAIService _geminiAIService;

        public DefaultController(GeminiAIService geminiAIService)
        {
            _geminiAIService = geminiAIService;
        }

        [HttpGet]
        public IActionResult CreateRecipe()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateRecipe(string ingredients)
        {
            var result = await _geminiAIService.GetRecipeAsync(ingredients);
            ViewBag.recipe = result;
            return View();
        }
    }
}
