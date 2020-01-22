import processing.serial.*;

Serial serialKinect;
SerialReceiver srKinect;

Serial serialArduino;
boolean swipeLeft=false;
boolean swipeRight=false;
boolean stay1 = false;
boolean stay2=false;
boolean stay3=false;

Field f;
int totParticles = 10000;
Particle[] particles;
float distance = 50.0;
int strength=300;
PGraphics particlesLayer;
ArrayList<Attractor> attractors;
boolean hasLeftAtt = false;
boolean hasRightAtt = false;
Attractor leftAtt;
Attractor rightAtt;

color particlesColor;
float colorOffset=0;

int screenState = 0;

PFont font;
PImage imgHome;
PImage imgHand;
PImage imgPanel;
PImage imgKinect;

void setup() {
  font=createFont("font.ttf", 44);
  textAlign(CENTER, CENTER);

  imgHome=loadImage("home.jpg");
  imgHand=loadImage("hand.png");
  imgPanel=loadImage("panel.jpg");
  imgKinect=loadImage("kinect.jpg");

  serialKinect=new Serial(this, "COM2", 9600);
  srKinect=new SerialReceiver(serialKinect, 6);

  serialArduino=new Serial(this, "COM10", 9600);
  serialArduino.clear();

  fullScreen(P2D);
  noCursor();
  pixelDensity(1);
  particlesLayer = createGraphics(1920, 1080, P2D); 
  particlesLayer.pixelDensity = 1;
  particlesLayer.beginDraw();
  particlesLayer.background(0);
  particlesLayer.endDraw();
  f = new Field(8);
  attractors = new ArrayList<Attractor>();
  f.attractors = attractors;

  particles = new Particle[totParticles];
  for (int i = 0; i < totParticles; ++i) {
    particles[i] = new Particle(new PVector(random(width), random(height)), 4, 1);
  }
}

void draw() {
  if (serialArduino.available()>0) {
    byte ar=(byte)serialArduino.read();
    if (ar==0x08) {
      swipeLeft=true;
    } else if (ar==0x09) {
      swipeRight=true;
    } else {
      if (ar/4==1) {
        stay3=true;
      }
      if (ar%4/2==1) {
        stay2=true;
      }
      if (ar%2==1) {
        stay1=true;
      }
    }
  }
  /*
  println(stay1);
   println(stay2);
   println(stay3);
   println(swipeLeft);
   println(swipeRight);
   */

  background(0);

  int x;
  switch(screenState) {
  case 0:
    if (swipeLeft||swipeRight) {
      screenState=1;
      resetReg();
    }
    tint(255, 255, 255, 255);
    image(imgHome, 0, 0);

    x=frameCount%120;
    if (x<20) {
      tint(255, 255, 255, 6.25*x);
      image(imgHand, 580, 680, 140, 160);
    } else if (x<100) {
      tint(255, 255, 255, 255);
      image(imgHand, 8*x+420, 680, 140, 160);
    } else {
      tint(255, 255, 255, 750-6.25*x);
      image(imgHand, 1220, 680, 140, 160);
    }

    //blink
    if (frameCount%100>50) {
      fill(130, 255, 244);
      ellipse(640, 920, 30, 30);
    }
    break;

  case 1:
    if (stay1||stay2||stay3) {
      screenState=2;
      resetReg();
    }

    tint(255, 255, 255, 255);
    image(imgPanel, 0, 0);
    x=frameCount%120;
    if (x<30) {
      tint(255, 255, 255, 6*x);
      image(imgHand, 480, 280, 340, 400);
    } else if (x<90) {
      tint(255, 255, 255, 255);
      image(imgHand, 480, 280, 340, 400);
    } else {
      tint(255, 255, 255, 720-6*x);
      image(imgHand, 480, 280, 340, 400);
    }

    //blink
    if (frameCount%100>50) {
      fill(130, 255, 244);
      ellipse(510, 920, 30, 30);
    }
    break;

  case 2:
    if (stay1||stay2||stay3) {
      resetColor();
      screenState=3;
      resetReg();
    }

    tint(255, 255, 255, 255);
    image(imgKinect, 0, 0);

    //blink
    if (frameCount%100>50) {
      fill(130, 255, 244);
      ellipse(510, 920, 30, 30);
    }
    break;

  case 3:
    if (stay1) {
      screenState=0;
      resetReg();
    }
    if (stay2) {
      toggleRotation();
      resetReg();
    }
    if (stay3) {
      resetColor();
      resetReg();
    }

    srKinect.update();
    float[] kData = srKinect.recvData;
    if (kData[0]==0 && kData[1]==0 && kData[2]==0 && kData[3]==0 && kData[4]==0 && kData[5]==0) {
      fill(255, 180);
      textFont(font);
      text("Kinect disconnected or Bridge App not working", width/2, 3*height/4);
    } else if (kData[0]==-1 && kData[1]==-1) {
      renderParticles(particlesColor);
      image(particlesLayer, 0, 0);

      fill(255, 180);
      textFont(font);
      text("No Body Detected!", width/2, 3*height/4);
    } else {
      //process left hand
      if (kData[0]==-1) {
        hasLeftAtt=false;
        leftAtt=null;
      } else if (!hasLeftAtt && kData[0]==3) {
        hasLeftAtt=true;
        leftAtt=new Attractor(mapHandPosition(kData[2], kData[3]), strength);
      } else if (!hasLeftAtt && kData[0]==2) {
        hasLeftAtt=true;
        leftAtt=new Attractor(mapHandPosition(kData[2], kData[3]), -strength);
      } else if (hasLeftAtt && leftAtt.strength==strength && kData[0] == 2) {
        leftAtt.position=mapHandPosition(kData[2], kData[3]);
        leftAtt.strength=-strength;
      } else if (hasLeftAtt && leftAtt.strength==-strength && kData[0] == 3) {
        leftAtt.position=mapHandPosition(kData[2], kData[3]);
        leftAtt.strength=strength;
      } else if (hasLeftAtt) {
        leftAtt.position=mapHandPosition(kData[2], kData[3]);
      }

      //process right hand
      if (kData[1]==-1) {
        hasRightAtt=false;
        rightAtt=null;
      } else if (!hasRightAtt && kData[1]==3) {
        hasRightAtt=true;
        rightAtt=new Attractor(mapHandPosition(kData[4], kData[5]), strength);
      } else if (!hasRightAtt && kData[1]==2) {
        hasRightAtt=true;
        rightAtt=new Attractor(mapHandPosition(kData[4], kData[5]), -strength);
      } else if (hasRightAtt && rightAtt.strength==strength && kData[1] == 2) {
        rightAtt.position=mapHandPosition(kData[4], kData[5]);
        rightAtt.strength=-strength;
      } else if (hasRightAtt && rightAtt.strength==-strength && kData[1] == 3) {
        rightAtt.position=mapHandPosition(kData[4], kData[5]);
        rightAtt.strength=strength;
      } else if (hasRightAtt) {
        rightAtt.position=mapHandPosition(kData[4], kData[5]);
      }

      attractors.clear();
      if (hasLeftAtt) {
        attractors.add(leftAtt);
      }
      if (hasRightAtt) {
        attractors.add(rightAtt);
      }

      blendMode(ADD);
      noStroke();
      colorMode(HSB);
      float handsDistance=sqrt((kData[2]-kData[4])*(kData[2]-kData[4])+(kData[3]-kData[5])*(kData[3]-kData[5]));
      particlesColor=color((map(handsDistance, 0, 400, 0, 255)+colorOffset)%255, 255, 255);
      colorMode(RGB);
      //particlesLayer.strokeWeight(1.5);
      renderParticles(particlesColor);
      image(particlesLayer, 0, 0);
    }
    break;
  }
}

void renderParticles(color c) {
  f.distance = distance;
  noStroke();
  f.update();
  for (Attractor a : attractors) {
    a.display();
  }
  particlesLayer.beginDraw();
  particlesLayer.blendMode(BLEND);
  particlesLayer.fill(0, 10);
  particlesLayer.rect(0, 0, width, height);
  particlesLayer.blendMode(ADD);
  for (int i = 0; i < particles.length; ++i) {
    Particle p = particles[i];
    particlesLayer.stroke(c, map(p.velocity.mag(), 0, p.maxSpeed, 5, 50));
    for (Attractor a : attractors) {
      PVector dir = PVector.sub(a.position, p.position);
      if (dir.magSq() < 100) {
        p.lifeSpan = 0;
      };
    }
    if (p.isDead()) {
      particles[i] = new Particle(new PVector(random(width), random(height)), 10, 0.5);
    }
    p.follow(f);
    p.run(particlesLayer);
  }
  particlesLayer.endDraw();
}

void toggleRotation() {
  f.rotation = !f.rotation;
}

PVector mapHandPosition(float x, float y) {
  float new_x = map(x, 100, 420, 0, width);
  float new_y=map(y, 90, 300, 0, height);
  return new PVector(new_x, new_y);
}

void resetReg() {
  swipeLeft=false;
  swipeRight=false;
  stay1=false;
  stay2=false;
  stay3=false;
}

void resetColor() {
  colorMode(HSB);
  particlesColor=color(random(255), 255, 255);
  colorMode(RGB);
}
