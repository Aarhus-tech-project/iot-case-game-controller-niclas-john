[![Review Assignment Due Date](https://classroom.github.com/assets/deadline-readme-button-22041afd0340ce965d47ae6ef1cefeee28c7c493a6346c4f15d667ab976d596c.svg)](https://classroom.github.com/a/BxF6qiQf)

Quick Start Guide

1. Install the ViGEmBus Driver
   Download and install the ViGEmBus driver from:
   https://vigembusdriver.com

2. Connect the Controller via Bluetooth
   Pair your controller with your PC via Bluetooth. Look for name : Game_Controller
   Open Bluetooth settings and scroll down to More Bluetooth Settings.
   At the top, you’ll see the COM ports for connected devices.
   Find the device name you just paired and note the OUTGOING COM port.
   Example:
   COM11 - Incoming - "Device Name"
   COM12 - Outgoing - "Device Name"
   Use the OUTGOING port (e.g., COM12).

3. Start the GameController Driver
   Go to the Driver folder and run the Gamecontroller shortcut file. (• Requires .NET 8 runtime installed on the target machine.)
   Else just open the project on visual studio and run it there.
   When prompted, enter the COM port number (e.g., 12 for COM12).
4. MQTT Connection (Optional)
   If you are connected to the h4prog WiFi network, the controller will connect to MQTT automatically.
   If not, it will continue to work locally.

   This is the fastest way to get started with the controller.
   If you have any issues, check your Bluetooth connection and COM port settings.
