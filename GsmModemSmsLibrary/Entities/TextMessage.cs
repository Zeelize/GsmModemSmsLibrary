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
        /// <summary>
        /// Number of final tries, if current try is bigger, than message will be ignored and sending will not take place
        /// </summary>
        public int NumberOfTries { get; }
        /// <summary>
        /// If first try was not successfull and message will be back in queue, is possible to set DateTime to a new time
        /// if null, next try is immediately proceed
        /// </summary>
        public DateTime? NextTry { get; set; }

        public TextMessage(string text, string number, int tries)
        {
            Text = AsciiConverter.ConvertTextToAscii(text.Length > 160 ? text.Substring(0, 160) : text);
            Number = number;            
            CurrentTry = 0;
            NumberOfTries = tries;
            NextTry = null;
        }        
    }
}
