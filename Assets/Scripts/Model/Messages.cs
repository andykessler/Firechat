using System.Collections.Generic;

public class Messages
{

    public string chatId;
    public List<Message> messages;

    public Messages()
    {

    }

    public Messages(string chatId, List<Message> messages)
    {
        this.chatId = chatId;
        this.messages = messages; // deep copy?
    }

}
