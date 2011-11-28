﻿namespace ClearMine.Plugin.AudioPlayer
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;

    using ClearMine.Common.Messaging;
    using ClearMine.Common.Modularity;
    using ClearMine.Common.Properties;
    using ClearMine.Common.Utilities;
    using ClearMine.GameDefinition;
    using ClearMine.GameDefinition.Messages;

    /// <summary>
    /// 
    /// </summary>
    public class AudioPlayerPlugin : AbstractPlugin
    {
        private LanguageSwitcher switcher;

        public override string Name
        {
            get { return ResourceHelper.FindText("AP_Name"); }
        }

        public override string Description
        {
            get { return ResourceHelper.FindText("AP_Description"); }
        }

        public override void Initialize()
        {
            MessageManager.SubscribeMessage<GameStateMessage>(OnGameStateChanged);
            MessageManager.SubscribeMessage<UserOperationMessage>(OnUserOperation);
            switcher = new LanguageSwitcher("/ClearMine.Plugin.AudioPlayer;component/Localization/{0}.xaml", new[] { typeof(string) }, false);
        }

        protected override void InitializeOptions()
        {
            options.Add(new PluginOption()
            {
                ID = "Volumn",
                NameKey = "AP_VolumnName",
                DescriptionKey = "AP_VolumnDescription",
                ValueType = typeof(double).FullName,
                Value = 0.5,
                ValueValidator = value =>
                {
                    var v = (double)value;
                    return v >= 0.0 && v <= 1.0;
                },
            });
            options.Add(new PluginOption()
            {
                ID = "Won",
                NameKey = "AP_WonMusic",
                DescriptionKey = "AP_WonMusicDescription",
                ValueType = typeof(string).FullName,
                Value = null,
                ValueValidator = value => File.Exists(Convert.ToString(value)),
            });
            options.Add(new PluginOption()
            {
                ID = "Lost",
                NameKey = "AP_LostMusic",
                DescriptionKey = "AP_LostMusicDescription",
                ValueType = typeof(string).FullName,
                Value = null,
                ValueValidator = value => File.Exists(Convert.ToString(value)),
            });
            options.Add(new PluginOption()
            {
                ID = "New",
                NameKey = "AP_NewGameMusic",
                DescriptionKey = "AP_NewGameMusicDescription",
                ValueType = typeof(string).FullName,
                Value = null,
                ValueValidator = value => File.Exists(Convert.ToString(value)),
            });

            Trace.WriteLine(String.Format("Ininitialize Options of {0}", GetType().FullName));
        }

        private void OnGameStateChanged(GameStateMessage message)
        {
            var game = message.Source as IClearMine;
            if (IsEnabled && game != null)
            {
                if (game.GameState == GameState.Failed)
                {
                    Player.Play(this["Lost"].Value as string ?? Settings.Default.SoundLose);
                }
                else if (game.GameState == GameState.Success)
                {
                    Player.Play(this["Won"].Value as string ?? Settings.Default.SoundWin);
                }
                else if (game.GameState == GameState.Initialized)
                {
                    Player.Play(this["New"].Value as string ?? Settings.Default.SoundStart);
                }
                else
                {
                    // Play nothing.
                }
            }
        }

        private void OnUserOperation(UserOperationMessage message)
        {
            if (IsEnabled && (message.UserOperation == GameOperation.Expand ||
                message.UserOperation == GameOperation.Dig))
            {
                int emptyCellExpanded = message.EffectedCells.Count(c => c.MinesNearby == 0);
                if (emptyCellExpanded == 0)
                {
                    // Do nothing.
                }
                else if (emptyCellExpanded > 1)
                {
                    Player.Play(Settings.Default.SoundTileMultiple);
                }
                else
                {
                    Player.Play(Settings.Default.SoundTileSingle);
                }
            }
        }
    }
}
