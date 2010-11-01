﻿namespace ClearMine.UI
{
    using System;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Linq;
    using System.Xml;
    using System.Xml.Serialization;

    using ClearMine.Common.ComponentModel;

    [Serializable]
    [XmlRoot("record")]
    public class HistoryRecord 
    {
        [XmlAttribute("score")]
        public int Score { get; set; }
        [XmlAttribute("when")]
        public DateTime Date { get; set; }
    }

    [Serializable]
    [XmlRoot("heroHistory")]
    public class HeroHistory : BindableObject
    {
        [XmlArray("records")]
        [XmlArrayItem("record")]
        public ObservableCollection<HistoryRecord> Items { get; set; }

        [XmlAttribute("level")]
        public Difficulty Level { get; set; }

        [XmlAttribute("played")]
        public int GamePlayed { get; set; }

        [XmlAttribute("won")]
        public int GameWon { get; set; }

        [XmlAttribute("longestWinning")]
        public int LongestWinning { get; set; }

        [XmlAttribute("longestLosing")]
        public int LongestLosing { get; set; }

        [XmlAttribute("current")]
        public int CurrentStatus { get; set; }

        [XmlAttribute("average")]
        public double AverageScore { get; set; }

        [XmlIgnore]
        public int GameLost
        {
            get
            {
                Debug.Assert(GamePlayed >= GameWon);

                return GamePlayed - GameWon;
            }
        }

        [XmlIgnore]
        public double GameWonPercentage
        {
            get
            {
                if (GamePlayed == 0)
                {
                    return 0.0;
                }

                return GameWon / (double)GamePlayed;
            }
        }

        [XmlIgnore]
        public double GameLostPercentage
        {
            get
            {
                if (GamePlayed == 0)
                {
                    return 0.0;
                }

                return GameLost / (double)GamePlayed;
            }
        }

        public void IncreaseWon(int score, DateTime time)
        {
            AverageScore = (GameWon * AverageScore + score) / ++GameWon;
            ++GamePlayed;
            if (CurrentStatus >= 0)
            {
                ++CurrentStatus;
            }
            else
            {
                CurrentStatus = 1;
            }
            if (CurrentStatus > LongestWinning)
            {
                LongestWinning = CurrentStatus;
            }

            Items.Add(new HistoryRecord() { Score = score, Date = time });
        }

        public void IncreaseLost()
        {
            ++GamePlayed;
            if (CurrentStatus <= 0)
            {
                --CurrentStatus;
            }
            else
            {
                CurrentStatus = -1;
            }
            if (-CurrentStatus > LongestLosing)
            {
                LongestLosing = -CurrentStatus;
            }
        }

        public void Reset()
        {
            Items.Clear();
            GamePlayed = 0;
            GameWon = 0;
            LongestLosing = 0;
            LongestWinning = 0;
            CurrentStatus = 0;
            AverageScore = 0;

            // Update UI after reset.
            OnPropertyChanged("Items");
            OnPropertyChanged("GamePlayed");
            OnPropertyChanged("GameWon");
            OnPropertyChanged("GameLost");
            OnPropertyChanged("LogestLosing");
            OnPropertyChanged("LongestWinning");
            OnPropertyChanged("CurrentStatus");
            OnPropertyChanged("EverageScore");
            OnPropertyChanged("GameWonPercentage");
            OnPropertyChanged("GameLostPercentage");
        }
    }

    [XmlRoot("heroList")]
    public class HeroHistoryList
    {
        [XmlElement("heroOnLevel")]
        public ObservableCollection<HeroHistory> Heroes { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="level"></param>
        /// <returns></returns>
        public HeroHistory GetByLevel(Difficulty level)
        {
            return Heroes.FirstOrDefault(history => history.Level == level);
        }
    }
}
