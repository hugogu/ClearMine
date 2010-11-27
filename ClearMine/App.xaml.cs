﻿namespace ClearMine
{
    using System.ComponentModel;
    using System.Windows;
    using System.Windows.Threading;

    using ClearMine.Common.Properties;
    using ClearMine.Framework.Dialogs;
    using ClearMine.UI.Dialogs;

    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    internal partial class App : Application
    {
        public bool IsSettingsDirty { get; set; }

        private void OnAppStartup(object sender, StartupEventArgs e)
        {
            DispatcherUnhandledException += OnCurrentDispatcherUnhandledException;
            Exit += new ExitEventHandler(OnApplicationExit);
            Settings.Default.PropertyChanged += new PropertyChangedEventHandler(OnSettingsChanged);
            Settings.Default.SettingsSaving += new System.Configuration.SettingsSavingEventHandler(OnSavingSettings);
            var mainWindow = new ClearMineWindow();
            mainWindow.Show();
        }

        private void OnApplicationExit(object sender, ExitEventArgs e)
        {
            if (IsSettingsDirty)
            {
                Settings.Default.Save();
            }
        }

        private void OnSettingsChanged(object sender, PropertyChangedEventArgs e)
        {
            IsSettingsDirty = true;
        }

        private void OnSavingSettings(object sender, CancelEventArgs e)
        {
            IsSettingsDirty = e.Cancel;
        }

        private void OnCurrentDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            e.Handled = ExceptionBox.Show(e.Exception).Value;
        }
    }
}
