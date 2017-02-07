using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PianoGenerator
{
    static class ParserHelper
    {
        public static int SkipSpaces(string line, int currentIndex)
        {
            while (currentIndex != line.Length && char.IsWhiteSpace(line[currentIndex]))
                ++currentIndex;

            return currentIndex;
        }
        public static int ReadWord(string line, int currentIndex, out string word)
        {
            int varNameLength = 0;

            while ((currentIndex + varNameLength) != line.Length && char.IsLetterOrDigit(line[currentIndex + varNameLength]))
                ++varNameLength;

            word = line.Substring(currentIndex, varNameLength);

            return currentIndex + varNameLength;
        }
        public static int ReadNumber(string line, int currentIndex, bool isInteger, out string number)
        {
            int varNameLength = 0;
            bool hasDot = false;

            if(line[currentIndex] == '-')
            {
                ++varNameLength;
            }
            if (line[currentIndex] == '+')
                ++currentIndex;

            while ((currentIndex + varNameLength) != line.Length && 
                   (char.IsDigit(line[currentIndex + varNameLength]) || line[currentIndex + varNameLength] == '.')
                  )
            {
                if(line[currentIndex + varNameLength] == '.')
                {
                    if (isInteger)
                        break;
                    if (hasDot)
                        break;
                    hasDot = true;
                }
                ++varNameLength;
            }

            number = line.Substring(currentIndex, varNameLength);

            return currentIndex + varNameLength;
        }
    }
}
