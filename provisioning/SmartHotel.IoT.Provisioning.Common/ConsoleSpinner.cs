using System;

namespace SmartHotel.IoT.Provisioning.Common
{
    public class ConsoleSpinner
    {
        int counter;

        public ConsoleSpinner()
        {
            counter = 0;
        }

        public void Turn()
        {
            try
            {
                counter++;
            }
            catch (OverflowException e)
            {
                counter = 0;
            }

            switch (counter % 4)
            {
                case 0:
                    {
                        Console.Write("/");
                    }
                    break;
                case 1:
                    {
                        Console.Write("-");
                    }
                    break;
                case 2:
                    {
                        Console.Write("\\");
                    }
                    break;
                case 3:
                    {
                        Console.Write("|");
                    }
                    break;
            }

            Console.SetCursorPosition(Console.CursorLeft - 1, Console.CursorTop);
        }
    }
}
