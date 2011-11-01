﻿namespace ClearMine.Common.Messaging
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class MessageAggregator<T> where T : MessageBase
    {
        private ICollection<Action<T>> processors = new List<Action<T>>();

        public void Subscribe(Action<T> processor)
        {
            if (!processors.Any(p => p == processor))
            {
                processors.Add(processor);
            }
        }

        public void Unsubscribe(Action<T> processor)
        {
            EnumerateAliveProcessors(r =>
            {
                if (r == processor)
                {
                    processors.Remove(r);
                }
            });
        }

        public T SendMessage(T message)
        {
            if (message != null)
            {
                EnumerateAliveProcessors(r => (r as Action<T>)(message));
            }

            return message;
        }

        private void EnumerateAliveProcessors(Action<Action<T>> action)
        {
            foreach (var reference in processors)
            {
                action(reference);
            }
        }
    }
}