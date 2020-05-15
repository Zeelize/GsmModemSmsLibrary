using System;
using GsmModemSmsLibrary;
using GsmModemSmsLibrary.Entities;
using System.Linq;

namespace SampleModemProject
{
    class Program
    {
        static void Main(string[] args)
        {
            var modem = new GsmModem();
            Console.WriteLine("Port:");
            var port = Console.ReadLine();
            Console.WriteLine("Baud:");
            var baud = int.Parse(Console.ReadLine());
            if (!modem.InitializeConnection(port, baud)) Console.WriteLine("ERROR: " + modem.LastError.Message);
            else
            {
                if (!modem.PhoneConnected) Console.WriteLine("PHONE NOT CONNECTED: " + modem.LastError.Message);
                else
                {
                    Console.WriteLine("PHONE CONNECTED");
                    modem.StartSmsConsumer();
                    Console.WriteLine("Phone number:");
                    var number = Console.ReadLine();
                    Console.WriteLine("Sms text:");
                    var smsText = Console.ReadLine();
                    Console.WriteLine("Num of sms:");
                    var num = int.Parse(Console.ReadLine());
                    for(var i = 0; i < num; i++)
                    {
                        var k = i * 20;
                        var newText = smsText;
                        for (var j = 0; j <= k; j++) newText += 'o';
                        var sms = new TextMessage(newText, number, 3);
                        modem.AddMessageToSend(sms);
                    }                    
                }
            }

            Console.WriteLine("Press any key to close...");
            Console.ReadKey();
            var notSend = modem.GetNotSendMessages();
            Console.WriteLine("Not send: " + notSend.Count());
            modem.Close();
        }
    }
}
