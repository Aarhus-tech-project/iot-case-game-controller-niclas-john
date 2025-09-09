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
    static string ComPort = "";  // <-- change to your COM port
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
        var rumbleThread = new Thread(() => RumbleFeedbackLoop(_ds4)) { IsBackground = true };
        rumbleThread.Start();

        // --- Start reading loop ---
        var readThread = new Thread(ReadLoop) { IsBackground = true };
        readThread.Start();

        Console.WriteLine("Controller running. Press Enter to quit.");
        Console.ReadLine();

        _running = false;
        readThread.Join();
        rumbleThread.Join();
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

        // Parse button states
        bool A = parts[0] == "1";
        bool B = parts[1] == "1";
        bool X = parts[2] == "1";
        bool Y = parts[3] == "1";
        bool DU = parts[4] == "1";
        bool DD = parts[5] == "1";
        bool DL = parts[6] == "1";
        bool DR = parts[7] == "1";
        bool leftThumb = parts[8] == "1";
        bool rightThumb = parts[9] == "1";
        bool L1 = parts[10] == "1";
        bool R1 = parts[11] == "1";
        bool L2 = parts[12] == "1";
        bool R2 = parts[13] == "1";
        bool Start = parts[14] == "1";
        bool GameButton = parts[15] == "1";

        // --- Mode switching ---
        if (GameButton)
        {
            // --- Controller mode ---
            // Set face buttons
            _ds4.SetButtonState(DualShock4Button.Cross, A);
            _ds4.SetButtonState(DualShock4Button.Circle, B);
            _ds4.SetButtonState(DualShock4Button.Square, X);
            _ds4.SetButtonState(DualShock4Button.Triangle, Y);

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

            // --- Analog sticks ---
            byte leftX = (byte)Math.Clamp((int)((float.Parse(parts[16]) / 4095f) * 255), 0, 255);
            byte leftY = (byte)Math.Clamp((int)((float.Parse(parts[17]) / 4095f) * 255), 0, 255);
            byte rightX = (byte)Math.Clamp((int)((float.Parse(parts[18]) / 4095f) * 255), 0, 255);
            byte rightY = (byte)Math.Clamp((int)((float.Parse(parts[19]) / 4095f) * 255), 0, 255);

            _ds4.SetAxisValue(DualShock4Axis.LeftThumbX, leftX);
            _ds4.SetAxisValue(DualShock4Axis.LeftThumbY, leftY);
            _ds4.SetAxisValue(DualShock4Axis.RightThumbX, rightX);
            _ds4.SetAxisValue(DualShock4Axis.RightThumbY, rightY);
        }
        else
        {
            // --- Mouse mode ---
            // Only allow mouse movement and left/right click

            // Example: Use right stick for mouse movement
            int mouseMoveX = (int)((float.Parse(parts[18]) - 2048) / 50); // Adjust divisor for sensitivity
            int mouseMoveY = (int)((float.Parse(parts[19]) - 2048) / 50);

            _inputSimulator.Mouse.MoveMouseBy(mouseMoveX, mouseMoveY);

            // Example: Use leftThumb for left click, rightThumb for right click
            if (leftThumb && !prevBtnState)
                _inputSimulator.Mouse.LeftButtonDown();
            else if (!leftThumb && prevBtnState)
                _inputSimulator.Mouse.LeftButtonUp();

            if (rightThumb)
                _inputSimulator.Mouse.RightButtonDown();
            else
                _inputSimulator.Mouse.RightButtonUp();

            prevBtnState = leftThumb;
        }
    }
    static void RumbleFeedbackLoop(IDualShock4Controller ds4)
    {
        while (_running)
        {
            try
            {
                bool timedOut;
                var report = ds4.AwaitRawOutputReport(100, out timedOut).ToArray();
                if (timedOut)
                    continue;

                // DS4 output report: [2]=smallRumble, [3]=largeRumble
                byte smallRumble = report[2];
                byte largeRumble = report[3];
                int intensity = Math.Max(smallRumble, largeRumble);
                intensity = Math.Clamp(intensity, 0, 127);

                _serial.WriteLine($"Rumble,{intensity}");
                Console.WriteLine($"Sent rumble intensity: {intensity} (small={smallRumble}, large={largeRumble})");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send rumble: {ex.GetType().Name}: {ex.Message}");
            }
        }
    }
}