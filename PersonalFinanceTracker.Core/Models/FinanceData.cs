using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PersonalFinanceTracker.Core.Models
{
    public class FinanceData
    {
        public List<Transaction> Transactions { get; set; }
        public List<string> CustomCategories { get; set; }
        public Budget CurrentBudget { get; set; }

        public FinanceData()
        {
            Transactions = new List<Transaction>();
            CustomCategories = new List<string>();
            CurrentBudget = new Budget();
        }
    }
}
