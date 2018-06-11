using System;

namespace DistributedSystems_1.Priority_queue
{
    internal class Heap<T> : IHeap<T> where T : IComparable
    {
        protected T[] Nodes;
        protected int HeapCurrentSize;
        private readonly int _d;

        public Heap(int nodesCount, int d)
        {
            Nodes = new T[nodesCount];
            HeapCurrentSize = 0;
            _d = d;
        }


        public bool Empty() => HeapCurrentSize == 0;

        public virtual void InsertNode(T node)
        {
            if (HeapCurrentSize >= Nodes.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(node), "Heap is full");
            }
            Nodes[HeapCurrentSize] = node;
            HeapCurrentSize++;
            SiftUp(HeapCurrentSize - 1);
        }

        public virtual T ExtractMin()
        {
            if (HeapCurrentSize == 0)
            {
                throw new InvalidOperationException("heap is empty");
            }
            var minRealxedNode = Nodes[0];
            Nodes[0] = Nodes[HeapCurrentSize - 1];
            HeapCurrentSize--;
            if (HeapCurrentSize > 0)
                SiftDown(0);

            return minRealxedNode;
        }

        public virtual T CheckMin()
        {
            if (HeapCurrentSize == 0)
            {
                throw new InvalidOperationException("heap is empty");
            }

            return Nodes[0];
        }

        protected virtual void SiftDown(int pos)
        {

            var insertedNode = Nodes[pos];
            var nextChild = MinChild(pos);

            while (nextChild != 0 && Nodes[nextChild].CompareTo(insertedNode) < 0)
            {
                Nodes[pos] = Nodes[nextChild];
                pos = nextChild;
                nextChild = MinChild(pos);
            }

            Nodes[pos] = insertedNode;

        }
        

        protected virtual void SiftUp(int pos)
        {
            var changedNode = Nodes[pos];
            var parentNodeIndex = Parent(pos);

            while (pos != 0 && Nodes[parentNodeIndex].CompareTo(changedNode) > 0)
            {
                Nodes[pos] = Nodes[parentNodeIndex];
                pos = parentNodeIndex;
                parentNodeIndex = Parent(pos);
            }

            Nodes[pos] = changedNode;
        }

        protected int MinChild(int pos)
        {
            var fChild = FirstChild(pos);
            if (fChild == 0)
            {
                return 0;
            }
            var lChild = LastChild(pos);
            var minValueNode = Nodes[fChild];
            var minNodeIndex = fChild;

            for (var chi = fChild + 1; chi <= lChild; chi++)
            {
                if (Nodes[chi].CompareTo(minValueNode) >= 0)
                {
                    continue;
                }
                minValueNode = Nodes[chi];
                minNodeIndex = chi;
            }

            return minNodeIndex;
        }

        protected int FirstChild(int pos)
        {
            var index = pos * _d + 1;
            return index >= HeapCurrentSize ? 0 : index;
        }

        protected int LastChild(int pos)
        {
            var fChi = FirstChild(pos);
            if (fChi == 0)
            {
                return 0;
            }
            var lChi = fChi + _d - 1;
            return lChi < HeapCurrentSize ? lChi : HeapCurrentSize - 1;
        }

        protected int Parent(int pos)
        {
            var index = pos / _d;
            if (pos % _d == 0 && index != 0)
            {
                return index - 1;
            }

            return index;
        }
    }
}