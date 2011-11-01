﻿namespace ClearMine.VM
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Windows;
    using System.Windows.Input;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using System.Windows.Threading;
    using System.Xml.Serialization;

    using ClearMine.Common;
    using ClearMine.Common.ComponentModel;
    using ClearMine.Common.Messaging;
    using ClearMine.Common.Properties;
    using ClearMine.Common.Utilities;
    using ClearMine.Framework.Messages;
    using ClearMine.GameDefinition;
    using ClearMine.VM.Commands;
    using Microsoft.Win32;

    internal sealed class ClearMineViewModel : ViewModelBase
    {
        private IClearMine game;
        private bool pandingInitialize = true;
        private bool isMousePressed = false;
        private double itemSize;

        public ClearMineViewModel()
        {
            HookupToGame(Infrastructure.Container.GetExportedValue<IClearMine>());
            Settings.Default.PropertyChanged += OnSettingsChanged;
        }

        [ReadOnly(true)]
        public int Columns
        {
            get { return (int)game.Size.Width; }
        }

        [ReadOnly(true)]
        public int Rows
        {
            get { return (int)game.Size.Height; }
        }

        public double ItemSize
        {
            get { return itemSize; }
            set { SetProperty(ref itemSize, value, "ItemSize"); }
        }

        [ReadOnly(true)]
        public string Time
        {
            get { return Convert.ToString(game.UsedTime / 1000d, CultureInfo.InvariantCulture); }
        }

        [ReadOnly(true)]
        public string RemainedMines
        {
            get { return Convert.ToString(game.RemainedMines, CultureInfo.InvariantCulture); }
        }

        public bool IsMousePressed
        {
            get { return isMousePressed; }
            set
            {
                if (isMousePressed != value)
                {
                    isMousePressed = value;
                    TriggerPropertyChanged("NewGameIcon");
                }
            }
        }

        [ReadOnly(true)]
        public bool IsPaused
        {
            get { return game.GameState == GameState.Paused; }
        }

        public Brush NewGameIcon
        {
            get
            {
                if (IsMousePressed)
                {
                    return Application.Current.FindResource("TryFaceBrush") as Brush;
                }

                if (game.GameState == GameState.Success)
                {
                    return Application.Current.FindResource("WinFaceBrush") as Brush;
                }
                else if (game.GameState == GameState.Failed)
                {
                    return Application.Current.FindResource("LosingFaceBrush") as Brush;
                }
                else
                {
                    return Application.Current.FindResource("NormalFaceBrush") as Brush;
                }
            }
        }

        public IEnumerable<MineCell> Cells
        {
            get { return game.Cells; }
        }

        internal IClearMine Game
        {
            get { return game; }
        }

        public void StartNewGame()
        {
            if (game.GameState == GameState.Started || game.GameState == GameState.Paused)
            {
                if (Settings.Default.AlwaysNewGame ||
                    ShowDialog("ClearMine.UI.Dialogs.ConfirmNewGameWindow, ClearMine.Dialogs"))
                {
                    if (pandingInitialize)
                    {
                        Initialize();
                        RefreshUI();
                    }

                    UpdateStatistics();
                    game.StartNew();
                }
                else
                {
                    game.ResumeGame();
                }
            }
            else
            {
                if (pandingInitialize)
                {
                    Initialize();
                }

                game.StartNew();
            }
        }

        public void RequestToClose(CancelEventArgs e)
        {
            if (game.GameState == GameState.Started)
            {
                if (Settings.Default.SaveOnExit)
                {
                    SaveCurrentGame(Settings.Default.UnfinishedGameFileName);
                }
                else
                {
                    var result = MessageBox.Show(LocalizationHelper.FindText("AskingSaveGameMessage"), LocalizationHelper.FindText("AskingSaveGameTitle"),
                        MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
                    if (result == MessageBoxResult.Cancel)
                    {
                        e.Cancel = true;
                    }
                    else if (result == MessageBoxResult.Yes)
                    {
                        SaveCurrentGame(Settings.Default.UnfinishedGameFileName);
                    }
                    else
                    {
                        UpdateStatistics();
                    }
                }
            }
        }

        public void MarkAt(MineCell cell)
        {
            if (cell != null && (game.GameState == GameState.Initialized || game.GameState == GameState.Started))
            {
                if (cell.State == CellState.Normal)
                {
                    game.TryMarkAt(cell, CellState.MarkAsMine);
                }
                else if (cell.State == CellState.MarkAsMine)
                {
                    if (Settings.Default.ShowQuestionMark)
                    {
                        game.TryMarkAt(cell, CellState.Question);
                    }
                    else
                    {
                        game.TryMarkAt(cell, CellState.Normal);
                    }
                }
                else if (cell.State == CellState.Question)
                {
                    game.TryMarkAt(cell, CellState.Normal);
                }
                else
                {
                    // Do nothing.
                }

                TriggerPropertyChanged("RemainedMines");
            }
        }

        public void DigAt(MineCell cell)
        {
            if (cell != null && (game.GameState == GameState.Initialized || game.GameState == GameState.Started))
            {
                ThreadPool.QueueUserWorkItem(o => HandleExpandedCells(game.TryDigAt(cell)));
            }
        }

        public void TryExpand(MineCell cell)
        {
            if (cell != null && (game.GameState == GameState.Initialized || game.GameState == GameState.Started))
            {
                ThreadPool.QueueUserWorkItem(o => HandleExpandedCells(game.TryExpandAt(cell)));
            }
        }

        public void RefreshUI()
        {
            TriggerPropertyChanged("Columns");
            TriggerPropertyChanged("Rows");
            TriggerPropertyChanged("RemainedMines");
            TriggerPropertyChanged("Time");
        }

        public override IEnumerable<CommandBinding> GetCommandBindings()
        {
            return GameCommandBindings.GetGameCommandBindings();
        }

        internal void SaveCurrentGame(string path = null)
        {
            if (String.IsNullOrWhiteSpace(path))
            {
                var savePathDialog = new SaveFileDialog();
                savePathDialog.DefaultExt = Settings.Default.SavedGameExt;
                savePathDialog.Filter = LocalizationHelper.FindText("SavedGameFilter", Settings.Default.SavedGameExt);
                if (savePathDialog.ShowDialog() == true)
                {
                    path = savePathDialog.FileName;
                }
                else
                {
                    return;
                }
            }

            // Pause game to make sure the timestamp correct.
            game.PauseGame();
            var gameSaver = new XmlSerializer(game.GetType());
            using (var file = File.Open(path, FileMode.Create, FileAccess.Write))
            {
                gameSaver.Serialize(file, game);
            }
        }

        internal void LoadSavedGame(string path)
        {
            if (!File.Exists(path))
            {
                throw new FileNotFoundException("The path to saved game cannot be found.", path);
            }

            IClearMine newgame = null;
            var gameLoader = new XmlSerializer(game.GetType());
            using (var file = File.Open(path, FileMode.Open, FileAccess.Read))
            {
                newgame = (IClearMine)gameLoader.Deserialize(file);
            }

            if (newgame.CheckHash())
            {
                HookupToGame(newgame);
                RefreshUI();
                game.ResumeGame();
            }
            else
            {
                MessageBox.Show(LocalizationHelper.FindText("CorruptedSavedGameMessage"), LocalizationHelper.FindText("CorruptedSavedGameTitle"));
            }
        }

        private void Initialize()
        {
            try
            {
                InitialPlayground();
                pandingInitialize = false;
            }
            catch (InvalidOperationException)
            {
                // While, there probably something wrong with your configurations.
                Settings.Default.Rows = 9;
                Settings.Default.Columns = 9;
                Settings.Default.Mines = 10;
                Settings.Default.Difficulty = Difficulty.Beginner;
                Settings.Default.Save();

                // Try again.
                InitialPlayground();
            }
        }

        private void InitialPlayground()
        {
            game.Initialize(new Size(Settings.Default.Columns, Settings.Default.Rows), (int)Settings.Default.Mines);
        }

        private void OnGameTimeChanged(object sender, EventArgs e)
        {
            TriggerPropertyChanged("Time");
        }

        private void OnSettingsChanged(object sender, PropertyChangedEventArgs e)
        {
            if (new[] { "Rows", "Columns", "Mines" }.Contains(e.PropertyName))
            {
                if (game.GameState == GameState.Initialized)
                {
                    Initialize();
                    TriggerPropertyChanged(e.PropertyName);
                }
                else
                {
                    pandingInitialize = true;
                }
            }
            else
            {
                // Ignore it.
            }
        }

        private void OnCellStateChanged(object sender, CellStateChangedEventArgs e)
        {
            if (Settings.Default.PlayAnimation)
            {
                if (e.Cell.State == CellState.Shown)
                {
                    Thread.Sleep(1);
                }
            }
        }

        private void OnGameStateChanged(object sender, EventArgs e)
        {
            TriggerPropertyChanged("IsPaused");
            TriggerPropertyChanged("RemainedMines");
            TriggerPropertyChanged("NewGameIcon");
            if (game.GameState == GameState.Failed)
            {
                IsMousePressed = false;
                Player.Play(Settings.Default.SoundLose);
                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    UpdateStatistics();
                    ShowLostWindow();
                }));
            }
            else if (game.GameState == GameState.Success)
            {
                IsMousePressed = false;
                Player.Play(Settings.Default.SoundWin);
                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    UpdateStatistics();
                    ShowWonWindow();
                }), DispatcherPriority.Input);
            }
            else if (game.GameState == GameState.Initialized)
            {
                Player.Play(Settings.Default.SoundStart);
            }
        }

        private static string TakeScreenShoot()
        {
            var target = VisualTreeHelper.GetChild(Application.Current.MainWindow, 0) as FrameworkElement;
            var targetBitmap = new RenderTargetBitmap((int)target.ActualWidth, (int)target.ActualHeight, 96d, 96d, PixelFormats.Default);
            targetBitmap.Render(target);

            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(targetBitmap));

            string fileName = DateTime.Now.ToString(Settings.Default.ScreenShotFileTimeFormat, CultureInfo.InvariantCulture) + ".png";
            string folder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + Settings.Default.ScreenShotFolder;

            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            fileName = Path.Combine(folder, fileName);

            // save file to disk
            using (var fs = File.Open(fileName, FileMode.OpenOrCreate))
            {
                encoder.Save(fs);
            }

            Trace.TraceInformation(LocalizationHelper.FindText("ScreenShotSavedTo"), fileName);

            return fileName;
        }

        private void ShowLostWindow()
        {
            if (ShowDialog("ClearMine.UI.Dialogs.GameLostWindow, ClearMine.Dialogs"))
            {
                ThreadPool.QueueUserWorkItem(a => game.StartNew());
            }
            else
            {
                ThreadPool.QueueUserWorkItem(a => game.Restart());
            }
        }

        private void ShowWonWindow()
        {
            if (ShowDialog("ClearMine.UI.Dialogs.GameWonWindow, ClearMine.Dialogs", new GameWonViewModel(game.UsedTime, DateTime.Now)))
            {
                Application.Current.Shutdown();
            }
            else
            {
                ThreadPool.QueueUserWorkItem(a => game.StartNew());
            }
        }

        private bool ShowDialog(string type, object data = null)
        {
            var message = new ShowDialogMessage()
            {
                DialogType = Type.GetType(type),
                Data = data,
            };
            MessageManager.GetMessageAggregator<ShowDialogMessage>().SendMessage(message);

            return (bool)message.HandlingResult;
        }

        private void UpdateStatistics()
        {
            var history = Settings.Default.HeroList.GetByLevel(Settings.Default.Difficulty);
            if (history != null)
            {
                if (game.GameState == GameState.Success)
                {
                    history.IncreaseWon(game.UsedTime / 1000.0, DateTime.Now, TakeScreenShoot());
                }
                else if (game.GameState == GameState.Failed)
                {
                    history.IncreaseLost();
                }
                else
                {
                    history.IncreaseUndone();
                }
            }

            Settings.Default.Save();
        }

        private static void HandleExpandedCells(IEnumerable<MineCell> cells)
        {
            int emptyCellExpanded = cells.Count(c => c.MinesNearby == 0);
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

        private void HookupToGame(IClearMine newgame)
        {
            if (game == null)
            {
                game = newgame;

                game.StateChanged += OnGameStateChanged;
                game.TimeChanged += OnGameTimeChanged;
                game.CellStateChanged += OnCellStateChanged;
            }
            else
            {
                game.Update(newgame);
            }
        }
    }
}
