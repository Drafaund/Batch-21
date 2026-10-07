public class PriceChangedEventArgs: EventArgs
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
    string symbol;
    decimal price;

    public Stock(string symbol)
    {
        this.symbol = symbol;
    }

    public event EventHandler<PriceChangedEventArgs> PriceChanged;

    protected virtual void OnPriceChanged(PriceChangedEventArgs e)
    {
        PriceChanged?.Invoke(this, e);
    }

    public decimal Price
    {
        get {return price;}
        set
        {
            if (price == value) return;

            decimal priceLama = price;       
            price = value;
            OnPriceChanged(new PriceChangedEventArgs(priceLama, price));
        }
    }

}

class Program
{
    static void Main()
    {
        Stock stock = new Stock("MSFT");
        stock.PriceChanged += stock_PriceChanged;
        stock.Price = 100;
        stock.Price = 200;

        
    }

    static void stock_PriceChanged(object sender, PriceChangedEventArgs e)
    {
        Console.WriteLine($"Price changed from {e.OldPrice} to {e.NewPrice}");
    }
}