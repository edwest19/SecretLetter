using System;
using System.IO;
using System.Security.Cryptography;
using System.Text.Json;

namespace SecretLetter.Core
{
    public static class ConfigManager
    {
        /// <summary>
        /// Loads config.json from the specified folder.
        /// If missing, creates a new config with a fresh key.
        /// </summary>
        public static AppConfig LoadOrCreate(string folderPath)
        {
            Directory.CreateDirectory(folderPath);

            string cfgPath = Path.Combine(folderPath, "config.json");

            if (!File.Exists(cfgPath))
            {
                // Create new config with a fresh key
                var cfg = new AppConfig
                {
                    LetterKey = GenerateKey(),
                    InputPath = "",
                    OutputPath = ""
                };

                Save(folderPath, cfg);
                return cfg;
            }

            // Load existing config
            string json = File.ReadAllText(cfgPath);
            return JsonSerializer.Deserialize<AppConfig>(json)
                   ?? new AppConfig();
        }

        /// <summary>
        /// Saves the config.json file to the specified folder.
        /// </summary>
        public static void Save(string folderPath, AppConfig cfg)
        {
            Directory.CreateDirectory(folderPath);

            string cfgPath = Path.Combine(folderPath, "config.json");

            string json = JsonSerializer.Serialize(
                cfg,
                new JsonSerializerOptions { WriteIndented = true });

            File.WriteAllText(cfgPath, json);
        }

        /// <summary>
        /// Generates a new 32-byte (256-bit) key and returns it as 64 hex chars.
        /// </summary>
        public static string GenerateKey()
        {
            byte[] key = RandomNumberGenerator.GetBytes(32);
            return Convert.ToHexString(key); // uppercase hex
        }
    }
}
