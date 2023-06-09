namespace Ai.Utils
{
	public class DelayedTrigger
	{
		public Func<Task<bool>> _action;

		private readonly int _delayMs;

		private readonly AutoResetEvent _gate = new(true);

		private readonly int _maxDelayMs;

		private DateTime _firstAttempt = DateTime.MinValue;

		private DateTime _lastAttempt = DateTime.MinValue;

		private Thread? _thread;

		public DelayedTrigger(Func<Task<bool>> action, int delayMs, int maxDelayMs)
		{
			this._delayMs = delayMs;

			this._action = action;
			this._maxDelayMs = maxDelayMs;
		}

		public void ResetWait(DateTime newTarget)
		{
			if (this._thread != null)
			{
				this._lastAttempt = newTarget.AddMilliseconds(0 - this._delayMs);
			}
		}

		/// <summary>
		/// Attempts to lock the thread and then executes the queued action, and
		/// sets the timer to fire the fire event
		/// </summary>
		/// <param name="prep">A preparatory action guaranteed to not invoke during the firing process</param>
		/// <returns>True if the thread was captured and the prep action was executed. False if the event was already firing</returns>
		public bool TryFire(Action prep)
		{
			return this.TryFire(() =>
			{
				prep.Invoke();
				return true;
			});
		}

		/// <summary>
		/// Attempts to lock the thread and then executes the queued action, and
		/// sets the timer to fire the fire event
		/// </summary>
		/// <param name="prep">A preparatory action guaranteed to not invoke during the firing process</param>
		/// <returns>True if the thread was captured and the prep action was executed. False if the event was already firing</returns>
		public bool TryFire(Func<bool> prep)
		{
			if (!this._gate.WaitOne(0))
			{
				return false;
			}

			DateTime now = DateTime.Now;

			if (this._lastAttempt < now)
			{
				this._lastAttempt = DateTime.Now;
			}

			bool threadNeedsInit = this._thread is null;

			if (threadNeedsInit)
			{
				this._firstAttempt = now;
				this._thread = new Thread(async () => await this.TryFireLoop());
			}

			try
			{
				prep.Invoke();

				if (threadNeedsInit)
				{
					this._thread!.Start();
				}
			}
			finally
			{
				this._gate.Set();
			}

			return true;
		}

		private double Since(DateTime dateTime) => (DateTime.Now - dateTime).TotalMilliseconds;

		private async Task TryFireLoop()
		{
			do
			{
				this._gate.WaitOne();

				try
				{
					if (this.Since(this._lastAttempt) > this._delayMs || this.Since(this._firstAttempt) > this._maxDelayMs)
					{
						if (await this._action.Invoke())
						{
							this._thread = null;

							return;
						}
					}
				}
				finally
				{
					this._gate.Set();
				}

				await Task.Delay(this._delayMs);
			} while (true);
		}
	}
}