namespace KursProject.Models
{
    public class FormulaSelection
    {
        public bool UseMntWines { get; set; }
        public bool UseMntFruits { get; set; }
        public bool UseMntMeatProducts { get; set; }
        public bool UseMntFishProducts { get; set; }
        public bool UseMntSweetProducts { get; set; }
        public bool UseMntGoldProds { get; set; }

        public bool UseNumDealsPurchases { get; set; }
        public bool UseNumWebPurchases { get; set; }
        public bool UseNumCatalogPurchases { get; set; }
        public bool UseNumStorePurchases { get; set; }
    }
}
