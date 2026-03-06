using System.ComponentModel.DataAnnotations;

namespace JMAPI.Models
{
    public class ExpenseCategory
    {
        [Key]
        public int Id { get; set; }
        public required string Name { get; set; }

    }
}
