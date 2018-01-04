using Firebase;
using Firebase.Database;
using Firebase.Unity.Editor;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using System.Collections;

public class ChatBox : MonoBehaviour
{
    public VerticalLayoutGroup textLayoutGroup;
    public Text textPrefab;

    Dictionary<string, Text> textDisplays;
    
    InputField inputField;

    User user;
    Chat chat;
    
    private DatabaseReference dbRef;

    // Use this for initialization
    void Start()
    {

        // Set these values before calling into the realtime database.
        FirebaseApp.DefaultInstance.SetEditorDatabaseUrl("https://firechat-28ae9.firebaseio.com");
        dbRef = FirebaseDatabase.DefaultInstance.RootReference;


        inputField = GetComponentInChildren<InputField>();

        textDisplays = new Dictionary<string, Text>();

        // FIX ME utilize firebase for key creation?
        chat = new Chat("room0", "Room 0");

        StartCoroutine(dataLoader());
    }

    IEnumerator dataLoader()
    {
        Text loadingText = GameObject.Find("LoadingText").GetComponent<Text>(); // do better later
        loadingText.enabled = true;
        // Need to do these in order...chain in a better way?
        LoadParticipants();
        yield return new WaitForSeconds(2.5f);
        LoadMessages();
        OnClickJoinButton();
        BuildChatLog();
        loadingText.enabled = false;
    }

    private void LoadParticipants() // LOADING MESSAGES AFTER COMPLETED PARTICIPANT LOAD
    {
        DatabaseReference pRef = dbRef.Child("participants").Child(chat.uid); // for current chat only!
        pRef.ChildAdded += HandleUserAdded;
        pRef.GetValueAsync()
            .ContinueWith(task =>
            {
                if (task.IsFaulted)
                {
                    // Handle the error...
                }
                else if (task.IsCompleted)
                {
                    DataSnapshot snapshot = task.Result;
                    Dictionary<string, bool> pIndex = (Dictionary<string, bool>) snapshot.GetValue(false);
                    LoadUsers(pIndex);
                }
            }
        );
    }

    private void LoadUsers(Dictionary<string, bool> pIndex)
    {
        foreach(string key in pIndex.Keys)
        {
            DatabaseReference uRef = dbRef.Child("users").Child(key);
            uRef.ValueChanged += HandleUserValueChanged;
            uRef.GetValueAsync()
                .ContinueWith(task =>
                {
                    if (task.IsFaulted)
                    {
                        // Handle the error...
                    }
                    else if (task.IsCompleted)
                    {
                        DataSnapshot snapshot = task.Result;
                        User user = JsonUtility.FromJson<User>(snapshot.GetRawJsonValue());
                        chat.participants.Add(key, user);
                    }
                }
            );
        }
    }

    private void LoadMessages()
    {
        dbRef.Child("messages").Child(chat.uid).ChildAdded += HandleChatMessageAdded; // subscribe to changes
        dbRef.Child("messages").Child(chat.uid).OrderByChild("timestamp").GetValueAsync()
            .ContinueWith(task =>
            {
                if (task.IsFaulted)
                {
                    // Handle the error...
                }
                else if (task.IsCompleted)
                {
                    DataSnapshot snapshot = task.Result;
                    Message[] ms = JsonUtility.FromJson<Message[]>(snapshot.GetRawJsonValue());
                    chat.messages.AddRange(ms);
                }
            }
        );
    }

    private void OnDestroy()
    {
        DatabaseReference pRef = dbRef.Child("participants").Child(chat.uid); // for current chat only!
        pRef.ChildAdded -= HandleUserAdded;

        foreach(string key in chat.participants.Keys)
        {
            DatabaseReference uRef = dbRef.Child("users").Child(key);
            uRef.ValueChanged -= HandleUserValueChanged;
        }

        dbRef.Child("messages").Child(chat.uid).ChildAdded -= HandleChatMessageAdded; // unsubscribe
    }

    public void OnClickSendButton()
    {
        string input = inputField.text.Trim();
        if(input.StartsWith("/"))
        {
            HandleInputCommand(input.Substring(1));
        }
        else if (!input.Equals(""))
        {
            DatabaseReference msgRef = dbRef.Child("messages").Child(chat.uid).Push();
            Message m = new Message(msgRef.Key, inputField.text, user.uid, DateTime.Now.Ticks);
            string json = JsonUtility.ToJson(m);
            msgRef.SetRawJsonValueAsync(json);
        }
        inputField.text = "";
    }

    public void OnClickJoinButton()
    {
        DatabaseReference userRef = dbRef.Child("users").Push();
        user = new User(userRef.Key, "Anon", "red");
        string json = JsonUtility.ToJson(user);
        userRef.SetRawJsonValueAsync(json);
        dbRef.Child("participants").Child(chat.uid).Child(user.uid).SetValueAsync(true);
    }

    public void HandleChatMessageAdded(object sender, ChildChangedEventArgs args)
    {
        if (args.DatabaseError != null)
        {
            Debug.LogError(args.DatabaseError.Message);
            return;
        }

        Message m = JsonUtility.FromJson<Message>(args.Snapshot.GetRawJsonValue());
        chat.messages.Add(m);

        AddMessageText(m);
    }

    private Text AddMessageText(Message m)
    {
        Text text = null;
        if(!textDisplays.ContainsKey(m.uid))
        {
            text = Instantiate(textPrefab);
            text.transform.SetParent(textLayoutGroup.transform);
            User user = chat.participants[m.authorId];
            string ts = new DateTime(m.timestamp).ToString("HH:mm:ss");
            text.name = m.uid;
            text.text = String.Format(MESSAGE_TEMPLATE, user.name, ts, m.text);
            text.color = user.GetColor();
            textDisplays.Add(m.uid, text);

            RectTransform rect = textLayoutGroup.GetComponent<RectTransform>();
            float offset = 25f * textDisplays.Count;
            rect.sizeDelta = new Vector2(rect.sizeDelta.x, offset);
        }
        return text;
    }

    private const string MESSAGE_TEMPLATE = "{0} [{1}]: {2}";
    private void BuildChatLog()
    {
        foreach (Message m in chat.messages)
        {
            AddMessageText(m);
        }

    }

    private void HandleUserValueChanged(object sender, ValueChangedEventArgs args)
    {
        if(args.DatabaseError != null)
        {
            Debug.LogError(args.DatabaseError.Message);
            return;
        }

        User user = JsonUtility.FromJson<User>(args.Snapshot.GetRawJsonValue());
        chat.participants[user.uid] = user;
        foreach (Message m in chat.messages)
        {
            if (m.authorId.Equals(user.uid))
            {
                Text text = textDisplays[m.uid];
                string ts = new DateTime(m.timestamp).ToString("HH:mm:ss");
                text.text = String.Format(MESSAGE_TEMPLATE, user.name, ts, m.text);
                text.color = user.GetColor();
            }
        }
    }
    
    private void HandleUserAdded(object sender, ChildChangedEventArgs args)
    {
        if (args.DatabaseError != null)
        {
            Debug.LogError(args.DatabaseError.Message);
            return;
        }

        string userId = args.Snapshot.Key;

        DatabaseReference uRef = dbRef.Child("users").Child(userId);
        uRef.ValueChanged += HandleUserValueChanged;
        uRef.GetValueAsync()
            .ContinueWith(task =>
            {
                if (task.IsFaulted)
                {
                        // Handle the error...
                }
                else if (task.IsCompleted)
                {
                    DataSnapshot snapshot = task.Result;
                    User user = JsonUtility.FromJson<User>(snapshot.GetRawJsonValue());
                    chat.participants.Add(userId, user);
                }
            }
        );
    }

    private void HandleInputCommand(string command)
    {
        string[] split = command.Split(' ');
        if (split.Length == 2)
        {
            switch (split[0])
            {
                case "name":
                    user.name = split[1];
                    dbRef.Child("users").Child(user.uid).Child("name").SetValueAsync(user.name);
                    chat.participants[user.uid] = user; // shouldnt have to do this?
                    return;
                case "color":
                    user.color = split[1];
                    dbRef.Child("users").Child(user.uid).Child("color").SetValueAsync(user.color);
                    chat.participants[user.uid] = user; // shouldnt have to do this?
                    return;
                default:
                    break;
            }
        }
        Debug.Log("Invalid command.");
    }
}
