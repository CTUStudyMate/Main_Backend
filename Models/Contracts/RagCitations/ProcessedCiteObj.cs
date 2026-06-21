namespace MainBackend.Models;
public class ProcessedCiteObj
{
    public Dictionary<string, List<string>> Texts { get; set; }
        = new();

    public Dictionary<string, List<string>> Images { get; set; }
        = new();
}