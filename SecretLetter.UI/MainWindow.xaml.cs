// ============================================================================
// Copyright (c) 2026 Ed. Licensed under the MIT License (see LICENSE).
// AI-GENERATED CODE: This file was written by Claude (Anthropic), replacing the
// original window code. All checks, settings and encryption are done by
// SecretLetter.Core, which has the tests; this file only connects them to the window.
// SecretLetter is a project built with AI-generated code; see README.md.
// ============================================================================
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;
// Exact-name imports so these can't clash with similarly named WinUI types.
using AppConfig = SecretLetter.Core.AppConfig;
using LetterFiles = SecretLetter.Core.LetterFiles;
using LetterOperation = SecretLetter.Core.LetterOperation;
using SecretLetterCrypto = SecretLetter.Core.SecretLetterCrypto;
using UserSettings = SecretLetter.Core.UserSettings;
using Validation = SecretLetter.Core.Validation;

namespace SecretLetter.UI
{
    public sealed partial class MainWindow : Window
    {
        // The window's size at 100% display scaling; it is scaled up on high-DPI screens.
        private const int WindowWidth = 760;
        private const int WindowHeight = 720;

        private string _configFolder = string.Empty;

        public MainWindow()
        {
            this.InitializeComponent();
            this.Activated += MainWindow_Activated;

            UpdateForInput();
            ReopenLastConfigFolder();
        }

        // -------------------------------
        // WINDOW SIZE (once, at start-up)
        // -------------------------------
        private void MainWindow_Activated(object sender, WindowActivatedEventArgs args)
        {
            this.Activated -= MainWindow_Activated;

            IntPtr hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);
            var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);
            if (appWindow != null)
            {
                // Resize takes physical pixels, so scale by the screen's DPI (96 = 100%).
                double scale = Math.Max(1.0, GetDpiForWindow(hWnd) / 96.0);
                appWindow.Resize(new Windows.Graphics.SizeInt32((int)(WindowWidth * scale), (int)(WindowHeight * scale)));
            }
        }

        [DllImport("user32.dll")]
        private static extern uint GetDpiForWindow(IntPtr hWnd);

        // -------------------------------
        // CONFIG FOLDER
        // -------------------------------
        private async void BrowseConfigFolderButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var picker = new FolderPicker();
                InitializePicker(picker);
                picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
                picker.FileTypeFilter.Add("*");

                StorageFolder? folder = await picker.PickSingleFolderAsync();
                if (folder != null)
                {
                    OpenConfigFolder(folder.Path, createIfMissing: true);
                }
            }
            catch (Exception ex)
            {
                ShowStatus("The config folder could not be opened: " + ex.Message, InfoBarSeverity.Error);
            }
        }

        /// <summary>At start-up, reopens the config folder used last time, if its config.json is still there.</summary>
        private void ReopenLastConfigFolder()
        {
            try
            {
                string folder = UserSettings.Load(UserSettings.DefaultPath).LastConfigFolder;
                if (folder.Length > 0 && File.Exists(AppConfig.PathIn(folder)))
                {
                    OpenConfigFolder(folder, createIfMissing: false);
                }
            }
            catch (Exception ex)
            {
                ShowStatus("The last config folder could not be reopened: " + ex.Message, InfoBarSeverity.Warning);
            }
        }

        /// <summary>
        /// Loads config.json from the folder. A folder without one gets a new config.json with a
        /// freshly generated key (when <paramref name="createIfMissing"/> is true).
        /// </summary>
        private void OpenConfigFolder(string folder, bool createIfMissing)
        {
            string configPath = AppConfig.PathIn(folder);
            bool created = false;
            AppConfig config;

            if (File.Exists(configPath))
            {
                config = AppConfig.Load(configPath);
            }
            else if (createIfMissing)
            {
                config = new AppConfig { RootKey = SecretLetterCrypto.GenerateRootKey() };
                config.Save(configPath);
                created = true;
            }
            else
            {
                return;
            }

            _configFolder = folder;
            ConfigFolderPathBox.Text = folder;
            InputPathBox.Text = config.InputPath;
            OutputPathBox.Text = config.OutputPath;
            RootKeyBox.Text = config.RootKey;
            RememberConfigFolder(folder);

            if (created)
            {
                ShowStatus($"Created config.json with a new key in {folder}. Keep this folder private, and keep a backup of the key.", InfoBarSeverity.Success);
            }
            else
            {
                ShowStatus($"Loaded config.json from {folder}.", InfoBarSeverity.Success);
            }
        }

        private static void RememberConfigFolder(string folder)
        {
            try
            {
                new UserSettings { LastConfigFolder = folder }.Save(UserSettings.DefaultPath);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // Only a convenience: the next start will ask for the folder again.
            }
        }

        // -------------------------------
        // INPUT AND OUTPUT FILES
        // -------------------------------
        private async void BrowseInputButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var picker = new FileOpenPicker();
                InitializePicker(picker);
                picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
                picker.FileTypeFilter.Add(LetterFiles.LetterExtension);
                picker.FileTypeFilter.Add(LetterFiles.EncryptedExtension);

                StorageFile? file = await picker.PickSingleFileAsync();
                if (file != null)
                {
                    InputPathBox.Text = file.Path;
                    OutputPathBox.Text = LetterFiles.SuggestOutputPath(file.Path, OutputPathBox.Text);
                }
            }
            catch (Exception ex)
            {
                ShowStatus("The input file could not be chosen: " + ex.Message, InfoBarSeverity.Error);
            }
        }

        private async void BrowseOutputButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string input = InputPathBox.Text.Trim();
                LetterOperation operation = LetterFiles.OperationFor(input);
                if (operation == LetterOperation.None)
                {
                    ShowStatus("Choose a .md letter or a .file as the input first.", InfoBarSeverity.Error);
                    return;
                }

                string extension = LetterFiles.OutputExtensionFor(operation);
                string suggested = LetterFiles.SuggestOutputPath(input, OutputPathBox.Text);

                var picker = new FileSavePicker();
                InitializePicker(picker);
                picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
                picker.FileTypeChoices.Add(
                    operation == LetterOperation.Encrypt ? "Encrypted letter" : "Markdown letter",
                    new List<string> { extension });
                picker.SuggestedFileName = Path.GetFileNameWithoutExtension(suggested);

                StorageFile? file = await picker.PickSaveFileAsync();
                if (file != null)
                {
                    OutputPathBox.Text = file.Path;
                }
            }
            catch (Exception ex)
            {
                ShowStatus("The output file could not be chosen: " + ex.Message, InfoBarSeverity.Error);
            }
        }

        private void InputPathBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateForInput();
        }

        /// <summary>Names the Run button after what it will do, and asks for the passphrase twice only when encrypting.</summary>
        private void UpdateForInput()
        {
            LetterOperation operation = LetterFiles.OperationFor(InputPathBox.Text);
            RunButton.Content = operation switch
            {
                LetterOperation.Encrypt => "Encrypt",
                LetterOperation.Decrypt => "Decrypt",
                _ => "Run",
            };
            ConfirmPassphraseBox.Visibility = operation == LetterOperation.Encrypt ? Visibility.Visible : Visibility.Collapsed;
        }

        // -------------------------------
        // KEY
        // -------------------------------
        private async void GenerateKeyButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(RootKeyBox.Text))
                {
                    bool replace = await ConfirmAsync(
                        "Replace the key?",
                        "Letters encrypted with the current key can only be decrypted with that key. " +
                        "If you replace it, keep a copy of the current key until those letters are decrypted.",
                        "Replace key");
                    if (!replace)
                    {
                        return;
                    }
                }

                RootKeyBox.Text = SecretLetterCrypto.GenerateRootKey();
                ShowStatus("New key generated. Click Save Config to keep it in config.json.", InfoBarSeverity.Warning);
            }
            catch (Exception ex)
            {
                ShowStatus("No key was generated: " + ex.Message, InfoBarSeverity.Error);
            }
        }

        // -------------------------------
        // SAVE CONFIG
        // -------------------------------
        private void SaveConfigButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_configFolder))
            {
                ShowStatus("Choose a config folder first.", InfoBarSeverity.Error);
                return;
            }

            string rootKey = RootKeyBox.Text.Trim();
            string? problem = Validation.RootKeyError(rootKey);
            if (problem != null)
            {
                ShowStatus("Not saved: " + problem, InfoBarSeverity.Error);
                return;
            }

            try
            {
                var config = new AppConfig
                {
                    InputPath = InputPathBox.Text.Trim(),
                    OutputPath = OutputPathBox.Text.Trim(),
                    RootKey = rootKey,
                };
                config.Save(AppConfig.PathIn(_configFolder));
                ShowStatus($"Saved config.json in {_configFolder}.", InfoBarSeverity.Success);
            }
            catch (Exception ex)
            {
                ShowStatus("Not saved: " + ex.Message, InfoBarSeverity.Error);
            }
        }

        // -------------------------------
        // ENCRYPT OR DECRYPT
        // -------------------------------
        private async void RunButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Read everything from the window first; the work itself runs off the UI thread.
                string input = InputPathBox.Text.Trim();
                string output = OutputPathBox.Text.Trim();
                string rootKey = RootKeyBox.Text.Trim();
                string passphrase = PassphraseBox.Password;
                LetterOperation operation = LetterFiles.OperationFor(input);

                string? problem = LetterFiles.PathsError(input, output)
                    ?? Validation.RootKeyError(rootKey)
                    ?? Validation.PassphraseError(passphrase);
                if (problem == null && operation == LetterOperation.Encrypt && passphrase != ConfirmPassphraseBox.Password)
                {
                    problem = "The two passphrases are different. Type the same passphrase in both boxes.";
                }
                if (problem != null)
                {
                    ShowStatus(problem, InfoBarSeverity.Error);
                    return;
                }

                // A file picker may already have created an empty output file; replacing that loses nothing.
                if (File.Exists(output) && new FileInfo(output).Length > 0)
                {
                    bool replace = await ConfirmAsync(
                        "Replace the existing file?",
                        $"{output} already exists. Do you want to replace it?",
                        "Replace");
                    if (!replace)
                    {
                        ShowStatus("Nothing was written.", InfoBarSeverity.Informational);
                        return;
                    }
                }

                bool encrypt = operation == LetterOperation.Encrypt;
                SetBusy(true);
                ShowStatus(encrypt ? "Encrypting..." : "Decrypting...", InfoBarSeverity.Informational);

                await Task.Run(() =>
                {
                    if (encrypt)
                    {
                        SecretLetterCrypto.EncryptFile(input, output, rootKey, passphrase);
                    }
                    else
                    {
                        SecretLetterCrypto.DecryptFile(input, output, rootKey, passphrase);
                    }
                });

                PassphraseBox.Password = string.Empty;
                ConfirmPassphraseBox.Password = string.Empty;
                ShowStatus($"{(encrypt ? "Encrypted" : "Decrypted")}: {output}", InfoBarSeverity.Success);
            }
            catch (Exception ex)
            {
                ShowStatus(ex.Message, InfoBarSeverity.Error);
            }
            finally
            {
                SetBusy(false);
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
        // HELPERS
        // -------------------------------
        private void InitializePicker(object picker)
        {
            IntPtr hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hWnd);
        }

        private async Task<bool> ConfirmAsync(string title, string message, string confirmText)
        {
            var dialog = new ContentDialog
            {
                Title = title,
                Content = new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap },
                PrimaryButtonText = confirmText,
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = this.Content.XamlRoot,
            };
            try
            {
                // The current WinUI look for dialogs (from XamlControlsResources in App.xaml).
                dialog.Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style;
            }
            catch (Exception)
            {
                // Without the style the dialog still works, with its built-in look.
            }
            return await dialog.ShowAsync() == ContentDialogResult.Primary;
        }

        private void SetBusy(bool busy)
        {
            BusyRing.IsActive = busy;
            RunButton.IsEnabled = !busy;
            SaveConfigButton.IsEnabled = !busy;
            GenerateKeyButton.IsEnabled = !busy;
            BrowseConfigFolderButton.IsEnabled = !busy;
            BrowseInputButton.IsEnabled = !busy;
            BrowseOutputButton.IsEnabled = !busy;
        }

        private void ShowStatus(string message, InfoBarSeverity severity)
        {
            StatusBar.Message = message;
            StatusBar.Severity = severity;
        }
    }
}
