/* fswebcam - FireStorm.cx's webcam generator                 */
/*============================================================*/
/* Copyright (C)2005-2011 Philip Heron <phil@sanslogic.co.uk> */
/*                                                            */
/* This program is distributed under the terms of the GNU     */
/* General Public License, version 2. You may use, modify,    */
/* and redistribute it under the terms of this license. A     */
/* copy should be included with this source.                  */

#ifdef HAVE_CONFIG_H
#include "config.h"
#endif

#include <stdio.h>
#include <stdlib.h>
#include <fcntl.h>
#include <unistd.h>
#include <string.h>
#include <errno.h>
#include "src.h"
#include "log.h"

typedef struct {
	
	int fd;
	char *img;
	size_t size;
	
} src_raw_t;

int src_raw_open(src_t *src)
{
	src_raw_t *s;
	
	if(!src->source)
	{
		ERROR("No device or file name specified.");
		return(-2);
	}
	
	s = calloc(sizeof(src_raw_t), 1);
	if(!s)
	{
		ERROR("Out of memory.");
		return(-2);
	}
	
	src->state = (void *) s;
	
	/* Calculate size of the frame. */
	switch(src->palette)
	{
	case SRC_PAL_ANY:
		ERROR("No palette format specified.");
		free(s);
		return(-1);
	case SRC_PAL_RGB32:
		s->size = src->width * src->height * 4;
		break;
	case SRC_PAL_RGB24:
		s->size = src->width * src->height * 3;
		break;
	case SRC_PAL_RGB565:
	case SRC_PAL_RGB555:
	case SRC_PAL_YUYV:
	case SRC_PAL_UYVY:
	case SRC_PAL_Y16:
		s->size = src->width * src->height * 2;
		break;
	case SRC_PAL_YUV420P:
	case SRC_PAL_NV12MB:
		s->size = (src->width * src->height * 3) / 2;
		break;
	case SRC_PAL_BAYER:
	case SRC_PAL_SGBRG8:
	case SRC_PAL_SGRBG8:
	case SRC_PAL_GREY:
		s->size = src->width * src->height;
		break;
	default:
		ERROR("Palette format not supported by raw source.");
		free(s);
		return(-1);
	}
	
	s->img  = malloc(s->size);
	if(!s->img)
	{
		ERROR("Out of memory.");
		free(s);
		return(-1);
	}
	
	/* Open the source. */
	s->fd = open(src->source, O_RDONLY);
	if(s->fd < 0)
	{
		ERROR("Error opening source: %s", src->source);
		ERROR("open: %s", strerror(errno));
		free(s->img);
		free(s);
		return(-2);
	}
	
	src->img = s->img;
	src->length = s->size;
	
	MSG("%s opened.", src->source);
	
	return(0);
}

int src_raw_close(src_t *src)
{
	src_raw_t *s = (src_raw_t *) src->state;
	
	if(s->img) free(s->img);
	if(s->fd >= 0) close(s->fd);
	free(s);
	
	return(0);
}

int src_raw_grab(src_t *src)
{
	int i;
	src_raw_t *s = (src_raw_t *) src->state;
	
	i = s->size;
	while(i)
	{
		int r = read(s->fd, s->img, i);
		
		if(!r)
		{
			MSG("End of file reached.");
			return(-1);
		}
		
		if(r < 0)
		{
			ERROR("Error reading from source");
			ERROR("read: %s", strerror(errno));
			return(-1);
		}
		
		i -= r;
	}
	
	return(0);
}

src_mod_t src_raw = {
	"raw", SRC_TYPE_NONE,
	src_raw_open,
	src_raw_close,
	src_raw_grab
};

