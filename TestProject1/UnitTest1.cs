using PersonalFinanceTracker;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Transactions;
using Xunit;

namespace PersonalFinanceTracker.Tests
{
    // ============= ТЕСТЫ ДЛЯ TransactionService =============

    public class TransactionServiceTests
    {
        private TransactionService CreateService(string testFile = "test_data.json")
        {
            // Удаляем тестовый файл перед каждым тестом для чистоты
            if (File.Exists(testFile))
            {
                File.Delete(testFile);
            }

            var repository = new DataRepository(testFile);
            return new TransactionService(repository);
        }

        [Fact]
        public void AddTransaction_ValidIncome_IncreasesBalance()
        {
            // Arrange
            var service = CreateService("test_income.json");
            decimal initialBalance = service.CalculateBalance();

            // Act
            service.AddTransaction(TransactionType.Income, "Зарплата", 50000m, "Зарплата за ноябрь");
            decimal newBalance = service.CalculateBalance();

            // Assert
            Assert.Equal(initialBalance + 50000m, newBalance);

            // Cleanup
            File.Delete("test_income.json");
        }

        [Fact]
        public void AddTransaction_ValidExpense_DecreasesBalance()
        {
            // Arrange
            var service = CreateService("test_expense.json");
            service.AddTransaction(TransactionType.Income, "Зарплата", 10000m);
            decimal balanceAfterIncome = service.CalculateBalance();

            // Act
            service.AddTransaction(TransactionType.Expense, "Еда", 3000m, "Продукты");
            decimal finalBalance = service.CalculateBalance();

            // Assert
            Assert.Equal(balanceAfterIncome - 3000m, finalBalance);

            // Cleanup
            File.Delete("test_expense.json");
        }

        [Fact]
        public void AddTransaction_NegativeAmount_ThrowsArgumentException()
        {
            // Arrange
            var service = CreateService("test_negative.json");

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() =>
                service.AddTransaction(TransactionType.Expense, "Еда", -500m)
            );

            Assert.Contains("положительной", exception.Message);

            // Cleanup
            File.Delete("test_negative.json");
        }

        [Fact]
        public void AddTransaction_ZeroAmount_ThrowsArgumentException()
        {
            // Arrange
            var service = CreateService("test_zero.json");

            // Act & Assert
            Assert.Throws<ArgumentException>(() =>
                service.AddTransaction(TransactionType.Income, "Зарплата", 0m)
            );

            // Cleanup
            File.Delete("test_zero.json");
        }

        [Fact]
        public void AddTransaction_EmptyCategory_ThrowsArgumentException()
        {
            // Arrange
            var service = CreateService("test_empty_category.json");

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() =>
                service.AddTransaction(TransactionType.Expense, "", 1000m)
            );

            Assert.Contains("Категория", exception.Message);

            // Cleanup
            File.Delete("test_empty_category.json");
        }

        [Fact]
        public void AddTransaction_NullCategory_ThrowsArgumentException()
        {
            // Arrange
            var service = CreateService("test_null_category.json");

            // Act & Assert
            Assert.Throws<ArgumentException>(() =>
                service.AddTransaction(TransactionType.Expense, null, 1000m)
            );

            // Cleanup
            File.Delete("test_null_category.json");
        }

        [Fact]
        public void CalculateBalance_MultipleTransactions_ReturnsCorrectSum()
        {
            // Arrange
            var service = CreateService("test_multiple.json");

            // Act
            service.AddTransaction(TransactionType.Income, "Зарплата", 50000m);
            service.AddTransaction(TransactionType.Expense, "Еда", 5000m);
            service.AddTransaction(TransactionType.Expense, "Транспорт", 3000m);
            service.AddTransaction(TransactionType.Income, "Подработка", 10000m);
            decimal balance = service.CalculateBalance();

            // Assert
            Assert.Equal(52000m, balance);

            // Cleanup
            File.Delete("test_multiple.json");
        }

        [Fact]
        public void CalculateBalance_NoTransactions_ReturnsZero()
        {
            // Arrange
            var service = CreateService("test_empty.json");

            // Act
            decimal balance = service.CalculateBalance();

            // Assert
            Assert.Equal(0m, balance);

            // Cleanup
            File.Delete("test_empty.json");
        }

        [Fact]
        public void GetAllTransactions_ReturnsTransactionsInDescendingOrder()
        {
            // Arrange
            var service = CreateService("test_order.json");
            service.AddTransaction(TransactionType.Income, "Зарплата", 1000m);
            System.Threading.Thread.Sleep(100); // Небольшая задержка для разных временных меток
            service.AddTransaction(TransactionType.Expense, "Еда", 500m);

            // Act
            var transactions = service.GetAllTransactions();

            // Assert
            Assert.Equal(2, transactions.Count);
            Assert.True(transactions[0].Date >= transactions[1].Date);

            // Cleanup
            File.Delete("test_order.json");
        }

        [Fact]
        public void GetTransactionsByMonth_FiltersByMonthAndYear()
        {
            // Arrange
            var service = CreateService("test_month_filter.json");
            var now = DateTime.Now;

            service.AddTransaction(TransactionType.Income, "Зарплата", 50000m);
            service.AddTransaction(TransactionType.Expense, "Еда", 5000m);

            // Act
            var transactions = service.GetTransactionsByMonth(now.Month, now.Year);

            // Assert
            Assert.Equal(2, transactions.Count);
            Assert.All(transactions, t =>
            {
                Assert.Equal(now.Month, t.Date.Month);
                Assert.Equal(now.Year, t.Date.Year);
            });

            // Cleanup
            File.Delete("test_month_filter.json");
        }

        [Fact]
        public void GetCategoryStatistics_IncomeType_ReturnsOnlyIncomes()
        {
            // Arrange
            var service = CreateService("test_income_stats.json");
            var now = DateTime.Now;

            service.AddTransaction(TransactionType.Income, "Зарплата", 50000m);
            service.AddTransaction(TransactionType.Income, "Подработка", 10000m);
            service.AddTransaction(TransactionType.Expense, "Еда", 5000m);

            // Act
            var stats = service.GetCategoryStatistics(now.Month, now.Year, TransactionType.Income);

            // Assert
            Assert.Equal(2, stats.Count);
            Assert.Equal(50000m, stats["Зарплата"]);
            Assert.Equal(10000m, stats["Подработка"]);
            Assert.False(stats.ContainsKey("Еда"));

            // Cleanup
            File.Delete("test_income_stats.json");
        }

        [Fact]
        public void GetCategoryStatistics_ExpenseType_ReturnsOnlyExpenses()
        {
            // Arrange
            var service = CreateService("test_expense_stats.json");
            var now = DateTime.Now;

            service.AddTransaction(TransactionType.Expense, "Еда", 3000m);
            service.AddTransaction(TransactionType.Expense, "Еда", 2000m);
            service.AddTransaction(TransactionType.Expense, "Транспорт", 1500m);
            service.AddTransaction(TransactionType.Income, "Зарплата", 50000m);

            // Act
            var stats = service.GetCategoryStatistics(now.Month, now.Year, TransactionType.Expense);

            // Assert
            Assert.Equal(2, stats.Count);
            Assert.Equal(5000m, stats["Еда"]); // Сумма двух транзакций
            Assert.Equal(1500m, stats["Транспорт"]);
            Assert.False(stats.ContainsKey("Зарплата"));

            // Cleanup
            File.Delete("test_expense_stats.json");
        }

        [Fact]
        public void GetCategoryStatistics_NoTransactions_ReturnsEmptyDictionary()
        {
            // Arrange
            var service = CreateService("test_no_stats.json");
            var now = DateTime.Now;

            // Act
            var stats = service.GetCategoryStatistics(now.Month, now.Year, TransactionType.Expense);

            // Assert
            Assert.Empty(stats);

            // Cleanup
            File.Delete("test_no_stats.json");
        }

        [Fact]
        public void SetBudget_ValidAmount_SavesBudget()
        {
            // Arrange
            var service = CreateService("test_budget.json");
            var now = DateTime.Now;

            // Act
            service.SetBudget(30000m, now.Month, now.Year);
            var budget = service.GetCurrentBudget();

            // Assert
            Assert.NotNull(budget);
            Assert.Equal(30000m, budget.MonthlyLimit);
            Assert.Equal(now.Month, budget.Month);
            Assert.Equal(now.Year, budget.Year);

            // Cleanup
            File.Delete("test_budget.json");
        }

        [Fact]
        public void SetBudget_NegativeAmount_ThrowsArgumentException()
        {
            // Arrange
            var service = CreateService("test_negative_budget.json");
            var now = DateTime.Now;

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() =>
                service.SetBudget(-1000m, now.Month, now.Year)
            );

            Assert.Contains("отрицательным", exception.Message);

            // Cleanup
            File.Delete("test_negative_budget.json");
        }

        [Fact]
        public void AddCustomCategory_NewCategory_AddsSuccessfully()
        {
            // Arrange
            var service = CreateService("test_custom_category.json");
            string newCategory = "Путешествия";

            // Act
            service.AddCustomCategory(newCategory);
            var categories = service.GetCustomCategories();

            // Assert
            Assert.Contains(newCategory, categories);

            // Cleanup
            File.Delete("test_custom_category.json");
        }

        [Fact]
        public void AddCustomCategory_DuplicateCategory_DoesNotAddDuplicate()
        {
            // Arrange
            var service = CreateService("test_duplicate_category.json");
            string category = "Путешествия";

            // Act
            service.AddCustomCategory(category);
            service.AddCustomCategory(category); // Попытка добавить дубликат
            var categories = service.GetCustomCategories();

            // Assert
            Assert.Single(categories);
            Assert.Equal(category, categories[0]);

            // Cleanup
            File.Delete("test_duplicate_category.json");
        }
    }

    // ============= ТЕСТЫ ДЛЯ DataRepository =============

    public class DataRepositoryTests
    {
        [Fact]
        public void Save_And_Load_PreservesData()
        {
            // Arrange
            string testFile = "test_save_load.json";
            var repository = new DataRepository(testFile);
            var data = new FinanceData();

            data.Transactions.Add(new Transaction
            {
                Type = TransactionType.Income,
                Category = "Зарплата",
                Amount = 50000m,
                Description = "Тест"
            });

            data.CurrentBudget = new Budget
            {
                MonthlyLimit = 30000m,
                Month = 11,
                Year = 2025
            };

            // Act
            repository.Save(data);
            var loadedData = repository.Load();

            // Assert
            Assert.NotNull(loadedData);
            Assert.Single(loadedData.Transactions);
            Assert.Equal(50000m, loadedData.Transactions[0].Amount);
            Assert.Equal("Зарплата", loadedData.Transactions[0].Category);
            Assert.Equal(30000m, loadedData.CurrentBudget.MonthlyLimit);

            // Cleanup
            File.Delete(testFile);
        }

        [Fact]
        public void Load_NonExistentFile_ReturnsNewFinanceData()
        {
            // Arrange
            string testFile = "nonexistent_file.json";
            var repository = new DataRepository(testFile);

            // Act
            var data = repository.Load();

            // Assert
            Assert.NotNull(data);
            Assert.Empty(data.Transactions);
            Assert.Empty(data.CustomCategories);
        }

        [Fact]
        public void Save_CreatesFileIfNotExists()
        {
            // Arrange
            string testFile = "test_create_file.json";
            if (File.Exists(testFile))
            {
                File.Delete(testFile);
            }

            var repository = new DataRepository(testFile);
            var data = new FinanceData();

            // Act
            repository.Save(data);

            // Assert
            Assert.True(File.Exists(testFile));

            // Cleanup
            File.Delete(testFile);
        }
    }

    // ============= ИНТЕГРАЦИОННЫЕ ТЕСТЫ =============

    public class IntegrationTests
    {
        [Fact]
        public void CompleteWorkflow_AddIncomeExpenseCheckBalance_WorksCorrectly()
        {
            // Arrange
            string testFile = "test_workflow.json";
            var repository = new DataRepository(testFile);
            var service = new TransactionService(repository);

            // Act
            service.AddTransaction(TransactionType.Income, "Зарплата", 50000m);
            service.AddTransaction(TransactionType.Expense, "Еда", 5000m);
            service.AddTransaction(TransactionType.Expense, "Транспорт", 3000m);

            var balance = service.CalculateBalance();
            var transactions = service.GetAllTransactions();

            // Assert
            Assert.Equal(42000m, balance);
            Assert.Equal(3, transactions.Count);

            // Cleanup
            File.Delete(testFile);
        }

        [Fact]
        public void BudgetTracking_WithExpenses_CalculatesCorrectly()
        {
            // Arrange
            string testFile = "test_budget_tracking.json";
            var repository = new DataRepository(testFile);
            var service = new TransactionService(repository);
            var now = DateTime.Now;

            // Act
            service.SetBudget(10000m, now.Month, now.Year);
            service.AddTransaction(TransactionType.Expense, "Еда", 3000m);
            service.AddTransaction(TransactionType.Expense, "Транспорт", 2000m);

            var stats = service.GetCategoryStatistics(now.Month, now.Year, TransactionType.Expense);
            var totalExpense = stats.Values.Sum();
            var budget = service.GetCurrentBudget();
            var remaining = budget.MonthlyLimit - totalExpense;

            // Assert
            Assert.Equal(5000m, totalExpense);
            Assert.Equal(5000m, remaining);
            Assert.True(totalExpense < budget.MonthlyLimit);

            // Cleanup
            File.Delete(testFile);
        }

        [Fact]
        public void DataPersistence_ServiceRestart_DataIsPreserved()
        {
            // Arrange
            string testFile = "test_persistence.json";

            // Первая сессия
            var repository1 = new DataRepository(testFile);
            var service1 = new TransactionService(repository1);
            service1.AddTransaction(TransactionType.Income, "Зарплата", 50000m);
            service1.AddTransaction(TransactionType.Expense, "Еда", 5000m);
            var balance1 = service1.CalculateBalance();

            // Вторая сессия (имитация перезапуска)
            var repository2 = new DataRepository(testFile);
            var service2 = new TransactionService(repository2);
            var balance2 = service2.CalculateBalance();
            var transactions = service2.GetAllTransactions();

            // Assert
            Assert.Equal(balance1, balance2);
            Assert.Equal(45000m, balance2);
            Assert.Equal(2, transactions.Count);

            // Cleanup
            File.Delete(testFile);
        }
    }

    // ============= ТЕСТЫ ГРАНИЧНЫХ СЛУЧАЕВ =============

    public class EdgeCaseTests
    {
        [Fact]
        public void AddTransaction_VeryLargeAmount_HandlesCorrectly()
        {
            // Arrange
            string testFile = "test_large_amount.json";
            var repository = new DataRepository(testFile);
            var service = new TransactionService(repository);

            // Act
            service.AddTransaction(TransactionType.Income, "Лотерея", 999999999.99m);
            var balance = service.CalculateBalance();

            // Assert
            Assert.Equal(999999999.99m, balance);

            // Cleanup
            File.Delete(testFile);
        }

        [Fact]
        public void AddTransaction_VerySmallAmount_HandlesCorrectly()
        {
            // Arrange
            string testFile = "test_small_amount.json";
            var repository = new DataRepository(testFile);
            var service = new TransactionService(repository);

            // Act
            service.AddTransaction(TransactionType.Expense, "Мелочь", 0.01m);
            var balance = service.CalculateBalance();

            // Assert
            Assert.Equal(-0.01m, balance);

            // Cleanup
            File.Delete(testFile);
        }

        [Fact]
        public void GetCategoryStatistics_SameCategoryMultipleTimes_SumsCorrectly()
        {
            // Arrange
            string testFile = "test_same_category.json";
            var repository = new DataRepository(testFile);
            var service = new TransactionService(repository);
            var now = DateTime.Now;

            // Act
            service.AddTransaction(TransactionType.Expense, "Еда", 1000m);
            service.AddTransaction(TransactionType.Expense, "Еда", 2000m);
            service.AddTransaction(TransactionType.Expense, "Еда", 3000m);

            var stats = service.GetCategoryStatistics(now.Month, now.Year, TransactionType.Expense);

            // Assert
            Assert.Single(stats);
            Assert.Equal(6000m, stats["Еда"]);

            // Cleanup
            File.Delete(testFile);
        }

        [Fact]
        public void AddTransaction_LongDescription_SavesCorrectly()
        {
            // Arrange
            string testFile = "test_long_description.json";
            var repository = new DataRepository(testFile);
            var service = new TransactionService(repository);
            string longDescription = new string('А', 500);

            // Act
            service.AddTransaction(TransactionType.Expense, "Разное", 1000m, longDescription);
            var transactions = service.GetAllTransactions();

            // Assert
            Assert.Equal(longDescription, transactions[0].Description);

            // Cleanup
            File.Delete(testFile);
        }

        [Fact]
        public void AddTransaction_SpecialCharactersInCategory_HandlesCorrectly()
        {
            // Arrange
            string testFile = "test_special_chars.json";
            var repository = new DataRepository(testFile);
            var service = new TransactionService(repository);
            string specialCategory = "Кафе \"Уют\" & Ресторан №1";

            // Act
            service.AddTransaction(TransactionType.Expense, specialCategory, 1500m);
            var transactions = service.GetAllTransactions();

            // Assert
            Assert.Equal(specialCategory, transactions[0].Category);

            // Cleanup
            File.Delete(testFile);
        }
    }
}