using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using WindowsInput.Native;
using WindowsInput;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using System.IO;
using Ookii.Dialogs.Wpf;

namespace PaintDrawingAutomation
{
    class Program
    {
        //Constants for Windows messages:
        private const int WM_CLOSE = 0x0010;

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool GetCursorPos(out POINT lpPoint);

        [DllImport("user32.dll")]
        public static extern bool SetCursorPos(int X, int Y);

        public struct POINT
        {
            public int X;
            public int Y;
        }
        //Example of more robust paint window finding.
        static IntPtr FindPaintWindow()
        {
            Process[] processes = Process.GetProcessesByName("mspaint");
            if (processes.Length > 0)
            {
                return processes[0].MainWindowHandle;
            }
            return IntPtr.Zero;
        }

        //Add these variables to main class
        private static readonly int PEN_BUTTON_X = 259;
        private static readonly int PEN_BUTTON_Y = 90;

        [DllImport("user32.dll")]
        private static extern bool SetWindowPlacement(IntPtr hWnd, [In] ref WINDOWPLACEMENT lpwndpl);

        [DllImport("user32.dll")]
        static extern int GetSystemMetrics(SystemMetric smIndex);

        public enum SystemMetric
        {
            SM_CXSCREEN = 0,
            SM_CYSCREEN = 1,
        }

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool CloseWindow(IntPtr hWnd);

        static volatile bool Drawing = true;
        static volatile bool DoneDrawing = true;

        static void Main(string[] args)
        {
            //display menu with options
            while (true)
            {
                Console.WriteLine("\nMenu:");
                Console.WriteLine("1. Draw Circle");
                Console.WriteLine("2. Draw Image from Image (Not Implemented)");
                Console.WriteLine("3. Exit");
                Console.Write("Enter your choice: ");

                string choice = Console.ReadLine();

                switch (choice)
                {
                    case "1":
                        RunDrawing("Circle");
                        break;
                    case "2":
                        RunDrawing("Image");
                        break;
                    case "3":
                        return;
                    default:
                        Console.WriteLine("Invalid choice. Please try again.");
                        break;
                }
            }
        }

        static void RunDrawing(string typeOfDrawing)
        {
            Drawing = true;
            DoneDrawing = false;
            POINT PenButton_Pos, prev_pos;
            List<POINT> coords = new List<POINT>();
            //image recognition for the pen position if we know what the image looks like
            PenButton_Pos.X = PEN_BUTTON_X;
            PenButton_Pos.Y = PEN_BUTTON_Y;
            double width = GetSystemMetrics(SystemMetric.SM_CXSCREEN);
            double height = GetSystemMetrics(SystemMetric.SM_CYSCREEN);
            double centerX = width / 2;
            double centerY = height / 2;
            string imagePath = null; // To store the selected image path
                                     // Handle file selection BEFORE opening Paint
            if (typeOfDrawing == "Image")
            {


                imagePath = getImageFileName(); // Get the path from DrawImage() somehow
                
               
            }

            IntPtr mainConsole = IntPtr.Zero;
            mainConsole = Process.GetCurrentProcess().MainWindowHandle;
            InputSimulator sim = new InputSimulator();
            prev_pos.X = 0;
            prev_pos.Y = 0;
            Console.WriteLine("Opening Paint with keystrokes");
            sim.Keyboard.KeyPress(VirtualKeyCode.LWIN);
            Thread.Sleep(200);
            sim.Keyboard.KeyPress(VirtualKeyCode.VK_P);
            Thread.Sleep(200);
            sim.Keyboard.KeyPress(VirtualKeyCode.VK_A);
            Thread.Sleep(200);
            sim.Keyboard.KeyPress(VirtualKeyCode.VK_I);
            Thread.Sleep(200);
            sim.Keyboard.KeyPress(VirtualKeyCode.VK_N);
            Thread.Sleep(200);
            sim.Keyboard.KeyPress(VirtualKeyCode.VK_T);
            Thread.Sleep(1500);
            sim.Keyboard.KeyPress(VirtualKeyCode.RETURN);
            Thread.Sleep(1500);

            IntPtr paintHandle = IntPtr.Zero;
            mainConsole = GetProcessByName("WindowsTerminal");
            paintHandle = GetProcessByName("mspaint");

            SetForegroundWindow(paintHandle);
            Thread.Sleep(1500);


            Thread quitThread = new Thread(() =>
            {
                while (true && !DoneDrawing)
                {
                    if (Console.KeyAvailable && Console.ReadKey(true).Key == ConsoleKey.Q)
                    {
                        Console.WriteLine("Quitting...");
                        Drawing = false;
                        return;
                    }
                    Thread.Sleep(10);

                    if (DoneDrawing)
                    {
                        SavePaintImage(paintHandle, mainConsole);
                        ClosePaint(paintHandle);
                        SetForegroundWindow(mainConsole); // Return focus
                    }
                }
            });

            quitThread.Start();

            if (Drawing)
            {
                SetForegroundWindow(paintHandle);
                Thread.Sleep(200);
                SetCursorPos(PenButton_Pos.X, PenButton_Pos.Y);
                Thread.Sleep(200);
                sim.Mouse.LeftButtonClick();
                Thread.Sleep(200);
                //Draw the thing at this point we can start drawing different kinds of things

                switch (typeOfDrawing)
                {
                    case "Circle":
                        DoneDrawing = DrawCircle(centerX, centerY, sim);
                        Console.WriteLine("Finished drawing Circle.");
                       
                        SetForegroundWindow(paintHandle);
                        break;
                    case "Image":
                        DoneDrawing = DrawImage(paintHandle , mainConsole, imagePath);
                        Console.WriteLine("Finished drawing Image.");
                        
                        SetForegroundWindow(paintHandle);
                        break;
                }


                //see if we need to quit and save the image if it's drawn successfully
                quitThread.Join();
            }

        }

        private static string? getImageFileName()
        {
            try
            {
                var dialog = new VistaOpenFileDialog();
                dialog.Filter = "Image Files (*.bmp;*.jpg;*.jpeg;*.png;*.gif)|*.bmp;*.jpg;*.jpeg;*.png;*.gif|All files (*.*)|*.*";
                dialog.Title = "Select an Image File";

                if (dialog.ShowDialog() == true)
                {
                    string imagePath = dialog.FileName;
                    Console.WriteLine("FileName:" + dialog.FileName); // Moved inside the if block
                    
                    if (!File.Exists(imagePath))
                    {
                        Console.WriteLine("Image file not found.");
                        return null;
                    }
                    return imagePath;
                    
                }
                else
                {
                    Console.WriteLine("Image selection canceled.");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error drawing image: {ex.Message}");
                return null;
            }
        }

        //Get Process by Name
        static IntPtr GetProcessByName(string processName)
        {
            Process[] procs = Process.GetProcesses();
            foreach (Process proc in procs)
            {
                switch (processName)
                {
                    case "WindowsTerminal":
                        if (proc.ProcessName == "WindowsTerminal")

                        {
                            return proc.MainWindowHandle;
                        }
                        break;
                    case "mspaint":

                        if (proc.ProcessName == "mspaint")
                        {
                            IntPtr paintHandle = proc.MainWindowHandle;
                            var placement = GetPlacement(paintHandle);
                            if (placement.showCmd.ToString() != "Maximized")
                            {
                                try
                                {
                                    WINDOWPLACEMENT wp = new WINDOWPLACEMENT();
                                    GetWindowPlacement(paintHandle, ref wp);
                                    wp.showCmd = ShowWindowCommands.Maximized;
                                    SetWindowPlacement(paintHandle, ref wp);
                                    break;
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"Error maximizing Paint: {ex.Message}");
                                }
                            }
                            else
                            {

                                Console.WriteLine("Paint is maximized.");
                            }
                            return proc.MainWindowHandle;

                        }
                        break;
                }
            }
            return IntPtr.Zero;
        }
          

        
        //Draw Circle Method
        static bool DrawCircle(double centerX, double centerY, InputSimulator sim)
        {
            // move code that draws circle to here
            int radius = 200;
            int points = 360;

            for (int i = 0; i < points && Drawing; i++)
            {
                if (!Drawing) { break; }
                double angle = 2 * Math.PI * i / points;
                int x = (int)(centerX + radius * Math.Cos(angle));
                int y = (int)(centerY + radius * Math.Sin(angle));
                SetCursorPos(x, y);
                sim.Mouse.LeftButtonDown(); //has to be called each time
                Thread.Sleep(1);
            }
            sim.Mouse.LeftButtonUp();
            return true;
        }
        //Draw Image Method
        static bool DrawImage(IntPtr paintHandle, IntPtr mainHandle, string imagePath = null)
        {
            try
            {
                Console.WriteLine("Processing");
                if (File.Exists(imagePath) && paintHandle != null)
                {
                    
                   

                    Bitmap originalImage = new Bitmap(imagePath);
                    Bitmap processedImage = ProcessImage(originalImage);
                    Console.WriteLine("Processed");
                    int startX = (GetSystemMetrics(SystemMetric.SM_CXSCREEN) / 2) - (processedImage.Size.Width / 2);
                    int startY = (GetSystemMetrics(SystemMetric.SM_CYSCREEN) / 2) - (processedImage.Size.Height / 2);

                    // Logic to draw the processed image in Paint using the pen tool
                    DrawProcessedImageInPaint(processedImage, startX, startY);
                    //save
                    SavePaintImage(paintHandle, mainHandle);
                    //close
                    ClosePaint(paintHandle);

                    return true;
                }
                else
                {
                    Console.WriteLine("Image selection not found or canceled.");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error drawing image: {ex.Message}");
                return false;
            }
        }

        static Bitmap ProcessImage(Bitmap originalImage)
        {
            // Implement Gaussian blur and edge detection here
            // 1. Apply Gaussian Blur
            Bitmap blurredImage = ApplyGaussianBlur(originalImage, 5, 1.0); // Adjust kernel size and sigma as needed

            // 2. Apply Edge Detection (using Sobel operator)
            Bitmap edgeDetectedImage = ApplySobelEdgeDetection(blurredImage);
           



            return edgeDetectedImage;
        }
        private static Bitmap ApplyGaussianBlur(Bitmap originalImage, int kernelSize, double sigma)
        {
            Bitmap blurredImage = new Bitmap(originalImage.Width, originalImage.Height);
            double[,] kernel = GenerateGaussianKernel(kernelSize, sigma);
            int kernelRadius = kernelSize / 2;

            for (int x = 0; x < originalImage.Width; x++)
            {
                for (int y = 0; y < originalImage.Height; y++)
                {
                    double redSum = 0, greenSum = 0, blueSum = 0;
                    double weightSum = 0;

                    for (int i = -kernelRadius; i <= kernelRadius; i++)
                    {
                        for (int j = -kernelRadius; j <= kernelRadius; j++)
                        {
                            int sampleX = x + i;
                            int sampleY = y + j;

                            if (sampleX >= 0 && sampleX < originalImage.Width && sampleY >= 0 && sampleY < originalImage.Height)
                            {
                                Color pixel = originalImage.GetPixel(sampleX, sampleY);
                                double weight = kernel[i + kernelRadius, j + kernelRadius];

                                redSum += pixel.R * weight;
                                greenSum += pixel.G * weight;
                                blueSum += pixel.B * weight;
                                weightSum += weight;
                            }
                        }
                    }

                    int red = (int)(redSum / weightSum);
                    int green = (int)(greenSum / weightSum);
                    int blue = (int)(blueSum / weightSum);

                    red = Math.Clamp(red, 0, 255);
                    green = Math.Clamp(green, 0, 255);
                    blue = Math.Clamp(blue, 0, 255);

                    blurredImage.SetPixel(x, y, Color.FromArgb(red, green, blue));
                }
            }

            return blurredImage;
        }
        private static Bitmap ConvertToGrayscale(Bitmap originalImage)
        {
            Bitmap grayscaleImage = new Bitmap(originalImage.Width, originalImage.Height);
            for (int x = 0; x < originalImage.Width; x++)
            {
                for (int y = 0; y < originalImage.Height; y++)
                {
                    Color originalColor = originalImage.GetPixel(x, y);
                    int grayScale = (int)((originalColor.R * 0.3) + (originalColor.G * 0.59) + (originalColor.B * 0.11));
                    Color grayColor = Color.FromArgb(grayScale, grayScale, grayScale);
                    grayscaleImage.SetPixel(x, y, grayColor);
                }
            }
            return grayscaleImage;
        }
        private static Bitmap ApplySobelEdgeDetection(Bitmap originalImage)
        {
            Bitmap edgeDetectedImage = new Bitmap(originalImage.Width, originalImage.Height);
            Bitmap grayscaleImage = ConvertToGrayscale(originalImage);

            int[,] gx = { { -1, 0, 1 }, { -2, 0, 2 }, { -1, 0, 1 } };
            int[,] gy = { { -1, -2, -1 }, { 0, 0, 0 }, { 1, 2, 1 } };

            for (int x = 1; x < grayscaleImage.Width - 1; x++)
            {
                for (int y = 1; y < grayscaleImage.Height - 1; y++)
                {
                    int pixelGx = 0, pixelGy = 0;

                    for (int i = -1; i <= 1; i++)
                    {
                        for (int j = -1; j <= 1; j++)
                        {
                            pixelGx += grayscaleImage.GetPixel(x + i, y + j).R * gx[i + 1, j + 1];
                            pixelGy += grayscaleImage.GetPixel(x + i, y + j).R * gy[i + 1, j + 1];
                        }
                    }

                    int gradientMagnitude = Math.Min(255, (int)Math.Sqrt(pixelGx * pixelGx + pixelGy * pixelGy));
                    edgeDetectedImage.SetPixel(x, y, Color.FromArgb(gradientMagnitude, gradientMagnitude, gradientMagnitude));
                }
            }

            return edgeDetectedImage;
        }
        private static double[,] GenerateGaussianKernel(int kernelSize, double sigma)
        {
            double[,] kernel = new double[kernelSize, kernelSize];
            int kernelRadius = kernelSize / 2;
            double sum = 0;

            for (int x = -kernelRadius; x <= kernelRadius; x++)
            {
                for (int y = -kernelRadius; y <= kernelRadius; y++)
                {
                    kernel[x + kernelRadius, y + kernelRadius] = Math.Exp(-(x * x + y * y) / (2 * sigma * sigma)) / (2 * Math.PI * sigma * sigma);
                    sum += kernel[x + kernelRadius, y + kernelRadius];
                }
            }

            // Normalize the kernel
            for (int x = 0; x < kernelSize; x++)
            {
                for (int y = 0; y < kernelSize; y++)
                {
                    kernel[x, y] /= sum;
                }
            }

            return kernel;
        }
        //Improved image drawing placement.
        static void DrawProcessedImageInPaint(Bitmap processedImage, int startX, int startY)
        {
            InputSimulator sim = new InputSimulator();

            for (int y = 0; y < processedImage.Height; y++)
            {
                for (int x = 0; x < processedImage.Width; x++)
                {
                    Color pixelColor = processedImage.GetPixel(x, y);
                    if (pixelColor.R < 250 || pixelColor.G < 250 || pixelColor.B < 250)
                    {
                        SetCursorPos(startX + x, startY + y);
                        sim.Mouse.LeftButtonDown();
                        sim.Mouse.LeftButtonUp();
                    }
                }
            }
        }
        static void SavePaintImage(IntPtr paintHandle, IntPtr mainHandle)
        {
            try
            {
                //create file name with timestamp
                string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
                string filename = $"PaintDrawing_{timestamp}.png";
                string fullPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), filename);
                //make sure paint is selected
                SetForegroundWindow(paintHandle);
                Thread.Sleep(500);
                //Ctrl + S to save
                InputSimulator sim = new InputSimulator();
                sim.Keyboard.KeyDown(VirtualKeyCode.CONTROL);
                sim.Keyboard.KeyPress(VirtualKeyCode.VK_S);
                sim.Keyboard.KeyUp(VirtualKeyCode.CONTROL);
                Thread.Sleep(500);
                //put file name as save
                sim.Keyboard.TextEntry(fullPath); // Changed to TextEntry for rapid entry
                Thread.Sleep(500);
                sim.Keyboard.KeyPress(VirtualKeyCode.RETURN);
                Thread.Sleep(500);

                Console.WriteLine($"Image saved as: {fullPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving image: {ex.Message}");
            }
        }
        static void ClosePaint(IntPtr paintHandle)
        {
            try
            {
                if (paintHandle != IntPtr.Zero)
                {
                    SendMessage(paintHandle, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
                    Thread.Sleep(500); // Give time for the window to close
                    if (IsWindow(paintHandle))
                    {
                        Process[] procs = Process.GetProcessesByName("mspaint");
                        foreach (Process proc in procs)
                        {
                            proc.Kill();
                        }
                        Console.WriteLine("Paint process forcefully terminated.");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error closing Paint: {ex.Message}");
            }
        }
        private static WINDOWPLACEMENT GetPlacement(IntPtr hwnd)
        {
            WINDOWPLACEMENT placement = new WINDOWPLACEMENT();
            placement.length = Marshal.SizeOf(placement);
            GetWindowPlacement(hwnd, ref placement);
            return placement;
        }
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetWindowPlacement(IntPtr hWnd, ref WINDOWPLACEMENT lpwndpl);
        [Serializable]
        [StructLayout(LayoutKind.Sequential)]
        internal struct WINDOWPLACEMENT
        {
            public int length;
            public int flags;
            public ShowWindowCommands showCmd;
            public System.Drawing.Point ptMinPosition;
            public System.Drawing.Point ptMaxPosition;
            public System.Drawing.Rectangle rcNormalPosition;
        }
        internal enum ShowWindowCommands : int
        {
            Hide = 0,
            Normal = 1,
            Minimized = 2,
            Maximized = 3,
        }
    }
}