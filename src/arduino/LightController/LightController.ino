const String MAGIC_PREFIX = "f347ur323";

bool isOn = false;

void setup() {
  // initialize digital pin 13 as an output.
  pinMode(13, OUTPUT);

  // setup serial to 9600 baud
  Serial.begin(9600);

  // wait for serial to come available
  while (!Serial) {
    ;
  }
}

void loop() {
  
  establishContact();

  String magic = Serial.readStringUntil('\0');

  if (magic != MAGIC_PREFIX) {
    Serial.println("403");
    return;
  }

  int cmd = Serial.read();

  switch (cmd) {
    case 1:
      turnOnLight();
      break;
    case 2:
      turnOffLight();
      break;
  }

  Serial.println("200");
}

void turnOnLight() {
  isOn = true;
  digitalWrite(13, HIGH);
}

void turnOffLight() {
  isOn = false;
  digitalWrite(13, LOW);
}

void establishContact() {
  // as long as we haven't received a byte, print ~
  while (Serial.available() <= 0) {
    Serial.write(isOn ? 1 : 2);
    delay(500);
  }
}
