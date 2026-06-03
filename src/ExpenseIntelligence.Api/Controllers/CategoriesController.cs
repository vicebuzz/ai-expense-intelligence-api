using ExpenseIntelligence.Api.Models;
using ExpenseIntelligence.Api.Services;
using ExpenseIntelligence.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseIntelligence.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CategoriesController : ControllerBase
{
    private readonly CategoryCatalogService _categories;
    private readonly TransactionCategorizationService _categorization;

    public CategoriesController(
        CategoryCatalogService categories,
        TransactionCategorizationService categorization)
    {
        _categories = categories;
        _categorization = categorization;
    }

    [HttpGet]
    public async Task<ActionResult<CategoryCatalogResponse>> GetAll(CancellationToken cancellationToken)
    {
        var (expense, income) = await _categories.GetCatalogAsync(cancellationToken);
        return Ok(new CategoryCatalogResponse(
            expense.Where(CategoryFilter.IsIncluded).ToList(),
            income.Where(CategoryFilter.IsIncluded).ToList()));
    }

    [HttpPut]
    public async Task<ActionResult<UpdateCategoriesResult>> Update(
        [FromBody] UpdateCategoriesRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _categories.UpdateCatalogAsync(
                request.Expense,
                request.Income,
                cancellationToken);

            var response = new UpdateCategoriesResult(
                result.Expense.Where(CategoryFilter.IsIncluded).ToList(),
                result.Income.Where(CategoryFilter.IsIncluded).ToList(),
                result.Warnings);

            var trained = await _categorization.RetrainFromDatabaseAsync(cancellationToken);

            return Ok(new
            {
                response.Expense,
                response.Income,
                response.Warnings,
                mlRetrainedSamples = trained
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
