using JMAPI.Database;
using JMAPI.Interfaces;
using JMAPI.Models;
using JMAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JMAPI.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class ExpenseItemsController : ControllerBase
    {
        
        private readonly IExpenseItemsService _expenseItem;

        public ExpenseItemsController(IExpenseItemsService expenseItemService)
        {
            _expenseItem = expenseItemService;
        }

        // GET: api/ExpenseItems
        [HttpGet]
        public async Task<IActionResult> GetExpenseItems()
        {
            return Ok(await _expenseItem.GetAllAsync());
        }

        // GET: api/ExpenseItems/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetExpenseItem(int id)
        {
            var expenseItem = await _expenseItem.GetByIdAsync(id);

            if (expenseItem == null)
            {
                return NotFound();
            }

            return Ok(expenseItem);
        }

        // GET: api/ExpenseItems/category/
        [HttpGet("category")]
        public async Task<IActionResult> GetExpenseCategories()
        {
            var items = await _expenseItem.GetExpenseCategories();
            if (items == null || !items.Any())
            {
                return NotFound($"No category found.");
            }
            return Ok(items);
        }

        // PUT: api/ExpenseItems/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateExpenseItem(int id, [FromBody] ExpenseItem request)
        {
            if (request == null) return BadRequest("Request body is required.");

            var updated = await _expenseItem.UpdateAsync( request);
            if (!updated) return NotFound($"Expense Item {id} not found.");

            return Ok(updated);
        }

        // POST: api/ExpenseItems
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<IActionResult> PostExpenseItem(ExpenseItem expenseItem)
        {
            var res = _expenseItem.CreateAsync(expenseItem);

            if (res != null) {
                return Ok(res);
            }

            return CreatedAtAction("GetExpenseItem", new { id = expenseItem.Id }, expenseItem);
        }

        // DELETE: api/ExpenseItems/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteExpenseItem(int id)
        {
            var res = await _expenseItem.DeleteAsync(id);
            if (!res )
            {
                return NotFound();
            }

            if(res == false)
            {
                return StatusCode(500, "Failed to delete expense item.");
            }

            return Ok();
        }
    }
}
