﻿namespace ClearMine.Common.Utilities
{
    using System;
    using System.Threading;
    using System.Windows;

    using ClearMine.Common.Messaging;
    using Microsoft.Win32;

    /// <summary>
    /// 
    /// </summary>
    public class LanguageSwitcher : ResourceSwitcher
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="stringFormat"></param>
        /// <param name="validTypes"></param>
        /// <param name="supportCustom"></param>
        public LanguageSwitcher(string stringFormat, Type[] validTypes, bool supportCustom)
            : base(stringFormat, validTypes, supportCustom)
        {
            
            MessageManager.SubscribeMessage<SwitchLanguageMessage>(OnSwitchLanguage);
        }

        private void OnSwitchLanguage(SwitchLanguageMessage message)
        {
            var path = String.Empty;
            message.HandlingResult = false;

            if (SwitchLanguageMessage.CustomLanguageKey.Equals(message.CultureName, StringComparison.Ordinal))
            {
                if (supportCustom)
                {
                    var openFileDialog = new OpenFileDialog()
                    {
                        DefaultExt = ".xaml",
                        CheckFileExists = true,
                        Multiselect = false,
                        Filter = ResourceHelper.FindText("LanguageFileFilter"),
                    };
                    if (openFileDialog.ShowDialog() == true)
                    {
                        path = openFileDialog.FileName;
                    }
                    else
                    {
                        message.HandlingResult = true;
                        return;
                    }
                }
                else
                {
                    path = resourceStringFormat.InvariantFormat(Thread.CurrentThread.CurrentUICulture.Name);
                }
            }
            else
            {
                path = resourceStringFormat.InvariantFormat(message.CultureName);
            }

            try
            {
                var languageDictionary = path.MakeResDic();
                if (Resources[resourceIndex].VerifyResources(languageDictionary, validResourceTypes))
                {
                    Resources[resourceIndex] = languageDictionary;
                }
                else
                {
                    message.HandlingResult = true;
                }
            }
            catch (Exception ex)
            {
                var msg = ResourceHelper.FindText("ResourceParseError", ex.Message);
                message.HandlingResult = true;
                MessageBox.Show(msg, ResourceHelper.FindText("ApplicationTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        protected override void OnApplicationStartup()
        {
            var cultureName = Thread.CurrentThread.CurrentUICulture.Name;
            Resources.Add(resourceStringFormat.MakeResDic(cultureName));
        }
    }
}
