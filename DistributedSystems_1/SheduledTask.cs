using System;

namespace DistributedSystems_1
{
    internal struct SheduledTask : IComparable
    {
        public Action Action;
        public int SheduledTime;

        public int CompareTo(object other) => CompareTo(other as int?);

        private int CompareTo(int? other)
        {
            if (other != null)
            {
                SheduledTime.CompareTo(other);
            }

            return -1;
        }
    }
}