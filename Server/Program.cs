
using MongoDB.Driver;
using MongoDB.Bson;
using Microsoft.VisualBasic;
using MongoDB.Bson.Serialization.Attributes;
using BCrypt.Net;


using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Data.Common;

namespace Server;

class Program
{
    static void Main(string[] args)
    {
        StartServer();

    }


    static void StartServer()
    {
        TcpListener server = null;
        try
        {
            // Ange IP-adressen och porten som servern ska lyssna på
            IPAddress ipAddress = IPAddress.Parse("127.0.0.1");
            int port = 8080;

            // Skapa en TCP-listener på den angivna IP-adressen och porten
            server = new TcpListener(ipAddress, port);

            // Starta lyssnaren
            server.Start();
            Console.WriteLine("Servern är igång och lyssnar på port " + port);



            while (true)
            {
                // Vänta på en anslutning från en klient
                TcpClient client = server.AcceptTcpClient();
                Console.WriteLine("En klient har anslutit.");

                //ny tråd för separata klienter
                Thread clientThread = new Thread(HandleClient);
                clientThread.Start(client);

            }
        }
        catch (Exception e)
        {
            Console.WriteLine("Error: " + e.Message);
        }
        finally
        {
            // Stäng servern
            server?.Stop();
        }
    }



    static void HandleClient(object klient)
    {
        TcpClient client = (TcpClient)klient;
        NetworkStream stream = client.GetStream();

        // Hantera kommunikationen med klienten
        try
        {
            while (client.Connected)
            {
                byte[] buffer = new byte[1024];
                int bytesRead = stream.Read(buffer, 0, buffer.Length);
                if (bytesRead == 0)
                {
                    // Om inga bytes läses in, betyder det att klienten har kopplat från
                    Console.WriteLine("Klienten har kopplat från.");
                    break;
                }
                string dataReceived = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                Console.WriteLine("Meddelande från klienten: " + dataReceived);

                // Hantera inkommande meddelanden och skicka svar
                string[] data = dataReceived.Split(" ");
                string command = data[0];
                string parameters = dataReceived.Substring(command.Length).Trim();


                if (commandActions.ContainsKey(command))
                {
                    commandActions[command].Invoke(parameters, stream);
                }
                else
                {
                    System.Console.WriteLine("Felaktigt kommando " + command);
                    SendMessage(" Ogiltigt kommando" + command, stream);
                }


                // Skicka tillbaka det mottagna meddelandet till klienten
                /* byte[] dataToSend = Encoding.ASCII.GetBytes(dataReceived);
                stream.Write(dataToSend, 0, dataToSend.Length); */
            }
        }
        catch (Exception e)
        {
            System.Console.WriteLine("Error " + e);
        }
        finally
        {
            client.Close();
        }
    }

    static void RegisterUser(string parameters, NetworkStream stream)
    {
        System.Console.WriteLine("Du försökte göra en registrering" + parameters);
        string[] data = parameters.Split(" ");
        if (data.Length < 2)
        {
            Console.WriteLine("Felaktigt format på registrering.");
            return;
        }
        else
        {
            string userName = data[0];
            string password = data[1];
            Random random = new Random();
            int randomTal = random.Next(1, 1000);

            // Hash the user's password
            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);

            User newUser = new User
            {
                Id = randomTal,
                UserName = userName,
                Password = hashedPassword // Store hashed password in the database
            };

            IMongoCollection<User> users = FetchMongoUser();
            User existingUser = users.Find(x => x.UserName == userName).FirstOrDefault();


            if (existingUser != null)
            {
                System.Console.WriteLine("Användarnamnet är upptaget");
                return;
            }

            Add(users, newUser);

        }
    }

    static void Add(IMongoCollection<User> collection, User user)
    {
        collection.InsertOne(user);
        System.Console.WriteLine("Användare registrerad!");
    }

    static void LoginUser(string parameters, NetworkStream stream)
    {
        System.Console.WriteLine("Du försökte logga in" + parameters);
        string[] loginData = parameters.Split();
        if (loginData.Length != 2)
        {
            System.Console.WriteLine("Felaktiga uppgifter"); //TODO: skicka medd till client om att det ej gick
        }

        string userName = loginData[0];
        string passWord = loginData[1];
        int loginId = Authenticate(userName, passWord);

        if (loginId == 0)
        {
            System.Console.WriteLine("Felaktiga uppgifter");
        }
        else
        {
            userStreams[userName] = stream;
            string text = "Du loggades in!";
            System.Console.WriteLine("Inloggning lyckades: " + "-----" + userName);
            byte[] dataToSend = Encoding.ASCII.GetBytes(text);
            stream.Write(dataToSend, 0, dataToSend.Length);
            string loginMessage = userName + " har loggat in.";
            SendMessage(loginMessage, stream);

            Messages userMessages = FetchMongoMessages(userName);
            byte[] messageHistory = Encoding.ASCII.GetBytes("Meddelande Historik:");
            stream.Write(messageHistory, 0, messageHistory.Length);
            if (userMessages != null)
            {

                foreach (string message in userMessages.UserMessages)
                {
                    byte[] messageList = Encoding.ASCII.GetBytes(message + "\n");
                    stream.Write(messageList, 0, messageList.Length);

                }
            }

        }

    }

    static int Authenticate(string userName, string passWord)
    {
        int id = 0;
        IMongoCollection<User> users = FetchMongoUser();
        User user = users.Find(x => x.UserName == userName).FirstOrDefault();

        if (user != null)
        {
            // Verify the entered password with the stored hashed password
            bool isPasswordCorrect = BCrypt.Net.BCrypt.Verify(passWord, user.Password);

            if (isPasswordCorrect)
            {
                id = user.Id;
            }
        }
        return id;
    }

    static void SendMessage(string message, NetworkStream senderStream)
    {
        string username = GetUsernameByStream(senderStream);
        string messageToSend = $"{username + " skickade: " + message}";
        foreach (var kvp in userStreams)
        {
            if (kvp.Value != senderStream)
            {
                byte[] dataToSend = Encoding.ASCII.GetBytes(messageToSend);
                kvp.Value.Write(dataToSend, 0, dataToSend.Length);
                AddSingleMessageToDB(kvp.Value, message);
            }

        }
    }

    static void SendPrivateMessage(string parameters, NetworkStream senderStream)
    {
        string[] data = parameters.Split(" ");
        if (data.Length < 2)
        {
            Console.WriteLine("Incorrect private message format.");
            return;
        }
        string sender = GetUsernameByStream(senderStream);
        string recipient = data[0];
        string message = sender + ": " + parameters.Substring(recipient.Length).Trim();

        if (userStreams.TryGetValue(recipient, out NetworkStream recipientStream))
        {
            byte[] dataToSend = Encoding.ASCII.GetBytes(message);
            recipientStream.Write(dataToSend, 0, dataToSend.Length);
        }
        else
        {
            Console.WriteLine($"User '{recipient}' not found or offline.");
        }

        AddSingleMessageToDB(recipientStream, message);
    }

    static IMongoCollection<User> FetchMongoUser()
    {
        const string newpass = "KokxLPCVbH0hKrp2";
        string connectionUri = "mongodb+srv://mattiashummer:" + newpass + "@cluster0.y5yh9uz.mongodb.net/?retryWrites=true&w=majority";

        var settings = MongoClientSettings.FromConnectionString(connectionUri);
        // Set the ServerApi field of the settings object to Stable API version 1
        settings.ServerApi = new ServerApi(ServerApiVersion.V1);
        // Create a new client and connect to the server
        var client = new MongoClient(settings);
        // Send a ping to confirm a successful connection
        try
        {
            var result = client.GetDatabase("admin").RunCommand<BsonDocument>(new BsonDocument("ping", 1));
            Console.WriteLine("Pinged your deployment. You successfully connected to MongoDB!");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
        // anslut till databasen
        var database = client.GetDatabase("testing");
        //anslut till kollektion
        IMongoCollection<User> collection = database.GetCollection<User>("users");

        return collection;
    }


    static string GetUsernameByStream(NetworkStream stream)
    {
        foreach (var kvp in userStreams)
        {
            if (kvp.Value == stream)
            {
                return kvp.Key; // Returnerar användarnamnet om nätverksströmmen matchar
            }
        }
        return null; // Returnerar null om ingen matchning hittades
    }

    static string AddSingleMessageToDB(NetworkStream stream, string message)
    {

        string username = GetUsernameByStream(stream);
        const string newpass = "KokxLPCVbH0hKrp2";
        string connectionUri = "mongodb+srv://mattiashummer:" + newpass + "@cluster0.y5yh9uz.mongodb.net/?retryWrites=true&w=majority";

        var settings = MongoClientSettings.FromConnectionString(connectionUri);
        settings.ServerApi = new ServerApi(ServerApiVersion.V1);
        var client = new MongoClient(settings);
        try
        {
            var result = client.GetDatabase("admin").RunCommand<BsonDocument>(new BsonDocument("ping", 1));
            Console.WriteLine("Pinged your deployment. You successfully connected to MongoDB!");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
        // anslut till databasen samt lägger till en
        var database = client.GetDatabase("testing");

        IMongoCollection<Messages> messageCollection = database.GetCollection<Messages>("messages");

        var filter = Builders<Messages>.Filter.Eq(message => message.UserName, username);
        var update = Builders<Messages>.Update.Push(message => message.UserMessages, message);

        int maxMessages = 29;
        var userMessages = FetchMongoMessages(username).UserMessages;
        if (userMessages.Count > maxMessages)
        {
            var oldestMessage = userMessages.FirstOrDefault();

            messageCollection.UpdateOne(
                Builders<Messages>.Filter.Eq("UserName", username),
                Builders<Messages>.Update.Pull("UserMessages", oldestMessage)
            );
        }

        messageCollection.UpdateOne(filter, update);



        return null;
    }




    static Messages FetchMongoMessages(string username)
    {
        const string newpass = "KokxLPCVbH0hKrp2";
        string connectionUri = "mongodb+srv://mattiashummer:" + newpass + "@cluster0.y5yh9uz.mongodb.net/?retryWrites=true&w=majority";

        var settings = MongoClientSettings.FromConnectionString(connectionUri);
        // Set the ServerApi field of the settings object to Stable API version 1
        settings.ServerApi = new ServerApi(ServerApiVersion.V1);
        // Create a new client and connect to the server
        var client = new MongoClient(settings);
        // Send a ping to confirm a successful connection
        try
        {
            var result = client.GetDatabase("admin").RunCommand<BsonDocument>(new BsonDocument("ping", 1));
            Console.WriteLine("Pinged your deployment. You successfully connected to MongoDB!");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
        // anslut till databasen
        var database = client.GetDatabase("testing");
        //anslut till kollektion
        IMongoCollection<Messages> collection = database.GetCollection<Messages>("messages");

        Messages existingMessages = collection.Find(x => x.UserName == username).FirstOrDefault();
        if (existingMessages == null)
        {
            Random random = new Random();
            int randomTal = random.Next(1, 1000);
            Messages tempMessage = new Messages(randomTal, username, new List<string>());
            collection.InsertOne(tempMessage);
            return tempMessage;
        }
        return existingMessages;
    }


    //Dictionary för commands
    static Dictionary<string, Action<string, NetworkStream>> commandActions = new Dictionary<string, Action<string, NetworkStream>>()
                {
                    { "register", RegisterUser},
                    { "login", LoginUser},
                    { "send", SendMessage},
                    { "sendPrivate", SendPrivateMessage},
                };

    //Dictionary för att servern ska hålla koll på vilken användare som är vilken
    static Dictionary<string, NetworkStream> userStreams = new Dictionary<string, NetworkStream>();

}

class User
{
    public int Id { get; set; }
    public string UserName { get; set; }
    public string Password { get; set; }
}

class Messages
{
    public int Id { get; set; }
    public string UserName { get; set; }
    public List<string> UserMessages { get; set; }

    public Messages(int id, string userName, List<string> userMessages)
    {
        Id = id;
        UserName = userName;
        UserMessages = userMessages;
    }
}


