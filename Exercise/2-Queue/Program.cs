using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.Marshalling;

var queue = new Queue<string>();

Enqueue("A");
Enqueue("B");
EnqueueVip("C");
Process();
Process();
Process();

void Enqueue(string value)
{
    queue.Enqueue(value);
    Console.WriteLine($"Queued {value}");
}

void EnqueueVip(string value)
{
    var items = new Queue<string>();
    items.Enqueue(value);

    foreach (var item in queue)
    {
        items.Enqueue(item);
    }

    queue = items;
    Console.WriteLine($"VIP Queued: {value}");
}

void Process()
{
    if (queue.Count == 0)
    {
        Console.WriteLine("Queue is empty");
    }
    Console.WriteLine($"Processed {queue.Dequeue()}");
}

#region Week 3 - Priority Queue

var priorityItems = new List<(string Value, int Priority)>();

Console.WriteLine();
Console.WriteLine("--- Week 3 ---");
EnqueueWithPriority("A", 1);
EnqueueWithPriority("B", 5);
EnqueueWithPriority("C", 5);
ProcessByPriority();
ProcessByPriority();

void EnqueueWithPriority(string value, int priority)
{
    priorityItems.Add((value, priority));
    Console.WriteLine($"Queued {value} with priority {priority}");
}

void ProcessByPriority()
{
    if (priorityItems.Count == 0)
    {
        Console.WriteLine("Queue is empty");
        return;
    }

    // Strict ">" keeps the oldest item on ties (FIFO tiebreak).
    var highestIndex = 0;
    for (var i = 1; i < priorityItems.Count; i++)
    {
        if (priorityItems[i].Priority > priorityItems[highestIndex].Priority)
        {
            highestIndex = i;
        }
    }

    var item = priorityItems[highestIndex];
    priorityItems.RemoveAt(highestIndex);
    Console.WriteLine($"Processed {item.Value}");
}

#endregion

