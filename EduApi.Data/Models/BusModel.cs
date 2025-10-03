namespace EduApi.Data.Models;

[Serializable]
public class BusModel
{
    public List<BusItem> Records { get; set; } = [];
    public int Total { get; set; }
}

[Serializable]
public class BusItem
{
    public string LineName { get; set; } = "";
    public string Description { get; set; } = "";
    public string DepartureStation { get; set; } = "";
    public string ArrivalStation { get; set; } = "";
    public string RunTime { get; set; } = "";
    public string ArrivalStationTime { get; set; } = "";
}