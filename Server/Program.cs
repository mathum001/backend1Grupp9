
using MongoDB.Driver;
using MongoDB.Bson;
using Microsoft.VisualBasic;
using MongoDB.Bson.Serialization.Attributes;

using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

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

            // Dictionary to map client commands to corresponding actions
            Dictionary<string, Action<string>> commandActions = new Dictionary<string, Action<string>>()
                {
                    { "register", RegisterUser },
                    { "login", LoginUser },
                    { "send", SendMessage},
                    { "sendPrivate",SendPrivateMessage},
                };

            while (true)
            {
                // Vänta på en anslutning från en klient
                TcpClient client = server.AcceptTcpClient();
                Console.WriteLine("En klient har anslutit.");

                // Hämta klientens nätverksström
                NetworkStream stream = client.GetStream();

                // Buffer för att lagra inkommande data
                byte[] buffer = new byte[1024];
                int bytesRead = stream.Read(buffer, 0, buffer.Length);

                // Konvertera inkommande data till en sträng
                string dataReceived = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                Console.WriteLine("Meddelande från klienten: " + dataReceived);
                string[] data = dataReceived.Split(" ");
                string command = data[0];
                string parameters = dataReceived.Substring(command.Length).Trim();

                if (commandActions.ContainsKey(command))
                {
                    commandActions[command].Invoke(parameters);
                }
                else
                {
                    System.Console.WriteLine("Felaktigt kommando " + command);
                }

                foreach (string ord in data)
                {
                    System.Console.WriteLine(ord + ".");
                }




                // Skicka tillbaka det mottagna meddelandet till klienten
                byte[] dataToSend = Encoding.ASCII.GetBytes(dataReceived);
                stream.Write(dataToSend, 0, dataToSend.Length);

                // Stäng anslutningen till klienten, ska det va med?
                /*  client.Close(); */
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


    static void RegisterUser(string parameters)
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

            User newUser = new User
            {
                Id = randomTal,
                UserName = userName,
                Password = password
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

    static void LoginUser(string parameters)
    {
        System.Console.WriteLine("Du försökte logga in" + parameters);


    }
    static void SendMessage(string parameters)
    {

    }
    static void SendPrivateMessage(string parameters)
    {

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


}

class User
{
    public int Id { get; set; }
    public string UserName { get; set; }
    public string Password { get; set; }

}



