using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ConcurrentPipes;
using LoggerMessages;

namespace NetworkVisualiser
{
    public class Listener<T>
        : PipeListener<T>
        where T : BaseMessage
    {
        private Action<T> action;

        protected override void Unregister(string name)
        {
            base.Unregister(name);
        }

        public Listener(Action<T> action)
        {
            this.action = action;
        }

        protected override void AddMessage(T data)
        {
            action(data);
        }
    }
}
