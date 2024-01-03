#include <DHT.h>

#define DHTPIN 21      // Pin where the DHT sensor is connected
#define DHTTYPE DHT11   // Type of the DHT sensor

DHT dht(DHTPIN, DHTTYPE);

void setup() {
  Serial.begin(115200);
  Serial.println("DHT sensor reading using ESP32");

  dht.begin();
}

void loop() {
  delay(2000);  // Delay between sensor readings

  float humidity = dht.readHumidity();
  float temperature = dht.readTemperature();

  if (isnan(humidity) || isnan(temperature)) {
    Serial.println("Failed to read from DHT sensor!");
    return;
  }

  Serial.print("Humidity: ");
  Serial.print(humidity);
  Serial.print("%\t");

  Serial.print("Temperature: ");
  Serial.print(temperature);
  Serial.println("°C");
}
