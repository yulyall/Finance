using PersonalFinanceTracker.Core.Data;
using PersonalFinanceTracker.Core.Services;
using System;

namespace PersonalFinanceTracker.App
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            try
            {
                var repository = new DataRepository();
                var service = new TransactionService(repository);
                var ui = new ConsoleUI(service);

                ui.Run();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\nКритическая ошибка: {ex.Message}");
                Console.ResetColor();
                Console.WriteLine("\nНажмите Enter для выхода...");
                Console.ReadLine();
            }
        }
    }
}
