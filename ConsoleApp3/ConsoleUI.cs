using PersonalFinanceTracker.Core.Models;
using PersonalFinanceTracker.Core.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PersonalFinanceTracker.App
{
    public class ConsoleUI
    {
        private readonly TransactionService _service;
        private readonly List<string> _defaultExpenseCategories = new List<string>
        {
            "Еда",
            "Транспорт",
            "Развлечения",
            "Коммунальные услуги",
            "Здоровье",
            "Образование",
            "Одежда",
            "Другое"
        };

        private readonly List<string> _defaultIncomeCategories = new List<string>
        {
            "Зарплата",
            "Подработка",
            "Инвестиции",
            "Подарки",
            "Другое"
        };

        public ConsoleUI(TransactionService service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
        }

        public void Run()
        {
            while (true)
            {
                Console.Clear();
                ShowHeader();
                ShowMainMenu();

                string choice = Console.ReadLine();

                switch (choice)
                {
                    case "1":
                        AddIncome();
                        break;
                    case "2":
                        AddExpense();
                        break;
                    case "3":
                        ShowHistory();
                        break;
                    case "4":
                        ShowStatistics();
                        break;
                    case "5":
                        SetBudget();
                        break;
                    case "6":
                        ManageCategories();
                        break;
                    case "0":
                        return;
                    default:
                        Console.WriteLine("\n Неверный выбор. Попробуйте снова.");
                        Pause();
                        break;
                }
            }
        }

        private void ShowHeader()
        {
            Console.WriteLine("СИСТЕМА УЧЕТА ЛИЧНЫХ ФИНАНСОВ");

            decimal balance = _service.CalculateBalance();
            Console.WriteLine($"\nТекущий баланс: {balance:N2} руб.");

            var budget = _service.GetCurrentBudget();
            var now = DateTime.Now;

            if (budget != null && budget.MonthlyLimit > 0
                && budget.Month == now.Month && budget.Year == now.Year)
            {
                var expenses = _service.GetCategoryStatistics(now.Month, now.Year, TransactionType.Expense);
                decimal totalExpenses = expenses.Values.Sum();
                decimal remaining = budget.MonthlyLimit - totalExpenses;

                Console.WriteLine($"Бюджет на месяц: {budget.MonthlyLimit:N2} руб.");
                Console.WriteLine($"Потрачено: {totalExpenses:N2} руб. ({(totalExpenses / budget.MonthlyLimit * 100):N1}%)");
                Console.WriteLine($"Осталось: {remaining:N2} руб.");

                if (totalExpenses > budget.MonthlyLimit)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("БЮДЖЕТ ПРЕВЫШЕН!");
                    Console.ResetColor();
                }
                else if (remaining < budget.MonthlyLimit * 0.2m)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("Осталось менее 20% бюджета");
                    Console.ResetColor();
                }
            }

            Console.WriteLine();
        }

        private void ShowMainMenu()
        {
            Console.WriteLine("Главное меню:");
            Console.WriteLine("1. Добавить доход");
            Console.WriteLine("2. Добавить расход");
            Console.WriteLine("3. Просмотреть историю операций");
            Console.WriteLine("4. Показать статистику");
            Console.WriteLine("5. Установить бюджет");
            Console.WriteLine("6. Управление категориями");
            Console.WriteLine("0. Выход");
            Console.Write("\nВыберите действие: ");
        }

        private void AddIncome()
        {
            Console.Clear();
            Console.WriteLine("ДОБАВЛЕНИЕ ДОХОДА\n");

            string category = SelectCategory(_defaultIncomeCategories, "дохода");
            if (category == null) return;

            decimal amount = ReadDecimal("Введите сумму: ");
            if (amount <= 0)
            {
                Console.WriteLine("\nСумма должна быть положительной!");
                Pause();
                return;
            }

            Console.Write("Введите описание (необязательно): ");
            string description = Console.ReadLine();

            try
            {
                _service.AddTransaction(TransactionType.Income, category, amount, description);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("\n✓ Доход успешно добавлен!");
                Console.ResetColor();
                Console.WriteLine($"Текущий баланс: {_service.CalculateBalance():N2} руб.");
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\n Ошибка: {ex.Message}");
                Console.ResetColor();
            }

            Pause();
        }

        private void AddExpense()
        {
            Console.Clear();
            Console.WriteLine("ДОБАВЛЕНИЕ РАСХОДА\n");

            var allCategories = new List<string>(_defaultExpenseCategories);
            allCategories.AddRange(_service.GetCustomCategories());

            string category = SelectCategory(allCategories, "расхода");
            if (category == null) return;

            decimal amount = ReadDecimal("Введите сумму: ");
            if (amount <= 0)
            {
                Console.WriteLine("\nСумма должна быть положительной!");
                Pause();
                return;
            }

            Console.Write("Введите описание (необязательно): ");
            string description = Console.ReadLine();

            try
            {
                _service.AddTransaction(TransactionType.Expense, category, amount, description);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("\nРасход успешно добавлен!");
                Console.ResetColor();
                Console.WriteLine($"Текущий баланс: {_service.CalculateBalance():N2} руб.");
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\n❌ Ошибка: {ex.Message}");
                Console.ResetColor();
            }

            Pause();
        }

        private void ShowHistory()
        {
            Console.Clear();
            Console.WriteLine("ИСТОРИЯ ОПЕРАЦИЙ\n");

            var transactions = _service.GetAllTransactions();

            if (transactions.Count == 0)
            {
                Console.WriteLine("История операций пуста.");
                Pause();
                return;
            }

            Console.WriteLine($"{"Дата",-20} {"Тип",-10} {"Категория",-20} {"Сумма",15} {"Описание",-30}");
            Console.WriteLine(new string('─', 100));

            foreach (var t in transactions.Take(20))
            {
                string type = t.Type == TransactionType.Income ? "Доход" : "Расход";
                ConsoleColor color = t.Type == TransactionType.Income ? ConsoleColor.Green : ConsoleColor.Red;
                string sign = t.Type == TransactionType.Income ? "+" : "-";

                Console.Write($"{t.Date:dd.MM.yyyy HH:mm}    {type,-10} {t.Category,-20} ");
                Console.ForegroundColor = color;
                Console.Write($"{sign}{t.Amount,14:N2}");
                Console.ResetColor();
                Console.WriteLine($" {t.Description,-30}");
            }

            if (transactions.Count > 20)
            {
                Console.WriteLine($"\n... и еще {transactions.Count - 20} операций");
            }

            Pause();
        }

        private void ShowStatistics()
        {
            Console.Clear();
            Console.WriteLine("СТАТИСТИКА\n");

            var now = DateTime.Now;
            Console.WriteLine($"Период: {now:MMMM yyyy}\n");

            var incomes = _service.GetCategoryStatistics(now.Month, now.Year, TransactionType.Income);
            var expenses = _service.GetCategoryStatistics(now.Month, now.Year, TransactionType.Expense);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("ДОХОДЫ:");
            Console.ResetColor();

            if (incomes.Count > 0)
            {
                foreach (var item in incomes.OrderByDescending(x => x.Value))
                {
                    Console.WriteLine($"  {item.Key,-25} {item.Value,12:N2} руб.");
                }
                Console.WriteLine($"  {"ИТОГО доходов:",-25} {incomes.Values.Sum(),12:N2} руб.");
            }
            else
            {
                Console.WriteLine("  Нет доходов за этот период");
            }

            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("РАСХОДЫ:");
            Console.ResetColor();

            if (expenses.Count > 0)
            {
                foreach (var item in expenses.OrderByDescending(x => x.Value))
                {
                    Console.WriteLine($"  {item.Key,-25} {item.Value,12:N2} руб.");
                }
                Console.WriteLine($"  {"ИТОГО расходов:",-25} {expenses.Values.Sum(),12:N2} руб.");

                var maxExpense = expenses.OrderByDescending(x => x.Value).First();
                Console.WriteLine($"\nБольше всего потрачено на: {maxExpense.Key} ({maxExpense.Value:N2} руб.)");
            }
            else
            {
                Console.WriteLine("Нет расходов за этот период");
            }

            Console.WriteLine($"\n{"=",-38}");
            decimal balance = _service.CalculateBalance();
            Console.WriteLine($"Текущий баланс: {balance,20:N2} руб.");

            Pause();
        }

        private void SetBudget()
        {
            Console.Clear();
            Console.WriteLine("УСТАНОВКА БЮДЖЕТА\n");

            decimal amount = ReadDecimal("Введите месячный бюджет (руб.): ");

            if (amount < 0)
            {
                Console.WriteLine("\nБюджет не может быть отрицательным!");
                Pause();
                return;
            }

            var now = DateTime.Now;
            _service.SetBudget(amount, now.Month, now.Year);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"\nБюджет на {now:MMMM yyyy} установлен: {amount:N2} руб.");
            Console.ResetColor();

            Pause();
        }

        private void ManageCategories()
        {
            Console.Clear();
            Console.WriteLine("УПРАВЛЕНИЕ КАТЕГОРИЯМИ\n");

            var customCategories = _service.GetCustomCategories();

            Console.WriteLine("Пользовательские категории:");
            if (customCategories.Count > 0)
            {
                foreach (var cat in customCategories)
                {
                    Console.WriteLine($"  • {cat}");
                }
            }
            else
            {
                Console.WriteLine("(нет пользовательских категорий)");
            }

            Console.Write("\nДобавить новую категорию (Enter для отмены): ");
            string newCategory = Console.ReadLine()?.Trim();

            if (!string.IsNullOrEmpty(newCategory))
            {
                _service.AddCustomCategory(newCategory);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"\nКатегория '{newCategory}' добавлена!");
                Console.ResetColor();
            }

            Pause();
        }

        private string SelectCategory(List<string> categories, string type)
        {
            Console.WriteLine($"Выберите категорию {type}:");

            for (int i = 0; i < categories.Count; i++)
            {
                Console.WriteLine($"{i + 1}. {categories[i]}");
            }

            Console.Write("\nВаш выбор: ");

            if (int.TryParse(Console.ReadLine(), out int choice)
                && choice > 0 && choice <= categories.Count)
            {
                return categories[choice - 1];
            }

            Console.WriteLine("\nНеверный выбор");
            Pause();
            return null;
        }

        private decimal ReadDecimal(string prompt)
        {
            Console.Write(prompt);
            decimal.TryParse(Console.ReadLine(), out decimal result);
            return result;
        }

        private void Pause()
        {
            Console.WriteLine("\n[Нажмите Enter для продолжения]");
            Console.ReadLine();
        }
    }
}
