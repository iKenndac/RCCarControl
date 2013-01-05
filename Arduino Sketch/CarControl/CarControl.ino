/*
  CarControl

  This Arduino sketch provides an interface to a model vehicle with the following
  electronic interfaces:

  - Throttle servo connected to pin 12.
  - Steering servo connected to pin 13.
  - Four ultrasonic sensors connected to pins 2-5.
*/

// ------ Ping ------

/*
  The Ping code uses the NewPing library to listen to the four sensors in
  an event-driven manner. The sensors are fired at 33ms intervals, and the
  values reported over the serial interface when they've all been pinged.
*/

#include <NewPing.h>

static const int kSonarSensorCount = 4; // Number of sensors.
static const int kSonarMaxDistance = 300; // Maximum distance (in cm) to ping.
static const int kSonarPingInterval = 33; // Milliseconds between sensor pings (29ms is about the min to avoid cross-sensor echo).

unsigned long pingTimer[kSonarSensorCount]; // Holds the times when the next ping should happen for each sensor.
unsigned int cm[kSonarSensorCount];         // Where the ping distances are stored.
uint8_t currentSensor = 0;                  // Keeps track of which sensor is active.

NewPing sonar[kSonarSensorCount] = { // Sensor object array.
  NewPing(2, 2, kSonarMaxDistance),  // Each sensor's trigger pin, echo pin, and max distance to ping.
  NewPing(3, 3, kSonarMaxDistance),
  NewPing(4, 4, kSonarMaxDistance),
  NewPing(5, 5, kSonarMaxDistance)
};

// ------- Servos ------

#include <Servo.h>

static const int kSteeringServoPin = 13;
static const int kThrottleServoPin = 12;

Servo steeringServo; 
Servo throttleServo;

// Protocol details (two header bytes, 2 value bytes, checksum)

const int kProtocolHeaderFirstByte = 0xBA;
const int kProtocolHeaderSecondByte = 0xBE;

const int kProtocolHeaderLength = 2;
const int kProtocolBodyLength = 2;
const int kProtocolChecksumLength = 1;

// Buffers and state

bool appearToHaveValidMessage;
byte receivedMessage[2];

// ------- Code --------


void setup() {
  Serial.begin(9600);
  pingTimer[0] = millis() + 75;           // First ping starts at 75ms, gives time for the Arduino to chill before starting.
  for (uint8_t i = 1; i < kSonarSensorCount; i++) // Set the starting time for each sensor.
    pingTimer[i] = pingTimer[i - 1] + kSonarPingInterval;
    
  steeringServo.attach(kSteeringServoPin);
  throttleServo.attach(kThrottleServoPin);
  
  steeringServo.write(90);
  throttleServo.write(90);
}

void loop() {
  for (uint8_t i = 0; i < kSonarSensorCount; i++) { // Loop through all the sensors.
    if (millis() >= pingTimer[i]) {         // Is it this sensor's time to ping?
      pingTimer[i] += kSonarPingInterval * kSonarSensorCount;  // Set next time this sensor will be pinged.
      if (i == 0 && currentSensor == kSonarSensorCount - 1)
        oneSensorCycle(); // Sensor ping cycle complete, do something with the results.
      sonar[currentSensor].timer_stop();          // Make sure previous timer is canceled before starting a new ping (insurance).
      currentSensor = i;                          // Sensor being accessed.
      cm[currentSensor] = 0;                      // Make distance zero in case there's no ping echo for this sensor.
      sonar[currentSensor].ping_timer(echoCheck); // Do the ping (processing continues, interrupt will call echoCheck to look for echo).
    }
  }
  // Other code that *DOESN'T* analyze ping results can go here.
  
  int availableBytes = Serial.available();
  
  if (!appearToHaveValidMessage) {
    
    // If we haven't found a header yet, look for one.
    if (availableBytes >= kProtocolHeaderLength) {
      
      // Read then peek in case we're only one byte away from the header.
      byte firstByte = Serial.read();
      byte secondByte = Serial.peek();
      
      if (firstByte == kProtocolHeaderFirstByte &&
          secondByte == kProtocolHeaderSecondByte) {
            
          // We have a valid header. We might have a valid message!
          appearToHaveValidMessage = true;
          
          // Read the second header byte out of the buffer and refresh the buffer count.
          Serial.read();
          availableBytes = Serial.available();
      }
    }
  }
  
  if ((availableBytes >= (kProtocolBodyLength + kProtocolChecksumLength)) && appearToHaveValidMessage) {
     
    // Read in the body, calculating the checksum as we go.
    byte calculatedChecksum = 0;
    
    for (int i = 0; i < kProtocolBodyLength; i++) {
      receivedMessage[i] = Serial.read();
      calculatedChecksum ^= receivedMessage[i];
    }
    
    byte receivedChecksum = Serial.read();
    
    if (receivedChecksum == calculatedChecksum) {
      // Hooray! Push the values to the output pins.
      unsigned int steeringValue = receivedMessage[0];
      unsigned int throttleValue = receivedMessage[1];
      
      if (steeringValue <= 180 && throttleValue <= 180) {
        steeringServo.write(receivedMessage[0]);
        throttleServo.write(receivedMessage[1]);
      
        Serial.println("SERVO: OK");
      } else {
        Serial.println("SERVO: FAIL - VALUE(S) OUT OF BOUNDS [0-180]");
      }
    } else {
      Serial.println("SERVO: FAIL - INVALID CHECKSUM");
    }
    
    appearToHaveValidMessage = false;
  }
  
}

void echoCheck() { // If ping received, set the sensor distance to array.
  if (sonar[currentSensor].check_timer())
    cm[currentSensor] = sonar[currentSensor].ping_result / US_ROUNDTRIP_CM;
}

void oneSensorCycle() { // Sensor ping cycle complete, do something with the results.
  // The following code would be replaced with your code that does something with the ping results.
  
  Serial.print("DISTANCE: ");
  for (uint8_t i = 0; i < kSonarSensorCount; i++) {
    Serial.print(cm[i]);
    if (i != kSonarSensorCount - 1) Serial.print(",");
  }
  Serial.println();
}
