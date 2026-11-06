// ============================================================================
// Copyright (c) 2026 Ed. Licensed under the MIT License (see LICENSE).
// AI-GENERATED CODE (partial): Claude (Anthropic) added this header and keeps the
// main window in a field for the app's lifetime. The rest predates Claude's work.
// SecretLetter is a project built with AI-generated code; see README.md.
// ============================================================================
using Microsoft.UI.Xaml;

namespace SecretLetter.UI
{
    public partial class App : Application
    {
        private Window? _window;

        public App()
        {
            this.InitializeComponent();
        }

        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            _window = new MainWindow();
            _window.Activate();
        }
    }
}
