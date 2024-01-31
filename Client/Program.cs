using System;
using System.Net.Sockets;
using System.Text;
using System.Diagnostics;

namespace Client;
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
            // Skapa en TCP-klient och anslut till servern
            TcpClient client = new TcpClient("127.0.0.1", 8080);
            Console.WriteLine("Ansluten till servern.");

            // Hämta nätverksströmmen från klienten
            NetworkStream stream = client.GetStream();



            // Skicka ett meddelande till servern
            System.Console.WriteLine("Vilket meddelande vill du skicka?");
            string pass = Console.ReadLine();




            byte[] dataToSend = Encoding.ASCII.GetBytes(pass);
            stream.Write(dataToSend, 0, dataToSend.Length);
            Console.WriteLine("Skickat meddelande till servern: " + pass);

            // Läs inkommande data från servern
            byte[] buffer = new byte[1024];
            int bytesRead = stream.Read(buffer, 0, buffer.Length);

            // Konvertera inkommande data till en sträng och skriv ut det
            string dataReceived = Encoding.ASCII.GetString(buffer, 0, bytesRead);
            Console.WriteLine("Meddelande från servern: " + dataReceived);


            // Stäng anslutningen till servern
            client.Close();
        }
        catch (Exception e)
        {
            Console.WriteLine("Error: " + e.Message);
        }
    }


}
