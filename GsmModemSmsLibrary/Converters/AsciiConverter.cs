using System;
using System.Collections.Generic;
using System.Text;

namespace GsmModemSmsLibrary.Converters
{
    public static class AsciiConverter
    {
        private static readonly Dictionary<char, char> _mappings = new Dictionary<char, char>()
        {
            {'á', 'a'},
            {'č', 'c'},
            {'ď', 'd'},
            {'é', 'e'},
            {'ě', 'e'},
            {'š', 's'},
            {'ř', 'r'},
            {'ž', 'z'},
            {'ý', 'y'},
            {'í', 'i'},
            {'ú', 'u'},
            {'ů', 'u'},
            {'ň', 'n'},
            {'ó', 'o'},
            {'ť', 't'},
            {'_', '.'}            
        };

        public static string ConvertTextToAscii(string original)
        {
            var convert = string.Empty;
            for (var i = 0; i < original.Length; i++)
            {
                int decValue = original[i];
                if ((decValue >= 32 && decValue <= 90) || (decValue >= 97 && decValue <= 122)) convert += original[i];
                else
                {
                    var lowerChar = char.ToLower(original[i]);
                    if (_mappings.ContainsKey(lowerChar)) convert += _mappings[lowerChar];
                    else convert += ' ';
                }                
            }
            return convert;
        }

        public static void AddAsciiMapping(char original, char asciiRepre)
        {
            _mappings[original] = asciiRepre;
        }
    }
}
