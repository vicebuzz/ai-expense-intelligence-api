using ExpenseIntelligence.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseIntelligence.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CategoriesController : ControllerBase
{
    private readonly CategoryCatalogService _categories;

    public CategoriesController(CategoryCatalogService categories) => _categories = categories;

    [HttpGet]
    public ActionResult<object> GetAll() =>
        Ok(new
        {
            expense = _categories.GetExpenseCategories(),
            income = _categories.GetIncomeCategories()
        });
}
