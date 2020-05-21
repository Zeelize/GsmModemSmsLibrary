using System;
using GsmModemSmsLibrary;
using GsmModemSmsLibrary.Entities;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace SampleModemProject
{
    internal class Program
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
                    Console.WriteLine("Phone numbers:");
                    var numbers = Console.ReadLine().Split(';');
                    do
                    {
                        Console.WriteLine("Sms text:");
                        var smsText = Console.ReadLine();
                        Console.WriteLine("Num of sms:");
                        var num = int.Parse(Console.ReadLine());

                        for (var i = 0; i < num; i++)
                        {
                            numbers = Extensions.Randomize<string>(numbers).ToArray();
                            foreach (var number in numbers)
                            {
                                var sms = new TextMessage(smsText, number, 3);
                                Manager.AddSms(sms, modem);
                            }
                        }
                    } while (Console.ReadLine() == "y");
                }
            }

            Console.WriteLine("Press any key to close...");
            Console.ReadKey();
            Manager.End();
            modem.Close();
            modem.Dispose();
            Console.ReadKey();
        }        
    }
}
