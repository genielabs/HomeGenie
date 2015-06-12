#ifdef HAVE_CONFIG_H
#include "config.h"
#endif

#include <stdint.h>
#include <gd.h>

#include "src.h"

/* Define the bitmap type */
#ifdef USE_32BIT_BUFFER

typedef uint32_t avgbmp_t;
#define MAX_FRAMES (UINT32_MAX >> 8)

#else

typedef uint16_t avgbmp_t;
#define MAX_FRAMES (UINT16_MAX >> 8)

#endif
/*----*/

#define CLIP(val, min, max) (((val) > (max)) ? (max) : (((val) < (min)) ? (min) : (val)))

typedef struct PictureBuffer {
	int size;
	char *data;
} pictureBuffer;


gdImage* fswc_gdImageDuplicate(gdImage* src);
void SaveImageToJpegFile(char *filename, gdImagePtr im);
gdImage *grabPicture(char *device, int width, int height);

/* to take a simple picture */
extern pictureBuffer TakePicture(char *device, int width, int height, int jpegQuantity);
extern src_t* OpenCameraStream(char *device, int width, int height, int fps);
extern pictureBuffer GetFrame(src_t* src);
extern void CloseCameraStream(src_t* src);
