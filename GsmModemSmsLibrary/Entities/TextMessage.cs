using System;
using System.Collections.Generic;
using System.Text;

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
        /// Number of tries to send message before it will be terminated and sending will not take place
        /// </summary>
        public int NumberOfTries { get; }
        /// <summary>
        /// Try counter, if bigger than NumberOfTries, than message will be ignored and sending will not take place
        /// </summary>
        public int CurrentTry { get; set; }

        public TextMessage(string text, string number, int tries)
        {
            Text = text.Length > 160 ? text.Substring(0, 160) : text;
            Number = number;
            NumberOfTries = tries;
            CurrentTry = 0;
        }
    }
}
