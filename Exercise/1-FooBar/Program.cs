Generate(15);

void Generate(int number)
{
    for(int i = 1 ; i <= number; i++)
    {
        if (i % 3 == 0 && i % 5 == 0)
        {
            Console.WriteLine("foobar");
        }

        else if (i % 5 == 0)
        {
            Console.WriteLine("bar");
        }

        else if(i % 3 ==0)
        {
            Console.WriteLine("foo");
        }

        else
        {
            Console.WriteLine(i);
        }
    }
    
}