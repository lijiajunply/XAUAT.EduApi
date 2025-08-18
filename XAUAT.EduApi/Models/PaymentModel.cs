using System.Text.Json;

namespace XAUAT.EduApi.Models;

[Serializable]
public class PaymentData
{
    public List<PaymentModel> Records { get; set; } = [];
    public double Total { get; set; }
}

[Serializable]
public class PaymentModel(string turnoverType, string datetimeStr, string resume, double tranamt)
{
    public string TurnoverType { get; set; } = turnoverType;
    public string DatetimeStr { get; set; } = datetimeStr;
    public string Resume { get; set; } = resume;
    public double Tranamt { get; set; } = tranamt;

    public override string ToString()
    {
        return $"{DatetimeStr} | {TurnoverType} | {Tranamt / 100.0:F2} 元 | {Resume.Trim()}";
    }

    public static PaymentModel FromJson(Dictionary<string, JsonElement> json)
    {
        return new PaymentModel(
            turnoverType: json["turnoverType"].GetString() ?? "",
            datetimeStr: json["jndatetimeStr"].GetString() ?? "",
            resume: json["resume"].GetString() ?? "",
            tranamt: json["tranamt"].GetInt32() / 100.0
        );
    }
}