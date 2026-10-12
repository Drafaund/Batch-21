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

