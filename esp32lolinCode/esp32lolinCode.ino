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
char movementLightStatus = 'a'; //auto, high, low


void setup() {
  Serial.println("starting");
  dht.begin();
  pinMode(MOVEMENT_SENSOR_PIN, INPUT);
  pinMode(LED_PIN, OUTPUT);


  Serial.println("setuped succesfully");
}

void loop() {
  readDht11();

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
    bool moved = analogRead(MOVEMENT_SENSOR_PIN)>3500;
    if(moved){
      digitalWrite(LED_PIN, HIGH);
    }
    else{
      digitalWrite(LED_PIN, LOW);
    }
  }
}