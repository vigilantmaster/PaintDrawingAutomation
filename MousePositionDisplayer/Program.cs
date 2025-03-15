using System;
using System.Runtime.InteropServices;
using System.Threading;

public class MouseLocationDisplayer
{
    [StructLayout(LayoutKind.Sequential)]
    public struct POINT
    {
        public int X;
        public int Y;
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool GetCursorPos(out POINT lpPoint);

    static void Main(string[] args)
    {
        Console.WriteLine("Mouse Location Displayer");
        Console.WriteLine("Press Ctrl+C to exit.");

        while (true)
        {
            POINT p;
            if (GetCursorPos(out p))
            {
                Console.SetCursorPosition(0, 2); // Move cursor to the third line
                Console.Write($"X: {p.X}, Y: {p.Y}        "); //Overwrite previous output
            }
            Thread.Sleep(50); // Adjust refresh rate (milliseconds)
        }
    }
}