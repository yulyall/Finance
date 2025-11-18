using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PersonalFinanceTracker.Core.Models
{
    public enum TransactionType
    {
        Income,
        Expense
    }

    public class Transaction
    {
        public Guid Id { get; set; }
        public TransactionType Type { get; set; }
        public string Category { get; set; } = "";
        public decimal Amount { get; set; }
        public string Description { get; set; } = "";
        public DateTime Date { get; set; }

        public Transaction()
        {
            Id = Guid.NewGuid();
            Date = DateTime.Now;
        }
    }
}
