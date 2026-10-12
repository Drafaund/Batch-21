Generate(105);

void Generate(int number)
{
    for(int i = 1 ; i <= number; i++)
    {
        if (i % 3 == 0 && i % 5 == 0 && i % 7 == 0)
        {
            Console.WriteLine("foobarjazz");
        }
        
        else if (i % 3 == 0 && i % 5 == 0)
        {
            Console.WriteLine("foobar");
        }
        else if (i % 5 == 0 && i % 7 == 0)
        {
            Console.WriteLine("barjazz");
        }

        else if(i % 3 == 0 && i % 7 == 0)
        {
            Console.WriteLine("foojazz");
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