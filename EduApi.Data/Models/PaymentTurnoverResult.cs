namespace EduApi.Data.Models;

public class PaymentTurnoverResult
{
    public List<PaymentModel> Records { get; set; } = [];
    public double Balance { get; set; }
}
