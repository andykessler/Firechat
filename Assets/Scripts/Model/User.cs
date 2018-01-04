using UnityEngine;

public class User
{

    public string uid;
    public string name;
    public string color;

    public User()
    {
        
    }

    public User(string uid, string name, string color)
    {
        this.uid = uid;
        this.name = name;
        this.color = color;
    }

    public Color GetColor()
    {
        switch(color)
        {
            case "black":
                return Color.black;
            case "white":
                return Color.white;
            case "red":
                return Color.red;
            case "yellow":
                return Color.yellow;
            case "green":
                return Color.green;
            case "blue":
                return Color.blue;
            case "cyan":
                return Color.cyan;
            case "magenta":
                return Color.magenta;
            case "grey":
            case "gray":
                return Color.grey;
            default:
                return Color.black;
            // TODO Accept hex inputs?
        }
    }

}
