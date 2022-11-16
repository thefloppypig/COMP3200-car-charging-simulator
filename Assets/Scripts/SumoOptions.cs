using Newtonsoft.Json;
using System.IO;

public class SumoOptions
{
    public float simSpeed = 1f;
    public bool hideSumoWindow = true;
    public bool useSublaneModel = false;
    public float lateralResolution = 1.2f;

    public bool netConvertRefresh = true;
    public bool randomTripsRefresh = true;

    public float rTripsFringe = 10f;
    public float rTripsPeriod = 1f;

    public bool hideNetconvert = true;
    public bool hideRandomTrips = true;
    public bool waitForUserToClose = false;

    public bool csStopsEnabled = true;
    public float csCarsProbability = 0.5f;
    public float csStopDurationMin = 100f;
    public float csStopDurationMax = 100f;

    public bool csSetValues = false;
    public float csPowerValue = 20000f;
    public float csEfficiency = 0.95f;

    public string dataTime = "None";

    public static void Save(SumoOptions opt, string path)
    {
        string json = JsonConvert.SerializeObject(opt, Newtonsoft.Json.Formatting.Indented);
        File.WriteAllText(path, json);
    }

    public static SumoOptions Load(string path)
    {
        string json = File.ReadAllText(path);
        SumoOptions opt = JsonConvert.DeserializeObject<SumoOptions>(json);
        return opt;
    }
}
