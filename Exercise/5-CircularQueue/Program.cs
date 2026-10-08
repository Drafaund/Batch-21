const int Capacity = 3;

var buffer = new int[Capacity];
var head = 0;
var count = 0;

Log(1);
Log(2);
Log(3);
Log(4);
Read();

void Log(int val)
{
    if (count == Capacity)
    {
        Console.WriteLine("Buffer Full");
        return;
    }

    var tail = (head + count) % Capacity;
    buffer[tail] = val;
    count++;
    Console.WriteLine($"Logged {val}");
}

void Read()
{
    if (count == 0)
    {
        Console.WriteLine("Buffer Empty");
        return;
    }

    var val = buffer[head];
    head = (head + 1) % Capacity;
    count--;
    Console.WriteLine($"Read {val}");
}
