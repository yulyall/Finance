using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using PersonalFinanceTracker.Core.Models;
using System.Text.Json;

namespace PersonalFinanceTracker.Core.Data
{
    public class DataRepository
    {
        private readonly string _filePath;

        public DataRepository(string filePath = "finance_data.json")
        {
            _filePath = filePath;
        }

        public FinanceData Load()
        {
            if (!File.Exists(_filePath))
                return new FinanceData();

            try
            {
                string json = File.ReadAllText(_filePath);
                return JsonSerializer.Deserialize<FinanceData>(json) ?? new FinanceData();
            }
            catch
            {
                return new FinanceData();
            }
        }

        public void Save(FinanceData data)
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(data, options);
            File.WriteAllText(_filePath, json);
        }
    }
}
