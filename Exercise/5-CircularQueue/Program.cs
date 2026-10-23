#region Week 1

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
#endregion

#region Week 2

const int CapacityW2 = 3;

var bufferW2 = new int[CapacityW2];
var headW2 = 0;
var countW2 = 0;

LogW2(1);
LogW2(2);
LogW2(3);
LogW2(4);
ReadW2();

void LogW2(int val)
{
    if (countW2 == CapacityW2)
    {
        bufferW2[headW2] = val;
        headW2 = (headW2 + 1) % CapacityW2;
        Console.WriteLine($"Overwritten oldest with {val}");
        return;
    }

    var tail = (headW2 + countW2) % CapacityW2;
    bufferW2[tail] = val;
    countW2++;
    Console.WriteLine($"Logged {val}");
}

void ReadW2()
{
    if (countW2 == 0)
    {
        Console.WriteLine("Buffer Empty");
        return;
    }

    var val = bufferW2[headW2];
    headW2 = (headW2 + 1) % CapacityW2;
    countW2--;
    Console.WriteLine($"Read {val}");
}
#endregion

#region Week 3

const int CapacityW3 = 3;
const int WarningThresholdPercent = 66;
const int FullPercent = 100;

var bufferW3 = new int[CapacityW3];
var headW3 = 0;
var countW3 = 0;

LogW3(1);
LogW3(2);
LogW3(3);
LogW3(4);
ReadW3();

int UsagePercentW3()
{
    return countW3 * FullPercent / CapacityW3;
}

void CheckAlertW3()
{
    var usage = UsagePercentW3();

    if (usage >= FullPercent)
    {
        Console.WriteLine("Critical: Buffer Full");
    }
    else if (usage >= WarningThresholdPercent)
    {
        Console.WriteLine($"Warning: Buffer at {WarningThresholdPercent}%");
    }
}

void LogW3(int val)
{
    CheckAlertW3();

    if (countW3 == CapacityW3)
    {
        bufferW3[headW3] = val;
        headW3 = (headW3 + 1) % CapacityW3;
        Console.WriteLine($"Overwritten oldest with {val}");
        return;
    }

    var tail = (headW3 + countW3) % CapacityW3;
    bufferW3[tail] = val;
    countW3++;
    Console.WriteLine($"Logged {val}");
}

void ReadW3()
{
    if (countW3 == 0)
    {
        Console.WriteLine("Buffer Empty");
        return;
    }

    var val = bufferW3[headW3];
    headW3 = (headW3 + 1) % CapacityW3;
    countW3--;
    Console.WriteLine($"Read {val}");
}
#endregion
