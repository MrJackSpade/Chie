﻿namespace Ai.Utils
{
    public class ChangeSyncObject<T>
    {
        private readonly Func<T, Task> _invoke;

        private bool _invokeEnabled;

        private T _lastInvoke;

        private T _lastSet;

        public ChangeSyncObject(T initialState, Func<T, Task> toInvoke)
        {
            this._invoke = toInvoke;
            this._lastSet = initialState;
        }

        public ChangeSyncObject(Func<T, Task> toInvoke)
        {
            this._invoke = toInvoke;
        }

        public async Task DisableChange() => await this.ToggleChange(false);

        public async Task EnableChange() => await this.ToggleChange(true);

        public async Task Flush()
        {
            if (!Equals(this._lastSet, this._lastInvoke))
            {
                await this._invoke.Invoke(this._lastSet);
                this._lastInvoke = this._lastSet;
            }
        }

        public async Task ToggleChange(bool toggle)
        {
            this._invokeEnabled = toggle;

            if (toggle)
            {
                await this.Flush();
            }
        }

        public async Task Update(T value)
        {
            this._lastSet = value;

            if (this._invokeEnabled)
            {
                await this.Flush();
            }
        }
    }
}