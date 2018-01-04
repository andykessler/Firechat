public class Message
{

    public string uid;
    public string text;
    public string authorId;
    public long timestamp;

    public Message()
    {

    }

    public Message(string uid, string text, string authorId, long timestamp)
    {
        this.uid = uid;
        this.text = text;
        this.authorId = authorId;
        this.timestamp = timestamp;
    }

}
