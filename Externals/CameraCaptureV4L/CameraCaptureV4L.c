//============================================================================
// Name        : CameraCaptureV4L.c
// Author      : Generoso Martello
// Version     :
// Copyright   : Open source based on RaspberryCam by Flechner Romain
// Description : Generic V4L camera capture for C# interop
//============================================================================

#ifdef HAVE_CONFIG_H
#include "config.h"
#endif

#include "CameraCaptureV4L.h"

#include <stdio.h>
#include <getopt.h>
#include <string.h>
#include <stdlib.h>
#include <unistd.h>
#include <time.h>
#include <gd.h>
#include <errno.h>
#include <signal.h>
#include <sys/types.h>
#include <sys/stat.h>

#include "log.h"
#include "src.h"
#include "parse.h"


src_t* cameraSource = NULL;


gdImage* fswc_gdImageDuplicate(gdImage* src)
{
	gdImage *dst;
	
	dst = gdImageCreateTrueColor(gdImageSX(src), gdImageSY(src));
	if(!dst) return(NULL);
	
	gdImageCopy(dst, src, 0, 0, 0, 0, gdImageSX(src), gdImageSY(src));
	
	return(dst);
}

void SaveImageToJpegFile(char *filename, gdImagePtr im)
{
  FILE *out;
  int size;
  char *data;
  out = fopen(filename, "wb");
  if (!out) {
    /* Error */
  }
  
  data = (char *) gdImageJpegPtr(im, &size, 100);
  
  if (!data) {
    /* Error */
  }
  if (fwrite(data, 1, size, out) != size) {
    /* Error */
  }
  if (fclose(out) != 0) {
    /* Error */
  }
  gdFree(data);  
}

pictureBuffer ConvertToPictureBuffer(gdImagePtr im, int quantity) {
	pictureBuffer buffer;
	
	memset(&buffer, 0, sizeof(buffer));
	
	buffer.data = (char *) gdImageJpegPtr(im, &(buffer.size), quantity);
	
	return buffer;
}



pictureBuffer TakePicture(char *device, int width, int height, int jpegQuantity) {
        pictureBuffer buffer;
        
        memset(&buffer, 0, sizeof(buffer));
        
        gdImage *image = grabPicture(strdup(device), width, height);
        
        if (image == NULL) {
                puts("image is NULL");
                return buffer;
        }
        
        buffer.data = (char *) gdImageJpegPtr(image, &buffer.size, jpegQuantity);
        
        gdImageDestroy(image);
        
        return buffer;
}

gdImage *grabPicture(char *device, int width, int height) {
        avgbmp_t *abitmap, *pbitmap;
        gdImage *image, *original;
        src_t src;
        uint32_t frame;
        uint32_t x, y;
        uint8_t modified;
        gdImage *im;
        int frames = 1;
        
        memset(&src, 0, sizeof(src));
        
        src.input = strdup("0");
        src.tuner = 0;
        src.frequency = 0;
        src.delay = 0;
        src.use_read = 0;
        src.list = 0;
        src.fps = 0;
        src.palette = SRC_PAL_ANY;
        src.option = NULL;
        src.timeout = 10000;
        src.width = width;
        src.height = height;
        
        if(src_open(&src, device) == -1)
                return NULL;
        
        abitmap = (avgbmp_t*)calloc(src.width * src.height * 3, sizeof(avgbmp_t));
        if(!abitmap)
        {
                puts("Out of memory.");
                return NULL;
        }
        
        src_grab(&src);
        
        fswc_add_image_jpeg(&src, abitmap);
        
        src_close(&src);
        
        original = gdImageCreateTrueColor(src.width, src.height);
        if(!original)
        {
                puts("Out of memory.");
                free(abitmap);
                return NULL;
        }
        
        pbitmap = abitmap;
        
        for(y = 0; y < src.height; y++)
                for(x = 0; x < src.width; x++)
                {
                        int px = x;
                        int py = y;
                        int colour;
                        
                        colour  = (*(pbitmap++) / frames) << 16;
                        colour += (*(pbitmap++) / frames) << 8;
                        colour += (*(pbitmap++) / frames);
                        
                        gdImageSetPixel(original, px, py, colour);
                }
        
        free(abitmap);
        
        image = fswc_gdImageDuplicate(original);
        if(!image)
        {
                puts("Out of memory.");
                gdImageDestroy(image);
                return NULL;
        }
        
        gdImageDestroy(original);
        
        return image;
}




src_t* OpenCameraStream(char *device, int width, int height, int fps) {
	src_t* cameraSource = (src_t*)malloc(sizeof(src_t));

	cameraSource->input = strdup("0");
	cameraSource->tuner = 0;
	cameraSource->frequency = 0;
	cameraSource->delay = 10;
	cameraSource->use_read = 0;
	cameraSource->list = 0;
	cameraSource->fps = fps;
	cameraSource->palette = SRC_PAL_ANY;
	cameraSource->option = NULL;
	cameraSource->timeout = 10000000;
	cameraSource->width = width;
	cameraSource->height = height;
	
	if(src_open(cameraSource, device) == -1)
	{
		return NULL;
	}
	return cameraSource;
}

void CloseCameraStream(src_t* cameraSource) {
	src_close(cameraSource);
}

int fswc_add_image_rgb565(src_t *src, avgbmp_t *abitmap)
{
	uint16_t *img = (uint16_t *) src->img;
	uint32_t i = src->width * src->height;
	
	if(src->length >> 1 < i) return(-1);
	
	while(i-- > 0)
	{
		uint8_t r, g, b;
		
		r = (*img & 0xF800) >> 8;
		g = (*img &  0x7E0) >> 3;
		b = (*img &   0x1F) << 3;
		
		*(abitmap++) += r + (r >> 5);
		*(abitmap++) += g + (g >> 6);
		*(abitmap++) += b + (b >> 5);
		
		img++;
	}
	
	return(0);
}

pictureBuffer GetFrame(src_t* cameraSource) {
	pictureBuffer buffer;
	avgbmp_t *abitmap, *pbitmap;
	gdImage *image, *original;
	uint32_t frame;
	uint32_t x, y;
	uint8_t modified;
	gdImage *im;
	int frames = 1;
	
	if (cameraSource == NULL)
		return buffer;
	

	abitmap = (avgbmp_t*)calloc(cameraSource->width * cameraSource->height * 3, sizeof(avgbmp_t));
	if(!abitmap)
	{
		puts("Out of memory.");
		return buffer;
	}
		
	
	src_grab(cameraSource);
	
	fswc_add_image_jpeg(cameraSource, abitmap);
	
	original = gdImageCreateTrueColor(cameraSource->width, cameraSource->height);
	if(!original)
	{
		puts("Out of memory.");
		free(abitmap);
		return buffer;
	}
	
	pbitmap = abitmap;
	
	for(y = 0; y < cameraSource->height; y++)
		for(x = 0; x < cameraSource->width; x++)
		{
			int px = x;
			int py = y;
			int colour;
			
			colour  = (*(pbitmap++) / frames) << 16;
			colour += (*(pbitmap++) / frames) << 8;
			colour += (*(pbitmap++) / frames);
			
			gdImageSetPixel(original, px, py, colour);
		}
	
	free(abitmap);
	
	image = fswc_gdImageDuplicate(original);
	if(!image)
	{
		puts("Out of memory.");
		gdImageDestroy(image);
		return buffer;
	}
	
	gdImageDestroy(original);
	
	

	if (image == NULL) {
		puts("image is NULL");
		return buffer;
	}
	
	buffer.data = (char *) gdImageJpegPtr(image, &buffer.size, 70);
	
	gdImageDestroy(image);
	
	return buffer;	

}
/*
int main(void) {
	FILE *out;
	OpenCameraStream("/dev/video0", 640, 480, 20);
	pictureBuffer buffer;
	
	uint32_t x, y, hlength;
	uint8_t *himg = NULL;
	gdImage *im;
	int i;
	


	memset(&buffer, 0, sizeof(buffer));

	src_grab(cameraSource);
	//buffer.data = (char*) src->img;
	//buffer.size = src->length;
	
	/ * MJPEG data may lack the DHT segment required for decoding... * /
	i = verify_jpeg_dht(cameraSource->img, cameraSource->length, &himg, &hlength);
	
	im = gdImageCreateFromJpegPtr(hlength, himg);
	if(i == 1) free(himg);
	
	if(!im)
		return(-1);
	
	
	buffer = ConvertToPictureBuffer(im, 100);
	
	gdImageDestroy(im);
	
	out = fopen("out-1.jpg", "wb");
	if (fwrite(buffer.data, 1, buffer.size, out) != buffer.size) {
		puts("Error");
	}
	fclose(out);

	free(buffer.data);




	
	memset(&buffer, 0, sizeof(buffer));

	src_grab(cameraSource);
	//buffer.data = (char*) src->img;
	//buffer.size = src->length;
	
	/ * MJPEG data may lack the DHT segment required for decoding... * /
	i = verify_jpeg_dht(cameraSource->img, cameraSource->length, &himg, &hlength);
	
	im = gdImageCreateFromJpegPtr(hlength, himg);
	if(i == 1) free(himg);
	
	if(!im)
		return(-1);
	
	
	buffer = ConvertToPictureBuffer(im, 100);
	
	gdImageDestroy(im);
	
	out = fopen("out-2.jpg", "wb");
	if (fwrite(buffer.data, 1, buffer.size, out) != buffer.size) {
		puts("Error");
	}
	fclose(out);

	free(buffer.data);






	
	CloseCameraStream();
	
	
	return EXIT_SUCCESS;
}
*/
