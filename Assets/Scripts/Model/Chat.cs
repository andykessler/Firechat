using System.Collections.Generic;

public class Chat
{

    public string uid;

    public string name;

    public List<Message> messages;
    public Dictionary<string, User> participants;

    public Chat()
    {
        // uid?
        name = "Untitled";
        participants = new Dictionary<string, User>();
        messages = new List<Message>();
    }

    public Chat(string uid, string name)
    {
        this.uid = uid;
        this.name = name;
        participants = new Dictionary<string, User>();
        messages = new List<Message>();
    }

}
