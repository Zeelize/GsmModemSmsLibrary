using System;
using System.Collections.Generic;
using System.Text;
using GsmModemSmsLibrary.Converters;

namespace GsmModemSmsLibrary.Entities
{
    public class TextMessage
    {
        /// <summary>
        /// Text of the SMS, if longer than 160, it will be cut
        /// </summary>
        public string Text { get; }
        /// <summary>
        /// Phone number with +420 etc...
        /// </summary>
        public string Number { get; }
        /// <summary>
        /// Try counter, if bigger than NumberOfTries, than message will be ignored and sending will not take place
        /// </summary>
        public int CurrentTry { get; set; }

        public TextMessage(string text, string number)
        {
            Text = AsciiConverter.ConvertTextToAscii(text.Length > 160 ? text.Substring(0, 160) : text);
            Number = number;            
            CurrentTry = 0;
        }        
    }
}
