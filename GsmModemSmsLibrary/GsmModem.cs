using GsmModemSmsLibrary.Enums;
using System;
using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using GsmModemSmsLibrary.Entities;
using System.Collections.Generic;

namespace GsmModemSmsLibrary
{    
    /// <summary>
    /// Class representing GSMModem including methods to invoke connection and sms sending with SerialPorts
    /// </summary>
    public class GsmModem
    {
        private const int WAIT_TIMEOUT = 10000;
        private readonly static string CTRL_Z = char.ConvertFromUtf32(26);

        private SerialPort _serialPort;
        private InputFlagEnum _inputFlag;
        private bool _dataReceived;
        private volatile bool _consume;
        private volatile bool _consuming;
        private BlockingCollection<TextMessage> _smsQueue = new BlockingCollection<TextMessage>();
        private volatile string _lastReceived;
        private ConcurrentBag<TextMessage> _notSend = new ConcurrentBag<TextMessage>();

        /// <summary>
        /// Last occured error in GsmModem library
        /// </summary>
        public Exception LastError { get; private set; }
        /// <summary>
        /// Flag if phone is connected to serialPort, command <AT> returns 0
        /// </summary>
        public bool PhoneConnected { get; private set; }        

        /// <summary>
        /// Initialize connection to serial port with correct settings
        /// </summary>
        /// <returns>If connection was successfull and port is open</returns>
        public bool InitializeConnection(string portName, int baudRate, Parity parity = Parity.None, int dataBits = 8, StopBits stopBits = StopBits.One)
        {
            try
            {
                _serialPort = new SerialPort(portName, baudRate, parity, dataBits, stopBits);
                _serialPort.DataReceived += new SerialDataReceivedEventHandler(SerialPort_DataReceived);
                _serialPort.Open();
                if (!_serialPort.IsOpen) throw new InvalidOperationException("Serial port could not be opened!");
                AtCommand("AT\r", InputFlagEnum.PhoneConnectionCheck);
                if (!ModemResponseTimeout()) throw new TimeoutException("Expected response, modem did not respond in time!");
            } catch(Exception e)
            {
                LastError = e;
                return false;
            }
            return true;
        }

        /// <summary>
        /// Ends consumer and close serial port
        /// </summary>        
        public void Close()
        {
            _consume = false;
            while (_consuming) {}
            _serialPort.Close();
        }

        /// <summary>
        /// Add new message to message queue for consumer
        /// </summary>
        /// <param name="msg"></param>
        public void AddMessageToSend(TextMessage msg)
        {
            _smsQueue.Add(msg);
        }

        /// <summary>
        /// Will start client consumer for sms messages. On seperate thread waiting for messages in queue to send
        /// </summary>
        public void StartSmsConsumer()
        {
            if (_consuming) return;
            _consume = true;
            Task.Run(() => SmsConsumerSender());
        }

        /// <summary>
        /// Returns copy of not send messages
        /// </summary>
        /// <returns></returns>
        public IEnumerable<TextMessage> GetNotSendMessages()
        {
            return _notSend.ToArray();
        }

        #region Private Methods
        private bool ModemResponseTimeout()
        {
            var timeout = 0;
            while (!_dataReceived && timeout < WAIT_TIMEOUT)
            {
                Thread.Sleep(100);
                timeout += 100;
            }
            if (!_dataReceived && timeout >= WAIT_TIMEOUT) return false;
            return true;
        }

        private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            var buffer = new char[1000];
            var data = _serialPort.Read(buffer, 0, buffer.Length);
            var input = string.Empty;
            for (var i = 0; i < data; i++) input += buffer[i];
            _lastReceived = null;
            switch (_inputFlag)
            {
                case InputFlagEnum.PhoneConnectionCheck:
                    PhoneConnected = string.Equals("0\r", input);
                    if (!PhoneConnected) LastError = new FormatException($"Serial port response: {input}");
                    break;
                case InputFlagEnum.SmsHeaderCheck:
                    if (!input.Contains(">")) break;
                    _lastReceived = input;
                    break;
                case InputFlagEnum.SmsSendCheck:
                    if (!input.Contains("+CMGS:") || !input.Contains("0\r")) break;
                    _lastReceived = input;
                    break;
            }
            _dataReceived = true;
        }

        private void AtCommand(string command, InputFlagEnum flag)
        {
            _dataReceived = false;
            _inputFlag = flag;
            _serialPort.Write(command);
        }
        #endregion

        #region Consumer
        private void SmsConsumerSender()
        {
            _consuming = true;
            while (_consume)
            {
                if (!_smsQueue.TryTake(out var message)) continue;
                try
                {
                    var command = $"AT+CMGS={message.Number}\r";
                    AtCommand(command, InputFlagEnum.SmsHeaderCheck);                    
                    if (!ModemResponseTimeout() || _lastReceived == null) throw new Exception();
                    AtCommand(message.Text + CTRL_Z, InputFlagEnum.SmsSendCheck);
                    if (!ModemResponseTimeout() || _lastReceived == null) throw new Exception();
                } catch (Exception)
                {
                    message.CurrentTry++;
                    if (message.CurrentTry >= message.NumberOfTries)
                    {
                        _notSend.Add(message);
                        continue;
                    }
                    _smsQueue.Add(message);
                }
            }
            _consuming = false;
        }
        #endregion
    }
}
