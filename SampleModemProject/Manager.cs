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
                if (!modem.SendSms(message))
                {
                    message.CurrentTry++;
                    _notSend.Add(message);                    
                }                
            }
            _consuming = false;
        }       
        
        public static void End()
        {
            _consume = false;
        }
    }
}
