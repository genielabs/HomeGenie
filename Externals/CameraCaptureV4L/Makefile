CFLAGS  = -g -O2 -DHAVE_CONFIG_H -fPIC
LDFLAGS = -lgd

OBJS =		CameraCaptureV4L.o log.o src.o src_file.o src_raw.o src_test.o src_v4l1.o src_v4l2.o 
OBJS +=		parse.o dec_jpeg.o

LIBS =

TARGET =	CameraCaptureV4L

$(TARGET):	$(OBJS)
	$(CC) -shared -fPIC $(OBJS) $(LIBS) $(LDFLAGS) -o $(TARGET).so
	#$(CC) -fPIC $(OBJS) $(LIBS) $(LDFLAGS) -o $(TARGET)

all:	$(TARGET)

clean:
	rm -f $(OBJS) $(TARGET) grab.raw $(TARGET).so

install:
	cp *.so /lib

exe:
	$(CC) -fPIC $(OBJS) $(LIBS) $(LDFLAGS) -o $(TARGET)
