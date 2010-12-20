﻿namespace ClearMine.Common.Logic
{
    using System;
    using System.ComponentModel;
    using System.Globalization;
    using System.Threading;
    using ClearMine.Common.ComponentModel;

    public class MineCell : BindableObject, ICachable<MineCell>
    {
        private CachingState cachingState;
        private CellState state;
        private PressState pressState;
        private int minesNearBy;
        private bool showResult;
        private bool hasMine;
        private bool isTerminator;

        internal MineCell(int column, int row)
        {
            UpdatePosition(column, row);
            State = CellState.Normal;
            MinesNearby = -1;
        }

        [field: NonSerialized]
        public event EventHandler CacheStateChanged;
        [field: NonSerialized]
        public event EventHandler<CellStateChangedEventArgs> StateChanged;

        [Bindable(true)]
        public bool HasMine
        {
            get { return hasMine; }
            set { SetProperty(ref hasMine, value, "HasMine"); }
        }

        public int Row { get; private set; }

        public int Column { get; private set; }

        [Bindable(true)]
        public CachingState CachingState
        {
            get { return cachingState; }
            set
            {
                if (cachingState != value)
                {
                    SetProperty(ref cachingState, value, "CachingState");
                    var temp = CacheStateChanged;
                    if (temp != null)
                    {
                        temp(this, EventArgs.Empty);
                    }
                }
            }
        }

        [ReadOnly(true)]
        [Bindable(true)]
        public bool IsPressed
        {
            get { return pressState != PressState.Released; }
        }

        public PressState PressState
        {
            get { return pressState; }
            set
            {
                SetProperty(ref pressState, value, "PressState");
                TriggerPropertyChanged("IsPressed");
            }
        }

        [Bindable(true)]
        public bool ShowResult
        {
            get { return showResult; }
            set { SetProperty(ref showResult, value, "ShowResult"); }
        }

        [Bindable(true)]
        public int MinesNearby
        {
            get { return minesNearBy; }
            set
            {
                // Must provide propertyName here, 
                // Otherwise it cause series performance issue. 
                // Because during the initalization of the Game.
                // This property will be set many many times.
                SetProperty(ref minesNearBy, value, "MinesNearby");
            }
        }

        [Bindable(true)]
        public CellState State
        {
            get { return state; }
            set
            {
                if (state != value)
                {
                    var oldState = state;
                    SetProperty(ref state, value, "State");
                    var temp = StateChanged;
                    if (temp != null)
                    {
                        temp(this, new CellStateChangedEventArgs(this, oldState));
                    }
                }
            }
        }

        [Bindable(true)]
        public bool IsTerminator
        {
            get { return isTerminator; }
            set { SetProperty(ref isTerminator, value, "IsTerminator"); }
        }

        public override string ToString()
        {
            return String.Format(CultureInfo.CurrentCulture, "({0}, {1}) {3} : {2}", Column, Row, MinesNearby, HasMine);
        }

        internal void UpdatePosition(int column, int row)
        {
            Row = row;
            Column = column;
        }

        public void Update(MineCell newValue)
        {
            Row = newValue.Row;
            Column = newValue.Column;
            State = newValue.State;
            HasMine = newValue.HasMine;
            if (newValue.MinesNearby >= 0)
            {
                MinesNearby = newValue.MinesNearby;
            }
        }

        internal bool Near(MineCell other)
        {
            return Math.Abs(Column - other.Column) <= 1 && Math.Abs(Row - other.Row) <= 1;
        }
    }
}
