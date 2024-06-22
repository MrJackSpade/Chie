using System.Collections;

namespace ChieApi.Utils
{
	public class CappedQueue<T> : IEnumerable<T>
	{
		private readonly int _capacity;

		private readonly Queue<T> _queue;

		public CappedQueue(int capacity)
		{
			_capacity = capacity;
			_queue = new Queue<T>();
		}

		public bool Contains(T item)
		{
			return _queue.Contains(item);
		}

		public bool Contains(T item, IEqualityComparer<T> comparer)
		{
			return _queue.Contains(item, comparer);
		}

		public void Dequeue()
		{
			_queue.Dequeue();
		}

		public void Enqueue(T item)
		{
			_queue.Enqueue(item);

			if (_queue.Count > _capacity)
			{
				_queue.Dequeue();
			}
		}

		public IEnumerator<T> GetEnumerator()
		{
			return ((IEnumerable<T>)_queue).GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return ((IEnumerable)_queue).GetEnumerator();
		}
	}
}