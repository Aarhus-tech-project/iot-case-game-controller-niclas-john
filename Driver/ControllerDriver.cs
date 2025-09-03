using Nefarius.ViGEm.Client;
using Nefarius.ViGEm.Client.Targets;
using Nefarius.ViGEm.Client.Targets.Xbox360;
using System;
using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;

class Program
{
    static SerialPort _serial;
    static ViGEmClient _client;
    static IXbox360Controller _xbox;
    static volatile bool _running = true;

    static BlockingCollection<string> _sendQueue = new BlockingCollection<string>();

    const string ComPort = "COM13"; 
    const int Baud = 115200;

    static async Task Main()
    {
        // --- 1. Initialize serial port ---
        _serial = new SerialPort(ComPort, Baud)
        {
            NewLine = "\n",
            ReadTimeout = 50
        };

        try
        {
            _serial.Open();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to open serial port: {ex.Message}");
            return;
        }

        // --- 2. Initialize ViGEm controller ---
        _client = new ViGEmClient();
        _xbox = _client.CreateXbox360Controller();
        _xbox.FeedbackReceived += Xbox_FeedbackReceived;
        _xbox.Connect();

        // --- 3. Start async read & write loops ---
        var readTask = Task.Run(() => ReadLoop());
        var writeTask = Task.Run(() => WriteLoop());

        Console.WriteLine("Controller running. Press Enter to quit.");
        Console.ReadLine();

        // --- 4. Stop tasks ---
        _running = false;
        _sendQueue.CompleteAdding();

        await Task.WhenAll(readTask, writeTask);

        // --- 5. Cleanup ---
        _serial.Close();
        _xbox.Disconnect();
    }

    // --- Asynchronous reading ---
    static void ReadLoop()
    {
        while (_running)
        {
            try
            {
                string line = _serial.ReadLine()?.Trim();
                if (!string.IsNullOrEmpty(line))
                {
                    ProcessFrame(line);
                }
            }
            catch (TimeoutException) { } // expected
            catch (Exception ex) { Console.WriteLine($"Serial read error: {ex.Message}"); }
        }
    }

    // --- Asynchronous writing ---
    static void WriteLoop()
    {
        foreach (var msg in _sendQueue.GetConsumingEnumerable())
        {
            try
            {
                if (_serial.IsOpen)
                    _serial.WriteLine(msg);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Serial write error: {ex.Message}");
            }
        }
    }

    static void ProcessFrame(string line)
    {
        var parts = line.Split(',');
        if (parts.Length < 14) return;

        // Map buttons
        _xbox.SetButtonState(Xbox360Button.A, parts[0] == "1");
        _xbox.SetButtonState(Xbox360Button.B, parts[1] == "1");
        _xbox.SetButtonState(Xbox360Button.X, parts[2] == "1");
        _xbox.SetButtonState(Xbox360Button.Y, parts[3] == "1");
        _xbox.SetButtonState(Xbox360Button.Up, parts[4] == "1");
        _xbox.SetButtonState(Xbox360Button.Down, parts[5] == "1");
        _xbox.SetButtonState(Xbox360Button.Left, parts[6] == "1");
        _xbox.SetButtonState(Xbox360Button.Right, parts[7] == "1");
        _xbox.SetButtonState(Xbox360Button.LeftThumb, parts[8] == "1");
        _xbox.SetButtonState(Xbox360Button.RightThumb, parts[9] == "1");

        // Analog sticks
        int stickX = (int)((float.Parse(parts[10]) / 4095.0f) * 65535 - 32768);
        int stickY = (int)((float.Parse(parts[11]) / 4095.0f) * -65535 + 32768);
        int stickX2 = (int)((float.Parse(parts[12]) / 4095.0f) * 65535 - 32768);
        int stickY2 = (int)((float.Parse(parts[13]) / 4095.0f) * -65535 + 32768);

        stickX = Math.Clamp(stickX, -32768, 32767);
        stickY = Math.Clamp(stickY, -32768, 32767);
        stickX2 = Math.Clamp(stickX2, -32768, 32767);
        stickY2 = Math.Clamp(stickY2, -32768, 32767);

        _xbox.SetAxisValue(Xbox360Axis.LeftThumbX, (short)stickX);
        _xbox.SetAxisValue(Xbox360Axis.LeftThumbY, (short)stickY);
        _xbox.SetAxisValue(Xbox360Axis.RightThumbX, (short)stickX2);
        _xbox.SetAxisValue(Xbox360Axis.RightThumbY, (short)stickY2);
    }

    private static void Xbox_FeedbackReceived(object sender, Xbox360FeedbackReceivedEventArgs e)
    {
        int intensity = Math.Max(e.LargeMotor, e.SmallMotor);
        intensity = Math.Clamp(intensity / 2, 0, 127);
        Console.WriteLine($"Rumble intensity: {intensity}");
        // Queue the rumble message instead of writing directly
        _sendQueue.Add($"Rumble,{intensity}");
    }
}
