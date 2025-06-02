using System.ComponentModel.DataAnnotations.Schema;

namespace KursProject.Models
{
    public class marketingdata
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public string Channel { get; set; }
        public decimal AdCost { get; set; }
        public decimal SalesVolume { get; set; }
        
    }
}
