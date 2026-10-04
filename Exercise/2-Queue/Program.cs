var queue = new Queue<string>();

Enqueue("A");
Enqueue("B");
Process();
Process();

void Enqueue(string value)
{
    queue.Enqueue(value); // Ini fungsi dari objek queue, bukan yang kita definisikan sendiri
    Console.WriteLine($"Queued {value}");
}

void Process()
{
    if (queue.Count == 0)
    {
        Console.WriteLine("Queue is empty");
    }
    Console.WriteLine($"Processed {queue.Dequeue()}");


}

// var queue = new Queue<string>();


// Enqueue_func("A");
// Enqueue_func("B");
// Process();
// Process();

// void Enqueue_func(string val)
// {
//     queue.Enqueue(val);
//     Console.WriteLine($"Queued {val}");
// }

// void Process()
// {
//     if (queue.Count == 0)
//     {
//         Console.WriteLine("Queue is empty");
//         return;
//     }

//     Console.WriteLine($"Processed {queue.Dequeue()}");
// }
