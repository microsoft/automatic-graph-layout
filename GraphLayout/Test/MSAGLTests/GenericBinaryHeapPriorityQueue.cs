using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Msagl.UnitTests {
    [TestClass]
    public class GenericBinaryHeapPriorityQueue {
        [TestMethod]
        public void Enqueue() {
            var q = new Core.DataStructures.GenericBinaryHeapPriorityQueue<int>();
            q.Enqueue(2, 2);
            q.Enqueue(1, 1);
            q.Enqueue(9, 9);
            q.Enqueue(8, 8);
            q.Enqueue(5, 5);
            q.Enqueue(3, 3);
            q.Enqueue(4, 4);
            q.Enqueue(7, 7);
            q.Enqueue(6, 6);
            q.Enqueue(0, 0);

            var a = new List<int>();
            while (q.IsEmpty() == false)
                a.Add(q.Dequeue());
            Console.WriteLine();
        }

    }
}

