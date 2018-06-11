using System;

namespace DistributedSystems_1.Priority_queue
{
    public interface IHeap<T> where T : IComparable
    {
        bool Empty();

        void InsertNode(T node);

        T ExtractMin();

        T CheckMin();
    }
}