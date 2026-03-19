using System;
using System.IO;
using System.Text.RegularExpressions;

namespace ProxyPatternTextReader
{
    
    public interface ISmartTextReader
    {
        char[][] ReadFile(string path);
    }

    
    public class SmartTextReader : ISmartTextReader
    {
        public char[][] ReadFile(string path)
        {
            string[] lines = File.ReadAllLines(path);
            char[][] result = new char[lines.Length][];

            for (int i = 0; i < lines.Length; i++)
            {
                result[i] = lines[i].ToCharArray();
            }

            return result;
        }
    }

    // Proxy 1 Logger
    public class SmartTextChecker : ISmartTextReader
    {
        private readonly ISmartTextReader reader;

        public SmartTextChecker(ISmartTextReader reader)
        {
            this.reader = reader;
        }

        public char[][] ReadFile(string path)
        {
            Console.WriteLine($"Opening file: {path}");

            char[][] data = reader.ReadFile(path);

            Console.WriteLine("File successfully read");

            int linesCount = data.Length;
            int charsCount = 0;

            foreach (var line in data)
            {
                charsCount += line.Length;
            }

            Console.WriteLine($"Lines: {linesCount}");
            Console.WriteLine($"Characters: {charsCount}");

            Console.WriteLine("Closing file");

            return data;
        }
    }

    // Proxy 2 Access Control
    public class SmartTextReaderLocker : ISmartTextReader
    {
        private readonly ISmartTextReader reader;
        private readonly Regex regex;

        public SmartTextReaderLocker(ISmartTextReader reader, string pattern)
        {
            this.reader = reader;
            this.regex = new Regex(pattern);
        }

        public char[][] ReadFile(string path)
        {
            if (regex.IsMatch(path))
            {
                Console.WriteLine("Access denied!");
                return Array.Empty<char[]>();
            }

            return reader.ReadFile(path);
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            string allowedFile = "allowed.txt";
            string restrictedFile = "secret.txt";

            ISmartTextReader reader = new SmartTextReader();

            ISmartTextReader checker = new SmartTextChecker(reader);

            ISmartTextReader locker = new SmartTextReaderLocker(checker, "secret");

            Console.WriteLine("=== Allowed file ===");
            locker.ReadFile(allowedFile);

            Console.WriteLine("\n=== Restricted file ===");
            locker.ReadFile(restrictedFile);
        }
    }
}