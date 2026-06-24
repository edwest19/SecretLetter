using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text.Json;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace SecretLetter.UI
{
    public sealed partial class MainWindow : Window
    {
        private string _configFolder = "";
        private string _configPath = "";

        public class ConfigModel
        {
            public string InputPath { get; set; }
            public string OutputPath { get; set; }
            public string RootKey { get; set; }
        }
        public MainWindow()
        {
            this.InitializeComponent();

            // Set window size (WinUI 3 / WindowsAppSDK 2026)
            this.AppWindow.Resize(new Windows.Graphics.SizeInt32(700, 600));
        }

        private string GetAutoOutputPath(string inputPath)
        {
            if (inputPath.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
            {
                // Encrypt → output is .file
                return Path.ChangeExtension(inputPath, ".file");
            }

            if (inputPath.EndsWith(".file", StringComparison.OrdinalIgnoreCase))
            {
                // Decrypt → output is .md
                return Path.ChangeExtension(inputPath, ".md");
            }

            throw new InvalidOperationException("Input file must be .md or .file");
        }



        // -------------------------------
        // BROWSE CONFIG FOLDER
        // -------------------------------
        private async void BrowseConfigFolderButton_Click(object sender, RoutedEventArgs e)
        {
            var picker = new Windows.Storage.Pickers.FolderPicker();
            picker.FileTypeFilter.Add("*");

            // REQUIRED for WinUI 3 — this makes the picker look like your screenshot
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

            StorageFolder folder = await picker.PickSingleFolderAsync();
            if (folder == null)
                return;

            _configFolder = folder.Path;
            ConfigFolderPathBox.Text = _configFolder;

            _configPath = Path.Combine(_configFolder, "config.json");

            if (!File.Exists(_configPath))
            {
                var cfg = new ConfigModel
                {
                    InputPath = "",
                    OutputPath = "",
                    RootKey = GenerateKey()
                };

                File.WriteAllText(_configPath, JsonSerializer.Serialize(cfg, new JsonSerializerOptions { WriteIndented = true }));
            }

            LoadConfig();
        }


        // -------------------------------
        // LOAD CONFIG.JSON
        // -------------------------------
        private void LoadConfig()
        {
            try
            {
                var json = File.ReadAllText(_configPath);
                var cfg = JsonSerializer.Deserialize<ConfigModel>(json);

                InputPathBox.Text = cfg.InputPath;
                OutputPathBox.Text = cfg.OutputPath;
                RootKeyBox.Text = cfg.RootKey;

                SetStatus("Config loaded", true);
            }
            catch (Exception ex)
            {
                SetStatus("Config load failed: " + ex.Message, false);
            }
        }

        // -------------------------------
        // BROWSE INPUT FILE
        // -------------------------------
        private async void BrowseInputButton_Click(object sender, RoutedEventArgs e)
        {
            var picker = new Windows.Storage.Pickers.FileOpenPicker();
            picker.FileTypeFilter.Add(".md");
            picker.FileTypeFilter.Add(".file");

            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

            StorageFile file = await picker.PickSingleFileAsync();
            if (file != null)
                InputPathBox.Text = file.Path;
        }



        // -------------------------------
        // BROWSE OUTPUT FILE
        // -------------------------------
        private async void BrowseOutputButton_Click(object sender, RoutedEventArgs e)
        {
            string input = InputPathBox.Text;

            if (string.IsNullOrWhiteSpace(input))
            {
                SetStatus("Select an input file first", false);
                return;
            }

            var picker = new Windows.Storage.Pickers.FileSavePicker();
            picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;

            // Determine correct extension based on input
            string requiredExt;

            if (input.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
            {
                // Encrypt mode → output must be .file
                requiredExt = ".file";
                picker.FileTypeChoices.Add("Encrypted File", new List<string>() { ".file" });
            }
            else if (input.EndsWith(".file", StringComparison.OrdinalIgnoreCase))
            {
                // Decrypt mode → output must be .md
                requiredExt = ".md";
                picker.FileTypeChoices.Add("Markdown File", new List<string>() { ".md" });
            }
            else
            {
                SetStatus("Input must be .md or .file", false);
                return;
            }

            // Respect JSON filename — only change extension
            string currentOutput = OutputPathBox.Text;

            string suggestedName;

            if (!string.IsNullOrWhiteSpace(currentOutput))
            {
                // Use JSON filename but enforce correct extension
                suggestedName = Path.GetFileNameWithoutExtension(currentOutput) + requiredExt;
            }
            else
            {
                // Fallback: use input filename
                suggestedName = Path.GetFileNameWithoutExtension(input) + requiredExt;
            }

            picker.SuggestedFileName = suggestedName;

            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

            StorageFile file = await picker.PickSaveFileAsync();
            if (file != null)
            {
                OutputPathBox.Text = file.Path;
            }
        }





        // -------------------------------
        // GENERATE NEW ROOT KEY
        // -------------------------------
        private void GenerateKeyButton_Click(object sender, RoutedEventArgs e)
        {
            RootKeyBox.Text = GenerateKey();
            SetStatus("New key generated", true);
        }

        private string GenerateKey()
        {
            byte[] bytes = RandomNumberGenerator.GetBytes(32);
            return Convert.ToHexString(bytes);
        }

        // -------------------------------
        // SAVE CONFIG.JSON
        // -------------------------------
        private void SaveConfigButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_configFolder))
                {
                    SetStatus("Select config folder first", false);
                    return;
                }

                var cfg = new ConfigModel
                {
                    InputPath = InputPathBox.Text,
                    OutputPath = OutputPathBox.Text,
                    RootKey = RootKeyBox.Text
                };

                File.WriteAllText(_configPath, JsonSerializer.Serialize(cfg, new JsonSerializerOptions { WriteIndented = true }));

                SetStatus("Config saved", true);
            }
            catch (Exception ex)
            {
                SetStatus("Save failed: " + ex.Message, false);
            }
        }

        // -------------------------------
        // RUN SECRETLETTER
        // -------------------------------
        private void RunButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string input = InputPathBox.Text;
                string output = OutputPathBox.Text;
                string key = RootKeyBox.Text;
                string code = DateCodeBox.Text;

                if (!File.Exists(input))
                {
                    SetStatus("Input file does not exist", false);
                    return;
                }

                if (string.IsNullOrWhiteSpace(output))
                {
                    SetStatus("Output file path is empty", false);
                    return;
                }

                if (string.Equals(input, output, StringComparison.OrdinalIgnoreCase))
                {
                    SetStatus("Input and output cannot be the same file", false);
                    return;
                }

                bool encrypt = input.EndsWith(".md", StringComparison.OrdinalIgnoreCase);
                bool decrypt = input.EndsWith(".file", StringComparison.OrdinalIgnoreCase);

                if (!encrypt && !decrypt)
                {
                    SetStatus("Input must be .md or .file", false);
                    return;
                }

                // Enforce correct extension pairing
                if (encrypt && !output.EndsWith(".file", StringComparison.OrdinalIgnoreCase))
                {
                    SetStatus("Encrypting requires output to be .file", false);
                    return;
                }

                if (decrypt && !output.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
                {
                    SetStatus("Decrypting requires output to be .md", false);
                    return;
                }

                // Perform the transform
                SecretLetter.Core.SecretLetterCrypto.TransformFile(
                    input,
                    output,
                    key,
                    code
                );

                string mode = encrypt ? "Encrypted" : "Decrypted";
                SetStatus($"{mode} OK → {Path.GetFileName(output)}", true);
            }
            catch (Exception ex)
            {
                SetStatus("Error: " + ex.Message, false);
            }
        }





        // -------------------------------
        // CLOSE APP
        // -------------------------------
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        // -------------------------------
        // STATUS BAR
        // -------------------------------
        private void SetStatus(string msg, bool ok)
        {
            StatusText.Text = msg;
            StatusText.Foreground = ok ? new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Lime)
                                       : new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Red);
        }
    }
}
