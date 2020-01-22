class SerialReceiver {
  private Serial myPort;
  public int dataCount;
  public float[] recvData;

  public SerialReceiver(Serial myPort, int dataCount) {
    this.recvData=new float[dataCount];
    this.dataCount=dataCount;
    this.myPort=myPort;
    myPort.clear();
  }

  public void update() {
    while (myPort.available()>0) {
      String buffer=myPort.readStringUntil(0x0A);
      if (buffer!=null) {
        String[] data=split(trim(buffer), ",");
        for (int i=0; i<this.dataCount; i++) {
          recvData[i]=float(data[i]);
        }
      }
    }
  }

  public void reset() {
    myPort.clear();
  }
}
