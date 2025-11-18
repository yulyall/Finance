using System;
using System.IO;
using Xunit;
using PersonalFinanceTracker.Core.Data;
using PersonalFinanceTracker.Core.Models;
using PersonalFinanceTracker.Core.Services;

namespace FinanceTracker.Tests
{
    public class TransactionServiceTests
    {
        private string CreateTempFile()
        {
            string path = Path.GetTempFileName();
            File.WriteAllText(path, "{}");
            return path;
        }

        [Fact]
        public void AddTransaction_ShouldAddIncomeCorrectly()
        {

            string temp = CreateTempFile();
            var repo = new DataRepository(temp);
            var service = new TransactionService(repo);

            service.AddTransaction(TransactionType.Income, "Зарплата", 1000m);
            decimal balance = service.CalculateBalance();

            Assert.Equal(1000m, balance);
        }

        [Fact]
        public void AddTransaction_ShouldThrowIfAmountIsNegative()
        {

            string temp = CreateTempFile();
            var repo = new DataRepository(temp);
            var service = new TransactionService(repo);

            Assert.Throws<ArgumentException>(() =>
                service.AddTransaction(TransactionType.Expense, "Еда", -50m));
        }

        [Fact]
        public void GetTransactionsByMonth_ShouldReturnOnlyMatching()
        {
            string temp = CreateTempFile();
            var repo = new DataRepository(temp);

            var data = new FinanceData();

            data.Transactions.Add(new Transaction
            {
                Type = TransactionType.Expense,
                Category = "Еда",
                Amount = 100m,
                Date = new DateTime(2024, 12, 5)
            });

            data.Transactions.Add(new Transaction
            {
                Type = TransactionType.Income,
                Category = "Зарплата",
                Amount = 500m,
                Date = new DateTime(2025, 1, 12)
            });

            repo.Save(data);

            var service = new TransactionService(repo);

            var december = service.GetTransactionsByMonth(12, 2024);
            var january = service.GetTransactionsByMonth(1, 2025);
            Assert.Single(december);
            Assert.Single(january);

            Assert.Equal("Еда", december[0].Category);
            Assert.Equal("Зарплата", january[0].Category);
        }

        [Fact]
        public void SetBudget_ShouldSaveCorrectly()
        {
            string temp = CreateTempFile();
            var repo = new DataRepository(temp);
            var service = new TransactionService(repo);

            service.SetBudget(30000m, 1, 2025);
            var budget = service.GetCurrentBudget();
            Assert.Equal(30000m, budget.MonthlyLimit);
            Assert.Equal(1, budget.Month);
            Assert.Equal(2025, budget.Year);
        }

        [Fact]
        public void AddCustomCategory_ShouldAddNewCategory()
        {
            string temp = CreateTempFile();
            var repo = new DataRepository(temp);
            var service = new TransactionService(repo);

            service.AddCustomCategory("Игрушки");
            var categories = service.GetCustomCategories();
            Assert.Contains("Игрушки", categories);
        }
    }
}
