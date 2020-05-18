using GsmModemSmsLibrary.Enums;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using GsmModemSmsLibrary.Entities;
using System.Collections.Generic;
using System.Text;
using crozone.SerialPorts.Abstractions;
using crozone.SerialPorts.LinuxSerialPort;
using crozone.SerialPorts.WindowsSerialPort;

namespace GsmModemSmsLibrary
{    
    /// <summary>
    /// Class representing GSMModem including methods to invoke connection and sms sending with SerialPorts
    /// </summary>
    public class GsmModem
    {
        private const int WAIT_TIMEOUT = 10000;
        private readonly static string CTRL_Z = char.ConvertFromUtf32(26);

        private ISerialPort _serialPort;
        private InputFlagEnum _inputFlag;
        private volatile bool _consume;
        private volatile bool _consuming;
        private readonly BlockingCollection<TextMessage> _smsQueue = new BlockingCollection<TextMessage>();
        private volatile string _lastReceived;
        private readonly ConcurrentBag<TextMessage> _notSend = new ConcurrentBag<TextMessage>();

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
        public bool InitializeConnection(string portName, int baudRate, string parityS = "None", int dataBits = 8, string stopBitsS = "One")
        {
            try
            {
                var parity = (Parity)Enum.Parse(typeof(Parity), parityS);
                var stopBits = (StopBits)Enum.Parse(typeof(StopBits), stopBitsS);
                if (OperatingSystem.IsLinux()) _serialPort = new LinuxSerialPort(portName)
                {
                    EnableDrain = false,
                    MinimumBytesToRead = 2,
                    ReadTimeout = WAIT_TIMEOUT,
                    BaudRate = baudRate,
                    Parity = parity,
                    DataBits = dataBits,
                    StopBits = stopBits,
                    Handshake = Handshake.None
                };
                else if (OperatingSystem.IsWindows()) _serialPort = new WindowsSerialPort(new System.IO.Ports.SerialPort(portName))
                {
                    ReadTimeout = WAIT_TIMEOUT,
                    BaudRate = baudRate,
                    Parity = parity,
                    DataBits = dataBits,
                    StopBits = stopBits,
                    Handshake = Handshake.None
                };
                else throw new Exception("Not supported Opearting System");

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
            if (_serialPort == null) return;
            _serialPort.Close();            
        }

        public void Dispose()
        {
            if (_serialPort == null) return;
            _serialPort.Dispose();
        }

        /// <summary>
        /// Add new message to message queue for consumer
        /// </summary>
        /// <param name="msg"></param>
        public void AddMessageToSend(TextMessage msg)
        {
            _smsQueue.Add(msg);
            if (_consuming) return;
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
            return SerialPortDataRead();            
        }

        private bool SerialPortDataRead()
        {
            var input = string.Empty;
            try
            {
                var buffer = new byte[1000];
                var data = _serialPort.BaseStream.Read(buffer, 0, buffer.Length);
                if (data == 0) return false;
                for (var i = 0; i < data; i++) input += (char)buffer[i];
            }
            catch (TimeoutException)
            {
                return false;
            }
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
            return true;
        }

        private void AtCommand(string command, InputFlagEnum flag)
        {
            _inputFlag = flag;
            var bytes = Encoding.ASCII.GetBytes(command);
            _serialPort.BaseStream.Write(bytes, 0, bytes.Length);
            _serialPort.BaseStream.Flush();
        }
        #endregion

        #region Consumer
        private void SmsConsumerSender()
        {
            _consuming = true;
            while (_smsQueue.TryTake(out var message) && _consume)
            {
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
