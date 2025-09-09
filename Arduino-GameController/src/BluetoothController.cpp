#include "BluetoothSerial.h"
#include <Wire.h>
#include "Haptic_Driver.h"
#include <math.h>

Haptic_Driver hapDrive;
BluetoothSerial SerialBT;

// Gamepad pins
const int PIN_X = 15, PIN_O = 2, PIN_Firkant = 4, PIN_Trekant = 0;
const int PIN_DU = 5, PIN_DD = 16, PIN_DL = 18, PIN_DR = 17;
const int JOY_X = 36, JOY_Y = 39;
const int JOY_X2 = 34, JOY_Y2 = 35;
const int joyStickBtnLeft = 19;
const int joyStickBtnRight = 23;

int readButton(int pin) { return digitalRead(pin) == LOW ? 1 : 0; }

void rotateJoystick(int rawX, int rawY, float angle, int &outX, int &outY)
{
    int x = rawX - 2048;
    int y = rawY - 2048;
    float xr = x * cos(angle) - y * sin(angle);
    float yr = x * sin(angle) + y * cos(angle);
    outX = constrain((int)xr + 2048, 0, 4095);
    outY = constrain((int)yr + 2048, 0, 4095);
}

void setup()
{
    Serial.begin(115200);      // COM11
    SerialBT.begin("Nillers"); // COM12
    pinMode(PIN_X, INPUT_PULLUP);
    pinMode(PIN_O, INPUT_PULLUP);
    pinMode(PIN_Firkant, INPUT_PULLUP);
    pinMode(PIN_Trekant, INPUT_PULLUP);
    pinMode(PIN_DU, INPUT_PULLUP);
    pinMode(PIN_DD, INPUT_PULLUP);
    pinMode(PIN_DL, INPUT_PULLUP);
    pinMode(PIN_DR, INPUT_PULLUP);
    pinMode(joyStickBtnLeft, INPUT_PULLUP);
    pinMode(joyStickBtnRight, INPUT_PULLUP);
    Wire.begin();
    hapDrive.begin();
    hapDrive.defaultMotor();
    hapDrive.enableFreqTrack(false);
    hapDrive.setOperationMode(DRO_MODE);
    Serial.println("DA7280 ready.");
    hapDrive.setVibrate(10);
}

void loop()
{
    // Read joysticks
    int lx = analogRead(JOY_X);
    int ly = analogRead(JOY_Y);
    int rx = analogRead(JOY_X2);
    int ry = analogRead(JOY_Y2);

    int rotLX, rotLY, rotRX, rotRY;
    rotateJoystick(lx, ly, M_PI / 2, rotLX, rotLY);
    rotateJoystick(rx, ry, M_PI / 2, rotRX, rotRY);

    // Send data over COM12 (Bluetooth Serial)
    String data =
        String(readButton(PIN_X)) + "," + String(readButton(PIN_O)) + "," +
        String(readButton(PIN_Firkant)) + "," + String(readButton(PIN_Trekant)) + "," +
        String(readButton(PIN_DU)) + "," + String(readButton(PIN_DD)) + "," +
        String(readButton(PIN_DL)) + "," + String(readButton(PIN_DR)) + "," +
        String(readButton(joyStickBtnLeft)) + "," + String(readButton(joyStickBtnRight)) + "," +
        String(rotLX) + "," + String(rotLY) + "," +
        String(rotRX) + "," + String(rotRY);

    SerialBT.println(data);
    // Read rumble commands from PC on COM
    if (SerialBT.available())
    {
        String cmd = SerialBT.readStringUntil('\n');
        cmd.trim();

        if (cmd.startsWith("Rumble,"))
        {
            int intensity = cmd.substring(7).toInt();
            hapDrive.setVibrate(intensity);
            Serial.println("Rumble set to: " + String(intensity));
        }
    }

    delay(15);
}
