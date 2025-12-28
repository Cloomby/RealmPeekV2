using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RealmPeek.Core.Infrastructure
{
    public class UserInput<T>
    {
        // The actual value stored inside the class
        private readonly T _value;

        public UserInput(string prompt, ConsoleColor color = ConsoleColor.White)
        {
            Console.ForegroundColor = color;

            Console.Write($"{prompt} \n\n> ");
            string? input = Console.ReadLine().Trim() ?? String.Empty;

            _value = (T)Convert.ChangeType(input, typeof(T));
        }

        public static implicit operator T(UserInput<T> wrapper)
        {
            return wrapper._value;
        }
    }
}
