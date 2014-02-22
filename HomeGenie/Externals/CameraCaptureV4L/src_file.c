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
#include <string.h>
#include <errno.h>
#include <sys/stat.h>
#include "src.h"
#include "log.h"

typedef struct {
	
	FILE *f;
	
	uint8_t *start;
	size_t length;
	
} src_file_t;

int src_file_open_jpeg(src_t *src)
{
	src_file_t *s = (src_file_t *) src->state;
	uint8_t *p;
	
	src->palette = SRC_PAL_JPEG;
	
	/* Scan the JPEG segments for the SOF, which contains
	 * the width and height of the image. */
	
	p = s->start + 2;
	
	while(p - s->start < src->length - 3)
	{
		uint8_t  header;
		uint16_t length;
		
		/* Check for the segment marker. */
		if(*(p++) != 0xFF)
		{
			ERROR("Segment marker not found,");
			return(-1);
		}
		
		header = *(p++);
		length = (p[0] << 8) + p[1];
		
		/* Verify the full segment is present. */
		if((p - s->start) + length >= s->length)
		{
			ERROR("Incomplete segment.");
			return(-1);
		}
		
		if(header == 0xC0) /* SOF */
		{
			uint16_t width  = (p[5] << 8) + p[6];
			uint16_t height = (p[3] << 8) + p[4];
			
			if(src->width != width ||
			   src->height != height)
			{
				MSG("Adjusting resolution to %ix%i.",
				    width, height);
				
				src->width = width;
				src->height = height;
			}
			
			return(0);
		}
		
		if(header == 0xD9 || /* EOI */
		   header == 0xDA)   /* SOS */
		{
			WARN("%s: Unable to read resolution.", src->source);
			return(-1);
		}
		
		p += length;
	}
	
	return(0);
}

int src_file_open_png(src_t *src)
{
	src_file_t *s = (src_file_t *) src->state;
	uint32_t width, height;
	uint8_t *p;
	
	src->palette = SRC_PAL_PNG;
	
	if(s->length < 24)
	{
		ERROR("%s: Unexpected end of file.", src->source);
		return(-1);
	}
	
        p = s->start + 12;
	
	if(strncmp((char *) p, "IHDR", 4))
	{
		ERROR("%s: IHDR chunk must be first.", src->source);
		return(-1);
	}
	
	p += 4;
	
	width  = (p[0] << 24) + (p[1] << 16) + (p[2] << 8) + p[3];
	height = (p[4] << 24) + (p[5] << 16) + (p[6] << 8) + p[7];
	
	if(src->width  != width ||
	   src->height != height)
	{
		MSG("Adjusting resolution to %ix%i.",
		    width, height);
		
		src->width = width;
		src->height = height;
	}
	
	return(0);
}

int src_file_open(src_t *src)
{
	src_file_t *s;
	struct stat st;
	size_t size;
	
	s = calloc(sizeof(src_file_t), 1);
	if(!s)
	{
		ERROR("Out of memory.");
		return(-2);
	}
	
	src->state = (void *) s;
	
	/* Get the file's size in bytes. */
	if(stat(src->source, &st) == -1)
	{
		ERROR("Error accessing file %s", src->source);
		ERROR("stat: %s", strerror(errno));
		free(s);
		return(-2);
	}
	
	s->f = fopen(src->source, "rb");
	if(!s->f)
	{
		ERROR("Error opening file %s", src->source);
		ERROR("fopen: %s", strerror(errno));
		free(s);
		return(-2);
	}
	
	/* Allocate memory for the file. */
	s->length = st.st_size;
	s->start = (uint8_t *) malloc(s->length);
	if(!s->start)
	{
		ERROR("Out of memory.");
		fclose(s->f);
		free(s);
		return(-1);
	}
	
	/* Read the entire file into memory. */
	size = 0;
	while(!feof(s->f) && size < s->length)
		size += fread(s->start, 1, s->length, s->f);
	
	fclose(s->f);
	s->f = NULL;
	
	if(size != s->length)
	{
		ERROR("Error reading file. Read %i bytes of %i byte file.",
		      size, s->length);
		free(s->start);
		free(s);
		return(-1);
	}
	
	src->length = s->length;
	src->img    = s->start;
	
	/* Test for a JPEG file. */
	if(s->start[0] == 0xFF && s->start[1] == 0xD8)
	{
		MSG("%s: Loading JPEG file.", src->source);
		
		if(src_file_open_jpeg(src))
		{
			src_close(src);
			return(-1);
		}
		
		return(0);
	}
	
	/* Test for a PNG file. */
	if(s->start[0] == 0x89 && s->start[1] == 0x50 &&
	   s->start[2] == 0x4e && s->start[3] == 0x47)
	{
		MSG("%s: Loading PNG file.", src->source);
		
		if(src_file_open_png(src))
		{
			src_close(src);
			return(-1);
		}
		
		return(0);
	}
	
	ERROR("%s: Unknown file format.", src->source);
	src_close(src);
	
	return(-2);
}

int src_file_close(src_t *src)
{
	src_file_t *s = (src_file_t *) src->state;
	
	if(s->f) fclose(s->f);
	free(s->start);
	free(s);
	
	return(0);
}

int src_file_grab(src_t *src)
{
	return(0);
}

src_mod_t src_file = {
	"file", SRC_TYPE_FILE,
	src_file_open,
	src_file_close,
	src_file_grab
};

