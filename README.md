Considering that the majority of readers of this article might be ESP32 developers who are not interested in how the mobile app is developed, I will first post a ready-to-use mobile app (this app requires an Android 15 or higher system to run; there's no way around it, as Google's technology updates too rapidly). ESP32 developers can directly download and install the app, then copy the flashing code for the ESP32-S3 and flash it onto your ESP32 circuit board. You can start using it right away without needing to understand the details of app development.
Download and merge these three files, and you will be able to obtain the app that controls the ESP32.
<img width="371" height="180" alt="image" src="https://github.com/user-attachments/assets/a51d40d5-7e8f-423d-8a1e-c139801fb693" />

BLE_ESP32_S3.ino It is for programming ESP32S3. Simply burn the program onto the circuit board and it can be used.
The most important thing is the three UUIDs. They must not be modified. My mobile app has already been paired with these codes. You can modify other functions as needed. After the app successfully connects to the ESP32, it will send a "ok" response to the ESP32. At the same time, the app will enter the lighting page. When the buttons on the page are pressed, "on" will be sent to the ESP32 if they are opened, and "off" will be sent if they are closed. The ESP32 will handle the lighting and turning off based on the received "on/off" signals. You can modify the signals sent by this app to any function you want to drive.

Original link：
https://blog.csdn.net/cjp560/article/details/155581126?spm=1001.2014.3001.5501
