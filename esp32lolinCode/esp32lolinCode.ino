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



void setup() {
  Serial.begin(115200);
  Serial.println("starting");
  dht.begin();
  pinMode(MOVEMENT_SENSOR_PIN, INPUT);
  pinMode(LED_PIN, OUTPUT);
  pinMode(POWERSWITCH, OUTPUT);


  Serial.println("setuped succesfully");
}

void loop() {
  power();
  readDht11();
  movementLight();
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