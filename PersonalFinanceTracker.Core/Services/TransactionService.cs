using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PersonalFinanceTracker.Core.Data;
using PersonalFinanceTracker.Core.Models;

namespace PersonalFinanceTracker.Core.Services
{
    public class TransactionService
    {
        private readonly DataRepository _repository;
        private FinanceData _data;

        public TransactionService(DataRepository repository)
        {
            _repository = repository;
            _data = _repository.Load();
        }

        public void AddTransaction(TransactionType type, string category, decimal amount, string description = "")
        {
            if (amount <= 0)
                throw new ArgumentException("Сумма должна быть положительной");

            if (string.IsNullOrWhiteSpace(category))
                throw new ArgumentException("Категория не может быть пустой");

            var transaction = new Transaction
            {
                Type = type,
                Category = category,
                Amount = amount,
                Description = description
            };

            _data.Transactions.Add(transaction);
            _repository.Save(_data);
        }

        public decimal CalculateBalance()
        {
            return _data.Transactions.Sum(t =>
                t.Type == TransactionType.Income ? t.Amount : -t.Amount);
        }

        public List<Transaction> GetAllTransactions()
        {
            return _data.Transactions
                .OrderByDescending(t => t.Date)
                .ToList();
        }

        public List<Transaction> GetTransactionsByMonth(int month, int year)
        {
            return _data.Transactions
                .Where(t => t.Date.Month == month && t.Date.Year == year)
                .OrderByDescending(t => t.Date)
                .ToList();
        }

        public Dictionary<string, decimal> GetCategoryStatistics(int month, int year, TransactionType type)
        {
            return _data.Transactions
                .Where(t => t.Date.Month == month && t.Date.Year == year && t.Type == type)
                .GroupBy(t => t.Category)
                .ToDictionary(g => g.Key, g => g.Sum(t => t.Amount));
        }

        public void SetBudget(decimal amount, int month, int year)
        {
            if (amount < 0)
                throw new ArgumentException("Бюджет не может быть отрицательным");

            _data.CurrentBudget = new Budget
            {
                MonthlyLimit = amount,
                Month = month,
                Year = year
            };

            _repository.Save(_data);
        }

        public Budget GetCurrentBudget() => _data.CurrentBudget;

        public void AddCustomCategory(string category)
        {
            if (!_data.CustomCategories.Contains(category))
            {
                _data.CustomCategories.Add(category);
                _repository.Save(_data);
            }
        }

        public List<string> GetCustomCategories() => _data.CustomCategories;
    }
}
