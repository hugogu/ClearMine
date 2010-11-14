﻿using System;
using System.Windows;
using System.Windows.Threading;

using ClearMine.Common.Log;

namespace ClearMine.UI.Dialogs
{
    /// <summary>
    /// Interaction logic for OutputWindow.xaml
    /// </summary>
    public partial class OutputWindow : Window
    {
        public OutputWindow()
        {
            InitializeComponent();
            RedirectorTraceListener.Current.ShowMessage += OnInstanceShowMessage;
            logBox.Text = RedirectorTraceListener.Current.Log;
        }

        private void OnInstanceShowMessage(object sender, TraceEventArgs e)
        {
            if (this.Dispatcher == Dispatcher.CurrentDispatcher)
            {
                logBox.Text = RedirectorTraceListener.Current.Log;
                logBox.ScrollToEnd();
            }
            else
            {
                Dispatcher.Invoke((Action)delegate
                {
                    logBox.Text = RedirectorTraceListener.Current.Log;
                    logBox.ScrollToEnd();
                });
            }
        }

        private void OnClearClick(object sender, RoutedEventArgs e)
        {
            logBox.Clear();
            RedirectorTraceListener.Current.Clear();
        }
    }
}