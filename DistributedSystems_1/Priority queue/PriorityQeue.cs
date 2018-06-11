using System;

namespace DistributedSystems_1.Priority_queue
{
    public class PriorityQeue<T> where T : IComparable
    {
        private readonly IHeap<T> _heap;

        public PriorityQeue(int queueMaxSize)
        {
            _heap = new Heap<T>(queueMaxSize, 2);
        }

        public PriorityQeue() : this(50) { }

        public void Enqueue(T item)
        {
            _heap.InsertNode(item);
        }

        public T Dequeue() => _heap.ExtractMin();

        public T Peek() => _heap.CheckMin();

        public bool IsEmpty() => _heap.Empty();
    }
}