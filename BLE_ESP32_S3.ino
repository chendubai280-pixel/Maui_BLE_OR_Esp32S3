#include <BLEDevice.h>
#include <BLEServer.h>
#include <BLEUtils.h>
#include <BLE2902.h>
#include <string> // 包含 std::string 头文件

// 定义LED引脚 (请根据您的实际接线修改，例如：GPIO 2 或 4)
const int LED_PIN = 4; 

// 定义BLE服务的UUID
#define SERVICE_UUID           "4fafc201-1fb5-459e-8fcc-c4c9c331914b"
// 定义TX特征的UUID (ESP32 -> 手机APP)
#define CHARACTERISTIC_UUID_TX "beb5483e-36e1-4688-b7f5-ea07361b26a8"
// 定义RX特征的UUID (手机APP -> ESP32)
#define CHARACTERISTIC_UUID_RX "ae5b0185-3e28-4856-91e0-e14b533a0129"

BLEServer *pServer = NULL;
BLECharacteristic *pTxCharacteristic = NULL;
bool deviceConnected = false;

// 全局标志：跟踪是否收到 MAUI 发来的 "OK" 确认 (用于控制心跳)
bool ackReceived = false; 

// =================================================================
// 1. 服务器事件回调类 (处理连接和断开)
// =================================================================
class MyServerCallbacks: public BLEServerCallbacks {
    void onConnect(BLEServer* pServer) {
      deviceConnected = true;
      ackReceived = false; // 每次连接时重置 ACK 状态
      Serial.println("手机客户端已连接!");
    };

    void onDisconnect(BLEServer* pServer) {
      deviceConnected = false;
      ackReceived = false; // 断开时重置 ACK 状态
      Serial.println("手机客户端已断开。");
      // 重新开始广播
      BLEDevice::startAdvertising();
      Serial.println("重新开始BLE广播...");
    }
};

// =================================================================
// 2. RX 特征事件回调类 (处理接收数据) - onWrite
// =================================================================
class MyRxCallbacks: public BLECharacteristicCallbacks {
    void onWrite(BLECharacteristic *pCharacteristic) {
      // 使用 getData() 和 getLength() 获取数据
      uint8_t* data = pCharacteristic->getData();
      size_t len = pCharacteristic->getLength();

      if (len > 0) {
        // 将接收到的字节数据转换为 C++ 标准字符串 (std::string)
        // 确保字符串只包含有效数据，不包含额外的 null 终止符
        std::string receivedString((char*)data, len);

        Serial.print("收到手机数据: ");
        Serial.println(receivedString.c_str());

        // *** 步骤 1: 检查是否为 ACK 消息 ***
        if (receivedString == "OK") {
            ackReceived = true; // 收到 ACK，设置标志
            Serial.println(">>> 收到 MAUI 的 'OK' 确认 <<<");
            // 如果是 ACK，则直接返回，不执行 LED 控制
            return; 
        }
        
        // *** 步骤 2: LED 控制逻辑 ***
        if (receivedString == "on") {
          digitalWrite(LED_PIN, HIGH); // 点亮LED
          Serial.println(">>> 收到 'on' 指令，LED 已点亮 <<<");
        } 
        else if (receivedString == "off") {
          digitalWrite(LED_PIN, LOW); // 关闭LED
          Serial.println(">>> 收到 'off' 指令，LED 已关闭 <<<");
        }
        else {
          Serial.print(">>> 收到未知指令: ");
          Serial.println(receivedString.c_str());
        }
      }
    }
};

// =================================================================
// 3. TX 特征事件回调类 (处理订阅事件)
// =================================================================
class MyTxCallbacks: public BLECharacteristicCallbacks {
    void onSubscribe(BLECharacteristic *pCharacteristic, uint16_t cccdValue) {
        if (cccdValue == 1) {
            Serial.println("MAUI 客户端已成功订阅 TX 特征 (Notify)。");
            
            // 订阅成功后，重置 ACK 状态，准备开始心跳
            ackReceived = false; 

            // 在客户端订阅成功后，立即发送初始消息
            pCharacteristic->setValue("Subscription Confirmed! Ready to chat.");
            pCharacteristic->notify();
            Serial.println("已发送 'Subscription Confirmed!' 初始消息。");
        } else {
            Serial.println("MAUI 客户端已取消订阅 TX 特征。");
        }
    }
};


void setup() {
  Serial.begin(115200);
  Serial.println("ESP32-S3 BLE Chat Server 启动中...");

  // 硬件初始化
  pinMode(LED_PIN, OUTPUT);
  digitalWrite(LED_PIN, LOW); // 默认关闭LED

  // 1. 初始化BLE设备
  BLEDevice::init("ESP32S3_BLE_Chat");

  // 2. 创建BLE服务器
  pServer = BLEDevice::createServer();
  pServer->setCallbacks(new MyServerCallbacks());

  // 3. 创建服务
  BLEService *pService = pServer->createService(SERVICE_UUID);

  // 4. 创建TX特征 (发送给手机)
  pTxCharacteristic = pService->createCharacteristic(
                        CHARACTERISTIC_UUID_TX,
                        BLECharacteristic::PROPERTY_READ   |
                        BLECharacteristic::PROPERTY_NOTIFY
                      );
  pTxCharacteristic->addDescriptor(new BLE2902());
  pTxCharacteristic->setCallbacks(new MyTxCallbacks()); 

  // 5. 创建RX特征 (接收手机数据)
  BLECharacteristic *pRxCharacteristic = pService->createCharacteristic(
                           CHARACTERISTIC_UUID_RX,
                           BLECharacteristic::PROPERTY_WRITE
                         );
  pRxCharacteristic->setCallbacks(new MyRxCallbacks());

  // 6. 启动服务
  pService->start();

  // 7. 开始广播
  BLEAdvertising *pAdvertising = BLEDevice::getAdvertising();
  pAdvertising->addServiceUUID(SERVICE_UUID);
  pAdvertising->setScanResponse(true);
  pAdvertising->setMinPreferred(0x06);
  pAdvertising->setMinPreferred(0x12);
  BLEDevice::startAdvertising();
  Serial.println("BLE广播已启动，等待手机连接...");
}

void loop() {
  // 示例：每隔10秒发送一次时间戳（心跳包）
  if (deviceConnected) {
    // 只有在未收到 ACK 时才发送心跳 (保持您的原有逻辑)
    if (!ackReceived) { 
        static unsigned long lastSendTime = 0;
        if (millis() - lastSendTime > 10000) {
            String timestamp = "Heartbeat: " + String(millis());
            pTxCharacteristic->setValue(timestamp.c_str());
            pTxCharacteristic->notify();
            Serial.println("发送心跳包: " + timestamp);
            lastSendTime = millis();
        }
    } 
  }
  
  delay(100);
}
