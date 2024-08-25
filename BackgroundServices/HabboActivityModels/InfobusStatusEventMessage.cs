namespace KeplerCMS.BackgroundServices.HabboActivityModels;

public class InfobusStatusEventMessage
{
    public int[] Players { get; set; }
    public int Time { get; set; }
    public string Question { get; set; }
    public string[] Options { get; set; }
    public int[] Votes { get; set; }
    public bool Door { get; set; }
}