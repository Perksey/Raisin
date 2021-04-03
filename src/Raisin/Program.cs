using System;

namespace Raisin
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("RAISIN - Static Site Generator - " +
                              $"v{typeof(Program).Assembly.GetName().Version?.ToString(3)}");
            Console.WriteLine();
        }
    }
}