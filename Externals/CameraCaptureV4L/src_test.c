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

#include <stdlib.h>
#include "src.h"
#include "log.h"

#define PUT_RGB(d, r, g, b) { d[0] = r; d[1] = g; d[2] = b; }

int src_test_open(src_t *src)
{
	uint8_t *p;
	uint32_t x, y;
	
	if(src->list & SRC_LIST_INPUTS) HEAD("--- No inputs.");
	if(src->list & SRC_LIST_TUNERS) HEAD("--- No tuners.");
	if(src->list & SRC_LIST_FORMATS) HEAD("--- Test only supports RGB24.");
	if(src->list & SRC_LIST_CONTROLS) HEAD("--- No controls.");
	
	/* Allocate memory for the test image. */
	src->length = src->width * src->height * 3;
	src->img    = (uint8_t *) malloc(src->length);
	if(!src->img) return(-1);
	
	/* Set the palette type. */
	src->palette = SRC_PAL_RGB24;
	
	/* Draw the test image. */
	p = src->img;
	for(y = 0; y < src->height; y++)
	{
		for(x = 0; x < src->width; x++)
		{
			int i = x / (src->width / 8);
			
			i = 7 - i;
			
			switch(i)
			{
			case 0:  /* Black */
				PUT_RGB(p, 0x00, 0x00, 0x00); break;
			case 1:  /* Blue */
				PUT_RGB(p, 0x00, 0x00, 0xFF); break;
			case 2:  /* Red */
				PUT_RGB(p, 0xFF, 0x00, 0x00); break;
			case 3:  /* Purple */
				PUT_RGB(p, 0xFF, 0x00, 0xFF); break;
			case 4:  /* Green */
				PUT_RGB(p, 0x00, 0xFF, 0x00); break;
			case 5:  /* Cyan */
				PUT_RGB(p, 0x00, 0xFF, 0xFF); break;
			case 6:  /* Yellow */
				PUT_RGB(p, 0xFF, 0xFF, 0x00); break;
			default: /* White */
				PUT_RGB(p, 0xFF, 0xFF, 0xFF); break;
			}
			
			p += 3;
		}
	}
	
	return(0);
}

int src_test_close(src_t *src)
{
	free(src->img);
	return(0);
}

int src_test_grab(src_t *src)
{
	return(0);
}

src_mod_t src_test = {
	"test", SRC_TYPE_NONE,
	src_test_open,
	src_test_close,
	src_test_grab
};

