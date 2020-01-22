int LED_1 = 9;
int LED_2 = 10;
int LED_3 = 11;
int DS_1 = 0;
int DS_2 = 1;
int DS_3 = 2;
int trig_count_1 = 0;
int trig_count_2 = 0;
int trig_count_3 = 0;
int distance_threshold = 275;
int preview_threshold = 3;
int trig_threshold = 150;
bool recv_act = false;
unsigned long last_act = 0;
unsigned long last_1 = 0;
unsigned long last_2 = 0;
unsigned long last_3 = 0;
int act_cooldown = 1400;
byte send_buf = 0;
int move_tolerance = 100;

int led_idle_level = 1;
int led_high_level = 255;

void setup() {
  pinMode(LED_1, OUTPUT);
  pinMode(LED_2, OUTPUT);
  pinMode(LED_3, OUTPUT);
  Serial.begin(9600);
  last_act = millis();
  last_1 = millis();
  last_2 = millis();
  last_3 = millis();
}

void loop() {
  /*
    if (Serial.available() > 0) {
    //serial buffer is not empty
    byte buf;
    while (Serial.available() > 0) {
      buf = Serial.read();
    }
    //just activate receiver
    recv_act = true;
    }
  */
  if (recv_act) {
    //trig 1
    if (analogRead(DS_1) > distance_threshold) {
      trig_count_1++;
    }
    else {
      trig_count_1 = 0;
    }
    //preview 1
    if (trig_count_1 > preview_threshold) {
      analogWrite(LED_1, led_high_level);
      last_1 = millis();
      if (millis() - last_2 < move_tolerance) {
        Serial.write(8);
        deactivate();
      }
    }
    else {
      analogWrite(LED_1, led_idle_level);
    }

    //trig 2
    if (analogRead(DS_2) > distance_threshold) {
      trig_count_2++;
    }
    else {
      trig_count_2 = 0;
    }
    //preview 2
    if (trig_count_2 > preview_threshold) {
      analogWrite(LED_2, led_high_level);
      if (millis() - last_1 < move_tolerance || millis() - last_3 < move_tolerance) {
        last_2 = millis();
      }
    }
    else {
      analogWrite(LED_2, led_idle_level);
    }

    //trig 3
    if (analogRead(DS_3) > distance_threshold) {
      trig_count_3++;
    }
    else {
      trig_count_3 = 0;
    }
    //preview 3
    if (trig_count_3 > preview_threshold) {
      analogWrite(LED_3, led_high_level);
      last_3 = millis();
      if (millis() - last_2 < move_tolerance) {
        Serial.write(9);
        deactivate();
      }
    }
    else {
      analogWrite(LED_3, led_idle_level * 10);
    }
    //stabilizer
    if (trig_count_1 > trig_threshold) {
      send_buf += 1;
    }
    if (trig_count_2 > trig_threshold) {
      send_buf += 2;
    }
    if (trig_count_3 > trig_threshold) {
      send_buf += 4;
    }
    if (send_buf > 0) {
      Serial.write(send_buf);
      deactivate();
    }
    send_buf = 0;
  }
  else {
    //set led
    digitalWrite(LED_1, LOW);
    digitalWrite(LED_2, LOW);
    digitalWrite(LED_3, LOW);
    if (millis() - last_act > act_cooldown) {
      recv_act = true;
    }
  }
  delay(5);
}

void deactivate() {
  trig_count_1 = 0;
  trig_count_2 = 0;
  trig_count_3 = 0;
  recv_act = false;
  last_act = millis();
}
