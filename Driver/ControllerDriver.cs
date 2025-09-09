using Nefarius.ViGEm.Client;
using Nefarius.ViGEm.Client.Targets;
using Nefarius.ViGEm.Client.Targets.DualShock4;
using System;
using System.IO.Ports;
using System.Threading;
using WindowsInput;

class Program
{
    static SerialPort _serial;
    static ViGEmClient _client;
    static IDualShock4Controller _ds4; // Use DS4 controller instead of Xbox360
    static volatile bool _running = true;
    static string ComPort = "COM11";  // <-- change to your COM port
    const int Baud = 115200;
    static InputSimulator _inputSimulator = new InputSimulator();
    static bool prevBtnState = false; // Track previous button state
    static void Main()
    {
        _serial = new SerialPort(ComPort, Baud)
        {
            NewLine = "\n",
            ReadTimeout = 50
        };

        try
        {
            _serial.Open();
            Console.WriteLine($"Serial port {_serial.PortName} opened.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to open serial port: {ex.Message}");
            return;
        }

        // --- Initialize ViGEm DS4 controller ---
        _client = new ViGEmClient();
        _ds4 = _client.CreateDualShock4Controller();
        _ds4.Connect();
        Console.WriteLine("DualShock 4 controller connected.");

        // --- Start rumble feedback loop ---
        //var rumbleThread = new Thread(() => RumbleFeedbackLoop(_ds4)) { IsBackground = true };
        //rumbleThread.Start();

        // --- Start reading loop ---
        var readThread = new Thread(ReadLoop) { IsBackground = true };
        readThread.Start();

        Console.WriteLine("Controller running. Press Enter to quit.");
        Console.ReadLine();

        _running = false;
        readThread.Join();
        //rumbleThread.Join();
        _serial.Close();
        _ds4.Disconnect();
    }

    static void ReadLoop()
    {
        while (_running)
        {
            try
            {
                string line = _serial.ReadLine()?.Trim();
                Console.WriteLine($"Received: {line}");
                if (!string.IsNullOrEmpty(line))
                    ProcessFrame(line);
            }
            catch (TimeoutException) { }
            catch (Exception ex) { Console.WriteLine($"Serial read error: {ex.Message}"); }
        }
    }

    static void ProcessFrame(string line)
    {
        var parts = line.Split(',');
        if (parts.Length < 16) return; // Adjust if you have more fields

        bool Cross = parts[0] == "1"; // X
        bool Circle = parts[1] == "1"; // O
        bool Square = parts[2] == "1"; // Firkant
        bool Triangle = parts[3] == "1"; // Trekant
        bool DU = parts[4] == "1";
        bool DD = parts[5] == "1";
        bool DL = parts[6] == "1";
        bool DR = parts[7] == "1";
        bool leftThumb = parts[8] == "1";
        bool rightThumb = parts[9] == "1";
        byte leftX = (byte)Math.Clamp((int)((float.Parse(parts[10]) / 4095f) * 255), 0, 255);
        byte leftY = (byte)Math.Clamp((int)((float.Parse(parts[11]) / 4095f) * 255), 0, 255);
        byte rightX = (byte)Math.Clamp((int)((float.Parse(parts[12]) / 4095f) * 255), 0, 255);
        byte rightY = (byte)Math.Clamp((int)((float.Parse(parts[13]) / 4095f) * 255), 0, 255);
        bool GameButton = parts[14] == "1";
        bool Start = parts[15] == "1";
        bool L1 = parts[16] == "1";
        bool L2 = parts[17] == "1";
        bool R1 = parts[18] == "1";
        bool R2 = parts[19] == "1";


        //---Mode switching-- -
        if (GameButton)
        {
            // --- Controller mode ---
            // Set face buttons
            _ds4.SetButtonState(DualShock4Button.Cross, Cross);
            _ds4.SetButtonState(DualShock4Button.Circle, Circle);
            _ds4.SetButtonState(DualShock4Button.Square, Square);
            _ds4.SetButtonState(DualShock4Button.Triangle, Triangle);

            // Set thumb buttons
            _ds4.SetButtonState(DualShock4Button.ThumbLeft, leftThumb);
            _ds4.SetButtonState(DualShock4Button.ThumbRight, rightThumb);

            // Set shoulder and trigger buttons
            _ds4.SetButtonState(DualShock4Button.ShoulderLeft, L1);
            _ds4.SetButtonState(DualShock4Button.ShoulderRight, R1);
            _ds4.SetButtonState(DualShock4Button.TriggerLeft, L2);
            _ds4.SetButtonState(DualShock4Button.TriggerRight, R2);

            _ds4.SetButtonState(DualShock4Button.Options, Start);

            // D-pad direction for DS4
            DualShock4DPadDirection dpadDirection = DualShock4DPadDirection.None;
            if (DU)
            {
                if (DL)
                    dpadDirection = DualShock4DPadDirection.Northwest;
                else if (DR)
                    dpadDirection = DualShock4DPadDirection.Northeast;
                else
                    dpadDirection = DualShock4DPadDirection.North;
            }
            else if (DD)
            {
                if (DL)
                    dpadDirection = DualShock4DPadDirection.Southwest;
                else if (DR)
                    dpadDirection = DualShock4DPadDirection.Southeast;
                else
                    dpadDirection = DualShock4DPadDirection.South;
            }
            else if (DL)
                dpadDirection = DualShock4DPadDirection.West;
            else if (DR)
                dpadDirection = DualShock4DPadDirection.East;

            _ds4.SetDPadDirection(dpadDirection);
            _ds4.SetAxisValue(DualShock4Axis.LeftThumbX, leftX);
            _ds4.SetAxisValue(DualShock4Axis.LeftThumbY, leftY);
            _ds4.SetAxisValue(DualShock4Axis.RightThumbX, rightX);
            _ds4.SetAxisValue(DualShock4Axis.RightThumbY, rightY);
        }


        // Calibration offsets (based on your reading)


        else
        {
            const int centerX = 2255;
            const int centerY = 1800;
            const int deadZone = 250;


            double sensitivity = 0.01;
            int rawX = int.Parse(parts[10]);
            int rawY = int.Parse(parts[11]);

            int deltaX = rawX - centerX;
            int deltaY = rawY - centerY;

            if (Math.Abs(deltaX) > deadZone || Math.Abs(deltaY) > deadZone)
            {
                int moveX = (int)(deltaX * sensitivity);
                int moveY = (int)(deltaY * sensitivity);

                _inputSimulator.Mouse.MoveMouseBy(moveX, moveY);
            }
        }
    }


}


//    static void RumbleFeedbackLoop(IDualShock4Controller ds4)
//    {
//        while (_running)
//        {
//            try
//            {
//                bool timedOut;
//                var report = ds4.AwaitRawOutputReport(100, out timedOut).ToArray();
//                if (timedOut)
//                    continue;

//                // DS4 output report: [2]=smallRumble, [3]=largeRumble
//                byte smallRumble = report[2];
//                byte largeRumble = report[3];
//                int intensity = Math.Max(smallRumble, largeRumble);
//                intensity = Math.Clamp(intensity, 0, 127);

//                _serial.WriteLine($"Rumble,{intensity}");
//                Console.WriteLine($"Sent rumble intensity: {intensity} (small={smallRumble}, large={largeRumble})");
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"Failed to send rumble: {ex.GetType().Name}: {ex.Message}");
//            }
//        }
//    }
//}