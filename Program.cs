using System.Globalization;
public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        if (args.Length != 3)
        {   
            Console.WriteLine("Quantidade incorreta de argumentos.");
            return 1;
        }

        string ticker = args[0].Trim().ToUpperInvariant();

        // Safe Parser para decimais, aceita vírgulas e pontos como separadores
        if (!decimal.TryParse(args[1], NumberStyles.Number, CultureInfo.InvariantCulture, out decimal sellPrice))
        {
            Console.WriteLine("Valor informado inválido.");            
            return 1;
        }
        
        // Safe Parser para decimais, aceita vírgulas e pontos como separadores
        if (!decimal.TryParse(args[2], NumberStyles.Number, CultureInfo.InvariantCulture, out decimal buyPrice))
        {
            Console.WriteLine("Valor informado inválido.");            
            return 1;
        }

        if(buyPrice >= sellPrice)
        {
            Console.WriteLine("O preço de referência para compra deve ser menor que o preço de referência para venda.");
            return 1;
        }

        Console.WriteLine($"ticker = {ticker} - sellPrice = {sellPrice} - buyPrice = {buyPrice}");

        return 0;
    }
}