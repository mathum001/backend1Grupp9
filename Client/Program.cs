using System;
using System.Net.Sockets;
using System.Text;

namespace Client
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello Client!");
            StartClient();
        }

        static void StartClient()
        {
            try
            {
                TcpClient client = new TcpClient("127.0.0.1", 8080);
                Console.WriteLine("Connected to the server.");

                NetworkStream stream = client.GetStream();

                Console.WriteLine("Enter 'register' or 'login':");
                string userInput = Console.ReadLine();

                if (userInput?.ToLower() == "register")
                {
                    RegisterUser(stream);
                }
                else if (userInput?.ToLower() == "login")
                {
                    LoginUser(stream);
                }

                //Ny tråd som lyssnar på medd från servern
                Thread receiveThread = new Thread(() => ReceiveMessages(stream));
                receiveThread.Start();

                while (true)
                {
                    Console.WriteLine("Enter 'send' to broadcast or 'private' for a private message:");
                    string command = Console.ReadLine();

                    if (command?.ToLower() == "send")
                    {
                        BroadcastMessage(stream);
                    }
                    else if (command?.ToLower() == "private")
                    {
                        SendPrivateMessage(stream);
                    }
                    else
                    {
                        client.Close();
                        break;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: " + e.Message);
            }
        }

        static void RegisterUser(NetworkStream stream)
        {
            Console.WriteLine("Enter username:");
            string username = Console.ReadLine();
            Console.WriteLine("Enter password:");
            string password = Console.ReadLine();

            // Send registration data to the server
            string dataToSend = $"register {username} {password}";
            SendData(stream, dataToSend);
        }

        static void LoginUser(NetworkStream stream)
        {
            Console.WriteLine("Enter username:");
            string username = Console.ReadLine();
            Console.WriteLine("Enter password:");
            string password = Console.ReadLine();

            // Send login data to the server
            string dataToSend = $"login {username} {password}";
            SendData(stream, dataToSend);
        }

        static void BroadcastMessage(NetworkStream stream)
        {
            Console.WriteLine("Enter your message:");
            string message = Console.ReadLine();

            // Send broadcast message to the server
            string dataToSend = $"send {message}";
            SendData(stream, dataToSend);
        }

        static void SendPrivateMessage(NetworkStream stream)
        {
            Console.WriteLine("Enter recipient's username:");
            string recipient = Console.ReadLine();

            Console.WriteLine("Enter your private message:");
            string message = Console.ReadLine();

            // Send private message to the server
            string dataToSend = $"sendPrivate {recipient} {message}";
            SendData(stream, dataToSend);
        }

        static void SendData(NetworkStream stream, string data)
        {
            // Send the data to the server
            byte[] dataToSend = Encoding.ASCII.GetBytes(data);
            stream.Write(dataToSend, 0, dataToSend.Length);
        }

        static void ReceiveMessages(NetworkStream stream)
        {
            try
            {
                byte[] buffer = new byte[1024];
                while (true)
                {
                    int bytesRead = stream.Read(buffer, 0, buffer.Length);
                    if (bytesRead > 0)
                    {
                        string message = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                        Console.WriteLine(message);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Error receiving message from server: " + e.Message);
            }
        }
    }
}
