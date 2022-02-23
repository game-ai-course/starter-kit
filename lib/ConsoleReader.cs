using System;
using System.Collections.Generic;
using System.Linq;

namespace bot
{
    public class ConsoleReader
    {
        private int curLine;
        private readonly string[] textLines;
        private readonly List<string> linesFromConsole = new List<string>();
        
        public ConsoleReader(string text = null)
        {
            textLines = text?.Split('|');
        }

        public int ReadNum()
        {
            return ReadLine().ToInt();
        }

        public long ReadLong()
        {
            return ReadLine().ToLong();
        }
        
        public int[] ReadNums()
        {
            return ReadLine().Split(' ').Select(int.Parse).ToArray();
        }
        
        public string ReadLine()
        {
            if (textLines != null) return textLines[curLine++];
            linesFromConsole.Add(Console.ReadLine());
            return linesFromConsole.Last();
        }

        public void FlushToStdErr()
        {
            Console.Error.WriteLine(linesFromConsole.StrJoin('|'));
            linesFromConsole.Clear();
        }
    }
}