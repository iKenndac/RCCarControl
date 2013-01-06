/*
  CarControl

  This Arduino sketch provides an interface to a model vehicle with the following
  electronic interfaces:

  - Throttle servo connected to pin 6.
  - Steering servo connected to pin 7.
  - Four ultrasonic sensors connected to pins 2-5.
  - SPI Accelerometer (ADXL345) connected to pins 10-13. 
*/

// ------ Accelerometer ------

#include <SPI.h>

//Assign the Chip Select signal to pin 10.
int CS=10;

//This is a list of some of the registers available on the ADXL345.
//To learn more about these and the rest of the registers on the ADXL345, read the datasheet!
char POWER_CTL = 0x2D;	//Power Control Register
char DATA_FORMAT = 0x31;
char DATAX0 = 0x32;	//X-Axis Data 0
char DATAX1 = 0x33;	//X-Axis Data 1
char DATAY0 = 0x34;	//Y-Axis Data 0
char DATAY1 = 0x35;	//Y-Axis Data 1
char DATAZ0 = 0x36;	//Z-Axis Data 0
char DATAZ1 = 0x37;	//Z-Axis Data 1

//This buffer will hold values read from the ADXL345 registers.
char ADXL_values[10];
//These variables will be used to hold the x,y and z axis accelerometer values.
int x,y,z;
double xg, yg, zg;

// ------ Ping ------

/*
  The Ping code uses the NewPing library to listen to the four sensors in
  an event-driven manner. The sensors are fired at 33ms intervals, and the
  values reported over the serial interface when they've all been pinged.
*/

#include <NewPing.h>

static const int kSonarSensorCount = 4; // Number of sensors.
static const int kSonarMaxDistance = 380; // Maximum distance (in cm) to ping.
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

static const int kSteeringServoPin = 7;
static const int kThrottleServoPin = 6;

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
  
  // -- ADXL
  
  //Initiate an SPI communication instance.
  SPI.begin();
  //Configure the SPI connection for the ADXL345.
  SPI.setDataMode(SPI_MODE3);
  
  //Set up the Chip Select pin to be an output from the Arduino.
  pinMode(CS, OUTPUT);
  //Before communication starts, the Chip Select pin needs to be set high.
  digitalWrite(CS, HIGH);
  
  //Put the ADXL345 into +/- 4G range by writing the value 0x01 to the DATA_FORMAT register.
  writeRegister(DATA_FORMAT, 0x01);
  //Put the ADXL345 into Measurement Mode by writing 0x08 to the POWER_CTL register.
  writeRegister(POWER_CTL, 0x08);  //Measurement mode
  
  // -- Ping
  pingTimer[0] = millis() + 75;           // First ping starts at 75ms, gives time for the Arduino to chill before starting.
  for (uint8_t i = 1; i < kSonarSensorCount; i++) // Set the starting time for each sensor.
    pingTimer[i] = pingTimer[i - 1] + kSonarPingInterval;
    
  // -- Servos
  steeringServo.attach(kSteeringServoPin);
  throttleServo.attach(kThrottleServoPin);
  
  steeringServo.write(90);
  throttleServo.write(90);
}

void loop() {
  
  // -- Ping --
  
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
  
  // -- ADXL --
  
  //Reading 6 bytes of data starting at register DATAX0 will retrieve the x,y and z acceleration values from the ADXL345.
  //The results of the read operation will get stored to the values[] buffer.
  readRegister(DATAX0, 6, ADXL_values);

  //The ADXL345 gives 10-bit acceleration values, but they are stored as bytes (8-bits). To get the full value, two bytes must be combined for each axis.
  //The X value is stored in values[0] and values[1].
  x = ((int)ADXL_values[1]<<8)|(int)ADXL_values[0];
  //The Y value is stored in values[2] and values[3].
  y = ((int)ADXL_values[3]<<8)|(int)ADXL_values[2];
  //The Z value is stored in values[4] and values[5].
  z = ((int)ADXL_values[5]<<8)|(int)ADXL_values[4];
  
  //Convert the accelerometer value to G's. 
  //With 10 bits measuring over a +/-4g range we can find how to convert by using the equation:
  // Gs = Measurement Value * (G-range/(2^10)) or Gs = Measurement Value * (8/1024)
  xg = x * 0.0078125;
  yg = y * 0.0078125;
  zg = z * 0.0078125;
  
  Serial.print("ACCEL: ");
  Serial.print((float)xg,2);
  Serial.print(",");
  Serial.print((float)yg,2);
  Serial.print(",");
  Serial.print((float)zg,2);
  Serial.println();
  
  // -- Servo Reading --
  
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

// -- Ping Functions --

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

// -- ADXL Functions --

//This function will write a value to a register on the ADXL345.
//Parameters:
//  char registerAddress - The register to write a value to
//  char value - The value to be written to the specified register.
void writeRegister(char registerAddress, char value){
  //Set Chip Select pin low to signal the beginning of an SPI packet.
  digitalWrite(CS, LOW);
  //Transfer the register address over SPI.
  SPI.transfer(registerAddress);
  //Transfer the desired register value over SPI.
  SPI.transfer(value);
  //Set the Chip Select pin high to signal the end of an SPI packet.
  digitalWrite(CS, HIGH);
}

//This function will read a certain number of registers starting from a specified address and store their values in a buffer.
//Parameters:
//  char registerAddress - The register addresse to start the read sequence from.
//  int numBytes - The number of registers that should be read.
//  char * values - A pointer to a buffer where the results of the operation should be stored.
void readRegister(char registerAddress, int numBytes, char * values){
  //Since we're performing a read operation, the most significant bit of the register address should be set.
  char address = 0x80 | registerAddress;
  //If we're doing a multi-byte read, bit 6 needs to be set as well.
  if(numBytes > 1)address = address | 0x40;
  
  //Set the Chip select pin low to start an SPI packet.
  digitalWrite(CS, LOW);
  //Transfer the starting register address that needs to be read.
  SPI.transfer(address);
  //Continue to read registers until we've read the number specified, storing the results to the input buffer.
  for(int i=0; i<numBytes; i++){
    values[i] = SPI.transfer(0x00);
  }
  //Set the Chips Select pin high to end the SPI packet.
  digitalWrite(CS, HIGH);
}

