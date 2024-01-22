//dht11
#include <DHT.h>
#define DHTPIN 21     
#define DHTTYPE DHT11   
DHT dht(DHTPIN, DHTTYPE);
float humidity = 0;
float temperature = 0;

//movement light
#define LED_PIN 32
#define MOVEMENT_SENSOR_PIN 12
char movementLightStatus = 'l'; //auto, high, low

//powering
#define POWERSWITCH 23
#define ANALOG_IN_PIN 25
char powerMode = 'a'; //auto(4,5v+ solar), solar, network
float adc_voltage = 0.0;
float in_voltage = 0.0;
float R1 = 30000.0;
float R2 = 7500.0; 
float ref_voltage = 5.0;
int adc_value = 0;

//RGB led strip
#include <Adafruit_NeoPixel.h>
#define PIN_NEO_PIXEL 15  // The ESP32 pin GPIO16 connected to NeoPixel
#define NUM_PIXELS 29     // The number of LEDs (pixels) on NeoPixel LED strip
#define PIXEL_POWERING_PIN 33
Adafruit_NeoPixel NeoPixel(NUM_PIXELS, PIN_NEO_PIXEL, NEO_GRB + NEO_KHZ800);
char ledStripMode = 'a'; // off, color, animation 
int Red = 0;
int Green = 255;
int Blue = 0;
int ledDensity = 1;
#include <math.h> //animation 1
unsigned long lastTime = 0;  
int animationPos[3] = {0,1,2};
int ledColor = 2;
  int colors[20][3] = {
    {255, 0, 0},    // Red
    {0, 255, 0},    // Green
    {0, 0, 255},    // Blue
    {255, 255, 0},  // Yellow
    {255, 0, 255},  // Magenta
    {0, 255, 255},  // Cyan
    {255, 128, 0},  // Orange
    {128, 0, 255},  // Purple
    {0, 255, 128},  // Teal
    {255, 255, 255},  // White
    {128, 128, 128},  // Gray
    {255, 128, 128},  // Light Red
    {128, 255, 128},  // Light Green
    {128, 128, 255},  // Light Blue
    {255, 255, 128},  // Light Yellow
    {255, 128, 255},  // Light Magenta
    {128, 255, 255},  // Light Cyan
    {255, 192, 128},  // Light Orange
    {192, 128, 255},  // Light Purple
    {128, 255, 192}   // Light Teal
  };

  //communication 
#include <BluetoothSerial.h>
BluetoothSerial SerialBT;

void setup() {
  Serial.begin(115200);
  Serial.println("starting");
  dht.begin();
  pinMode(MOVEMENT_SENSOR_PIN, INPUT);
  pinMode(LED_PIN, OUTPUT);
  pinMode(POWERSWITCH, OUTPUT);
  pinMode(PIXEL_POWERING_PIN, OUTPUT);
  digitalWrite(PIXEL_POWERING_PIN, HIGH);
  NeoPixel.begin();
  randomSeed(analogRead(19));
  SerialBT.begin("ESP32_BTSerial"); // Bluetooth device name
  Serial.println("setuped succesfully");
}

void loop() {
  //message format: (char)powering_(char)outsidelight_(char)stripMode_voltage_temperature_humidity_ledDensity_Red_Green_Blue#
  String sendMessage = String(powerMode) + "_" + String(movementLightStatus) + "_" + String(ledStripMode) + "_" + String(in_voltage) + "_" + String(temperature) + "_" + String(humidity) + "_" + String(ledDensity) + "_" + String(Red) + "_" + String(Green) + "_" + String(Blue) + "#";
SerialBT.print(sendMessage);
SerialBT.print(sendMessage);
SerialBT.print(sendMessage);
  power();
  readDht11();
  movementLight();
  neopixelStrip();
if (SerialBT.available()) {
    String incomingString = SerialBT.readStringUntil('\n');

    //message format: (char)powering_(char)outsidelight_(char)stripMode_ledDensity_Red_Green_Blue#
    // Split the incomingString by #
    int hashIndex = incomingString.indexOf('#');
    if (hashIndex != -1) {
        String firstPart = incomingString.substring(0, hashIndex);
        // Now, you can process the firstPart
        sscanf(firstPart.c_str(), "%c_%c_%c_%d_%d_%d_%d", &powerMode, &movementLightStatus, &ledStripMode, &ledDensity, &Red, &Green, &Blue);
    }
}

  Serial.println("loop succesfully");
}

void power(){
  adc_value = analogRead(ANALOG_IN_PIN);
  adc_voltage  = (adc_value * ref_voltage) / 4096.0*0.78;
  in_voltage = adc_voltage*(R1+R2)/R2;
  if(powerMode == 's'){
    digitalWrite(POWERSWITCH,LOW);
  }
  else if(powerMode == 'n'){
    digitalWrite(POWERSWITCH,HIGH);
  }
  else{
    if(in_voltage< 4.5){
      digitalWrite(POWERSWITCH,HIGH);
    }
    else if(in_voltage > 5.4){
      digitalWrite(POWERSWITCH,LOW);
    }
  }

}

void neopixelStrip(){
  unsigned long currentTime = millis();
  int difference = millis()-lastTime;
  if(difference>100){
      int change = floor(difference/100);
      lastTime+=change*100;
      for(int i = 0; i<change; i++){
        for(int x = 0; x<3; x++){
          animationPos[x]++;
          if(animationPos[x]>=NUM_PIXELS){
            animationPos[x]=0;
            if(x==2){
                ledColor++;
                if(ledColor>=20){
                  ledColor = 0;
                }
            }
          }
        }
      }
  }
  switch (ledStripMode) {
    case 'a':
      NeoPixel.clear();

      for(int i = 0; i<3; i++){
        NeoPixel.setPixelColor(animationPos[i], NeoPixel.Color(colors[ledColor][0], colors[ledColor][1], colors[ledColor][2]));  
      }
      NeoPixel.show();   
      break;
    case 'c':
      NeoPixel.clear();
      for (int pixel = 0; pixel < NUM_PIXELS; pixel+=ledDensity) {        
        NeoPixel.setPixelColor(pixel, NeoPixel.Color(Red, Green, Blue));  
      }
        NeoPixel.show();                                       
      break;
    default: //off
      digitalWrite(PIXEL_POWERING_PIN, LOW);
      break;
 }

}

void readDht11(){
  humidity = dht.readHumidity();
  temperature = dht.readTemperature();
  if (isnan(humidity) || isnan(temperature)) {
    Serial.println("Failed to read from DHT sensor!");
    humidity = 0;
    temperature = 0;
  }
}

void movementLight(){
  if(movementLightStatus == 'l'){
    digitalWrite(LED_PIN, LOW);
  }
  else if(movementLightStatus == 'h'){
    digitalWrite(LED_PIN, HIGH);
  }
  else{
    bool moved = analogRead(MOVEMENT_SENSOR_PIN)>1000;
    if(moved){
      digitalWrite(LED_PIN, HIGH);
    }
    else{
      digitalWrite(LED_PIN, LOW);
    }
  }
}

