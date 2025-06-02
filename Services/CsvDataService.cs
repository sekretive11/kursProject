namespace KursProject.Services;
using CsvHelper;
using KursProject.Models;
using System.Globalization;

public class CsvDataService
{
    public List<marketingdata> Loadmarketingdata(string filePath, FormulaSelection selection)
    {
        using var reader = new StreamReader(filePath);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

        var records = csv.GetRecords<dynamic>().ToList();

        return records.Select(r => new marketingdata
        {
            Channel = int.Parse(r.NumWebPurchases) > int.Parse(r.NumStorePurchases) ? "Online" : "Offline",
            AdCost = (selection.UseMntWines ? decimal.Parse(r.MntWines) : 0) +
                 (selection.UseMntFruits ? decimal.Parse(r.MntFruits) : 0) +
                 (selection.UseMntMeatProducts ? decimal.Parse(r.MntMeatProducts) : 0) +
                 (selection.UseMntFishProducts ? decimal.Parse(r.MntFishProducts) : 0) +
                 (selection.UseMntSweetProducts ? decimal.Parse(r.MntSweetProducts) : 0) +
                 (selection.UseMntGoldProds ? decimal.Parse(r.MntGoldProds) : 0),
            SalesVolume = (selection.UseNumDealsPurchases ? decimal.Parse(r.NumDealsPurchases) : 0) +
                     (selection.UseNumWebPurchases ? decimal.Parse(r.NumWebPurchases) : 0) +
                     (selection.UseNumCatalogPurchases ? decimal.Parse(r.NumCatalogPurchases) : 0) +
                     (selection.UseNumStorePurchases ? decimal.Parse(r.NumStorePurchases) : 0)
        }).ToList();
    }
}