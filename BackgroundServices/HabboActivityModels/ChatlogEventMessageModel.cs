namespace KeplerCMS.BackgroundServices.HabboActivityModels;

public class ChatlogEventMessageModel
{
    public int PlayerId { get; set; }
    public string Message { get; set; }
    public string ChatMessageType { get; set; }
    public int RoomId { get; set; }
    public long SentTime { get; set; }
    public string Username { get; set; }
}