class Program
{
    static void Main()
    {
        RunStandardPattern();
        Console.WriteLine();
        RunExplicitAccessor();
    }

    #region Event Handler Standard Pattern

    static void RunStandardPattern()
    {
        Console.WriteLine("== Event Handler Standard Pattern ==");

        var stock = new Stock("MSFT");
        stock.PriceChanged += Stock_PriceChanged;

        stock.Price = 100;
        stock.Price = 200;
    }

    static void Stock_PriceChanged(object? sender, PriceChangedEventArgs e)
    {
        Console.WriteLine($"Price changed from {e.OldPrice} to {e.NewPrice}");
    }

    #endregion

    #region Event Explicit Accessor

    static void RunExplicitAccessor()
    {
        Console.WriteLine("== Event Explicit Accessor ==");

        var stock = new StockWithAccessor();

        stock.PriceChanged += OnPriceChanged;
        stock.ChangePrice();

        stock.PriceChanged -= OnPriceChanged;
        stock.ChangePrice();
    }

    static void OnPriceChanged(object? sender, EventArgs e)
    {
        Console.WriteLine("Handler dijalankan");
    }

    #endregion
}

#region Event Handler Standard Pattern Types

public class PriceChangedEventArgs : EventArgs
{
    public readonly decimal OldPrice;
    public readonly decimal NewPrice;

    public PriceChangedEventArgs(decimal oldPrice, decimal newPrice)
    {
        OldPrice = oldPrice;
        NewPrice = newPrice;
    }
}

class Stock
{
    private readonly string symbol;
    private decimal price;

    public Stock(string symbol)
    {
        this.symbol = symbol;
    }

    public event EventHandler<PriceChangedEventArgs>? PriceChanged;

    protected virtual void OnPriceChanged(PriceChangedEventArgs e)
    {
        PriceChanged?.Invoke(this, e);
    }

    public decimal Price
    {
        get { return price; }
        set
        {
            if (price == value) return;

            decimal oldPrice = price;
            price = value;
            OnPriceChanged(new PriceChangedEventArgs(oldPrice, price));
        }
    }
}

#endregion

#region Event Explicit Accessor Types

class StockWithAccessor
{
    private EventHandler? priceChanged;

    public event EventHandler PriceChanged
    {
        add
        {
            Console.WriteLine("Subscriber ditambahkan");
            priceChanged += value;
        }

        remove
        {
            Console.WriteLine("Subscriber dihapus");
            priceChanged -= value;
        }
    }

    public void ChangePrice()
    {
        Console.WriteLine("Harga berubah");
        priceChanged?.Invoke(this, EventArgs.Empty);
    }
}

#endregion
