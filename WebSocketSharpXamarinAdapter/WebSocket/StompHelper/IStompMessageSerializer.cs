namespace WebSocketSharpXamarinAdapter.WebSocket.StompHelper
{
    public interface IStompMessageSerializer
    {
        string Serialize(StompMessage message);
        StompMessage Deserialize(string message);
    }
}