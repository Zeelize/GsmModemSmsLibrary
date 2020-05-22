using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GsmModemSmsLibrary;
using GsmModemSmsLibrary.Entities;

namespace SampleModemProject
{
    public static class Manager
    {

        private static volatile bool _consume;
        private static volatile bool _consuming;
        private static readonly BlockingCollection<TextMessage> _smsQueue = new BlockingCollection<TextMessage>();
        private static readonly ConcurrentBag<TextMessage> _notSend = new ConcurrentBag<TextMessage>();
        private static GsmModem _modem;
        private static volatile int _cnt = 0;
        private static Thread _consumer = null;
        private static CancellationTokenSource _src = new CancellationTokenSource();

        public static void SetModem(GsmModem modem)
        {
            _modem = modem;
            _consume = true;
            _consumer = new Thread(SmsConsumer);
            _consumer.Start();
        }

        public static void AddSms(TextMessage msg)
        {
            _smsQueue.Add(msg);
            /*if (_consuming || _cnt > 0) return;
            _consume = true;
            _cnt++;
            SmsConsumer();*/
        }

        private static async void SmsConsumer()
        {
            try
            {
                while (_consume)
                {
                    Console.Write(".");
                    //if (!_smsQueue.TryTake(out var message)) continue;
                    var message = _smsQueue.Take(_src.Token);
                    Console.WriteLine("----new message----" + message.Text);
                    if (message.NextTry.HasValue && message.NextTry.Value > DateTime.Now)
                    {
                        var waitingTime = message.NextTry.Value - DateTime.Now;
                        AddMessageBackToQueue(waitingTime, message);
                        continue;
                    }
                    //Console.WriteLine(message.Text + ": proceed");
                    if (!_modem.SendSms(message))
                    {
                        message.CurrentTry++;
                        if (message.CurrentTry >= message.NumberOfTries)
                        {
                            _notSend.Add(message);
                            Console.WriteLine("Couldnt sent: " + message.Number + ":" + message.Text);
                        }
                        else
                        {
                            message.NextTry = DateTime.Now.AddMinutes(3);
                            _smsQueue.Add(message);
                        }
                    }
                    //Console.WriteLine(message.Text + "->" + DateTime.Now.Second);
                    await Task.Delay(1000);
                    //Console.WriteLine(message.Text + "->" + DateTime.Now.Second);
                }
            } catch (ThreadAbortException)
            {
                Console.WriteLine("Consumer stopping...");
            } catch (OperationCanceledException)
            {
                Console.WriteLine("Opeartion canceled...");
            } catch (Exception e)
            {
                Console.WriteLine("ERROR: " + e);
            }
            Console.WriteLine("Consumer stopped...");
        }
        
        private static async void AddMessageBackToQueue(TimeSpan waitingTime, TextMessage message)
        {
            //Console.WriteLine(message.Text + ": starts waiting:" + waitingTime.TotalMilliseconds);
            await Task.Delay(waitingTime);
            //Console.WriteLine(message.Text + ": stop waiting, adding back");
            AddSms(message);
        }
        
        public static void End()
        {
            _consume = false;
            _src.Cancel();            
        }
    }
}
