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
        private string _lastReceived;
        private bool _sending;

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
                    MinimumBytesToRead = 0,
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
                if (OperatingSystem.IsWindows()) _serialPort.BaseStream.WriteTimeout = WAIT_TIMEOUT;
                if (!AtCommand("AT\r", InputFlagEnum.PhoneConnectionCheck)) throw new TimeoutException("Could not write to modem!");
                if (!ModemReadResponseTimeout()) throw new TimeoutException("Expected response, modem did not respond in time!");
            }
            catch (Exception e)
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
            if (_serialPort == null) return;
            _serialPort.Close();
        }

        public void Dispose()
        {
            if (_serialPort == null) return;
            _serialPort.Dispose();
        }

        #region Private Methods        
        private bool ModemReadResponseTimeout()
        {
            string input;
            try
            {
                input = Read();
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
                    if (!PhoneConnected)
                        LastError = new FormatException($"Serial port response. RESPONSE: <{input}>");
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

        private string Read()
        {
            var totalBytesReceived = 0;
            var buffer = new byte[1026];
            _serialPort.ReadTimeout = WAIT_TIMEOUT;
            var secondRun = false;
            while (true)
            {
                Thread.Sleep(10);
                if (secondRun) _serialPort.ReadTimeout = 0;
                int bytesReceived;
                try
                {
                    bytesReceived = _serialPort.BaseStream.Read(buffer, totalBytesReceived, buffer.Length - totalBytesReceived);
                }
                catch (TimeoutException)
                {
                    bytesReceived = 0;
                }
                totalBytesReceived += bytesReceived;
                if (bytesReceived <= 0) break;
                if (totalBytesReceived >= buffer.Length) break;
                secondRun = true;
            }
            return Encoding.ASCII.GetString(buffer, 0, totalBytesReceived);
        }

        private bool AtCommand(string command, InputFlagEnum flag)
        {
            try
            {
                _inputFlag = flag;
                var bytes = Encoding.ASCII.GetBytes(command);
                Thread.Sleep(100);
                _serialPort.BaseStream.Write(bytes, 0, bytes.Length);
                _serialPort.BaseStream.Flush();
                return true;
            }
            catch (TimeoutException)
            {
                return false;
            }
        }
        #endregion

        #region Consumer
        public bool SendSms(TextMessage message)
        {
            if (_sending)
                return false;
            _sending = true;
            try
            {
                var command = $"AT+CMGS={message.Number}\r";
                if (!AtCommand(command, InputFlagEnum.SmsHeaderCheck)) throw new Exception();
                if (!ModemReadResponseTimeout() || _lastReceived == null) throw new Exception();
                if (!AtCommand(message.Text + CTRL_Z, InputFlagEnum.SmsSendCheck)) throw new Exception();
                if (!ModemReadResponseTimeout() || _lastReceived == null) throw new Exception();
            }
            catch (Exception)
            {
                message.CurrentTry++;
                _sending = false;
                return false;
            }
            _sending = false;
            return true;
        }
        #endregion
    }
}
