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
        private readonly string _settingsFileName = "appsettings.json";

        public DataService()
        {
        }
        public async Task SaveDataAsync(SaveData data, string filePath)
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };

                var json = JsonSerializer.Serialize(data, options);
                await File.WriteAllTextAsync(filePath, json);

                Console.WriteLine($"Данные сохранены в: {filePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка сохранения: {ex.Message}");
                throw;
            }
        }
        public async Task<SaveData?> LoadDataAsync(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    Console.WriteLine($"Файл не найден: {filePath}");
                    return new SaveData();
                }

                var json = await File.ReadAllTextAsync(filePath);
                var result = JsonSerializer.Deserialize<SaveData>(json);

                Console.WriteLine($"Данные загружены из: {filePath}");
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки: {ex.Message}");
                throw;
            }
        }
        private string GetSettingsFilePath()
        {
            var appDirectory = AppContext.BaseDirectory;
            return Path.Combine(appDirectory, _settingsFileName);
        }
        public async Task SaveSettingsAsync(AppSettings settings)
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };

                var json = JsonSerializer.Serialize(settings, options);
                await File.WriteAllTextAsync(GetSettingsFilePath(), json);

                Console.WriteLine($"Настройки сохранены в: {GetSettingsFilePath()}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка сохранения настроек: {ex.Message}");
                throw;
            }
        }
        public async Task<AppSettings> LoadSettingsAsync()
        {
            try
            {
                var filePath = GetSettingsFilePath();
                if (!File.Exists(filePath))
                {
                    Console.WriteLine("Файл настроек не найден, создаем новый");
                    return new AppSettings();
                }

                var json = await File.ReadAllTextAsync(filePath);
                var settings = JsonSerializer.Deserialize<AppSettings>(json);

                if (settings == null)
                {
                    Console.WriteLine("Ошибка десериализации настроек, создаем новые");
                    return new AppSettings();
                }

                Console.WriteLine("Настройки загружены");
                return settings;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки настроек: {ex.Message}");
                return new AppSettings();
            }
        }
    }
}