using AxxonSoft_OSM_.Models.DataModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace AxxonSoft_OSM_.Services
{
    public class DataService
    {
        private readonly string _dataFileName = "mapdata.json";

        public DataService()
        {
        }

        public string GetDataFilePath()
        {
            var appDirectory = AppContext.BaseDirectory;
            return Path.Combine(appDirectory, _dataFileName);
        }

        public async Task SaveDataAsync(SaveData data)
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };

                var json = JsonSerializer.Serialize(data, options);
                await File.WriteAllTextAsync(GetDataFilePath(), json);

                Console.WriteLine($"Данные сохранены в: {GetDataFilePath()}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка сохранения: {ex.Message}");
            }
        }

        public async Task<SaveData?> LoadDataAsync()
        {
            try
            {
                var filePath = GetDataFilePath();
                if (!File.Exists(filePath))
                    return new SaveData();

                var json = await File.ReadAllTextAsync(filePath);
                return JsonSerializer.Deserialize<SaveData>(json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки: {ex.Message}");
                return new SaveData();
            }
        }
    }
}