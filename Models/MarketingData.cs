using System.ComponentModel.DataAnnotations.Schema;

namespace KursProject.Models
{
    [Table("MarketingData")]
    public class MarketingData
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public string Channel { get; set; }
        public decimal AdCost { get; set; }
        public decimal SalesVolume { get; set; }
        
    }
}
