using JMAPI.Database;
using JMAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JMAPI.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class ExpenseItemsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ExpenseItemsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/ExpenseItems
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ExpenseItem>>> GetExpenseItems()
        {
            return await _context.ExpenseItems.ToListAsync();
        }

        // GET: api/ExpenseItems/5
        [HttpGet("{id}")]
        public async Task<ActionResult<ExpenseItem>> GetExpenseItem(int id)
        {
            var expenseItem = await _context.ExpenseItems.FindAsync(id);

            if (expenseItem == null)
            {
                return NotFound();
            }

            return expenseItem;
        }

        // PUT: api/ExpenseItems/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutExpenseItem(int id, ExpenseItem expenseItem)
        {
            if (id != expenseItem.Id)
            {
                return BadRequest();
            }

            _context.Entry(expenseItem).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ExpenseItemExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/ExpenseItems
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<ExpenseItem>> PostExpenseItem(ExpenseItem expenseItem)
        {
            _context.ExpenseItems.Add(expenseItem);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetExpenseItem", new { id = expenseItem.Id }, expenseItem);
        }

        // DELETE: api/ExpenseItems/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteExpenseItem(int id)
        {
            var expenseItem = await _context.ExpenseItems.FindAsync(id);
            if (expenseItem == null)
            {
                return NotFound();
            }

            _context.ExpenseItems.Remove(expenseItem);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool ExpenseItemExists(int id)
        {
            return _context.ExpenseItems.Any(e => e.Id == id);
        }
    }
}
