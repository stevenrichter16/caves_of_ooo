namespace CavesOfOoo.Core
{
    /// <summary>
    /// Binary min-heap for A* open set. Stores cell indices sorted by F-cost.
    /// Pre-allocated array, zero GC.
    /// </summary>
    internal class PathMinHeap
    {
        private readonly int[] _heap;
        private readonly int[] _fCosts;
        private int _count;

        public PathMinHeap(int capacity)
        {
            _heap = new int[capacity];
            _fCosts = new int[capacity];
            _count = 0;
        }

        public int Count => _count;

        public void Clear()
        {
            _count = 0;
        }

        public void Push(int nodeIndex, int fCost)
        {
            int i = _count;
            _heap[i] = nodeIndex;
            _fCosts[i] = fCost;
            _count++;

            // Sift up
            while (i > 0)
            {
                int parent = (i - 1) / 2;
                if (_fCosts[i] < _fCosts[parent])
                {
                    Swap(i, parent);
                    i = parent;
                }
                else
                    break;
            }
        }

        public int Pop()
        {
            int result = _heap[0];
            _count--;
            if (_count > 0)
            {
                _heap[0] = _heap[_count];
                _fCosts[0] = _fCosts[_count];
                SiftDown(0);
            }
            return result;
        }

        private void SiftDown(int i)
        {
            while (true)
            {
                int left = 2 * i + 1;
                int right = 2 * i + 2;
                int smallest = i;

                if (left < _count && _fCosts[left] < _fCosts[smallest])
                    smallest = left;
                if (right < _count && _fCosts[right] < _fCosts[smallest])
                    smallest = right;

                if (smallest == i) break;
                Swap(i, smallest);
                i = smallest;
            }
        }

        private void Swap(int a, int b)
        {
            int tmpNode = _heap[a];
            _heap[a] = _heap[b];
            _heap[b] = tmpNode;

            int tmpCost = _fCosts[a];
            _fCosts[a] = _fCosts[b];
            _fCosts[b] = tmpCost;
        }
    }
}
