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

        public static async void AddSms(TextMessage msg, GsmModem modem)
        {
            _smsQueue.Add(msg);
            if (_consuming) return;
            _consume = true;
            await Task.Run(() => SmsConsumer(modem));
        }

        private static void SmsConsumer(GsmModem modem)
        {
            _consuming = true;
            while (_consume && _smsQueue.TryTake(out var message))
            {
                if (message.NextTry.HasValue && message.NextTry.Value > DateTime.Now)
                {
                    // its still not time to send this next try, so add it back to queue and proceed to the next planned sms
                    _smsQueue.Add(message);
                    continue;
                }
                if (!modem.SendSms(message))
                {
                    message.CurrentTry++;
                    if (message.CurrentTry > message.NumberOfTries)
                        _notSend.Add(message);
                    else
                    {
                        message.NextTry = DateTime.Now.AddMinutes(3);
                        _smsQueue.Add(message);                        
                    }                    
                }
                Thread.Sleep(500);
            }
            _consuming = false;
        }       
        
        public static void End()
        {
            _consume = false;
        }
    }
}
