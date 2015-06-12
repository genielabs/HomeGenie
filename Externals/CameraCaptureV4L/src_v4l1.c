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
#include <sys/ioctl.h>
#include <sys/mman.h>
#include "videodev.h"
#include "videodev_mjpeg.h"
#include "src.h"
#include "log.h"

#ifdef HAVE_V4L1

typedef struct {
	
	int fd;
	void *map;
	
	struct video_mbuf vm;
	struct video_mmap mm;
	int frame;
	int pframe;
	
	struct mjpeg_requestbuffers mjpeg_breq;
	struct mjpeg_sync mjpeg_bsync;
	
	uint32_t buffer_length;
	uint8_t *buffer;
	
	uint16_t palette;
	
} src_v4l_t;

static int src_v4l_close(src_t *src);

typedef struct {
	uint16_t src;
	uint16_t v4l;
	uint16_t depth;
} v4l_palette_t;

v4l_palette_t v4l_palette[] = {
	{ SRC_PAL_JPEG,    VIDEO_PALETTE_RGB24,   24 },
	{ SRC_PAL_MJPEG,   VIDEO_PALETTE_RGB24,   24 },
	{ SRC_PAL_YUYV,    VIDEO_PALETTE_YUYV,    16 },
	{ SRC_PAL_UYVY,    VIDEO_PALETTE_UYVY,    16 },
	{ SRC_PAL_YUV420P, VIDEO_PALETTE_YUV420P, 12 },
	{ SRC_PAL_BGR24,   VIDEO_PALETTE_RGB24,   24 }, /* RGB24/32 in V4L1 */
	{ SRC_PAL_BGR32,   VIDEO_PALETTE_RGB32,   32 }, /* are actually BGR */
	{ SRC_PAL_RGB565,  VIDEO_PALETTE_RGB565,  16 },
	{ SRC_PAL_RGB555,  VIDEO_PALETTE_RGB555,  16 },
	{ SRC_PAL_GREY,    VIDEO_PALETTE_GREY,    8 },
	{ 0, 0, 0 }
};

int src_v4l_get_capability(src_t *src, int fd, struct video_capability *vd)
{
	if(ioctl(fd, VIDIOCGCAP, vd) < 0)
	{
		ERROR("%s: Not a V4L device?", src->source);
		ERROR("VIDIOCGCAP: %s", strerror(errno));
		return(-1);
	}
	
	/* Dump information about the capture card. */
	DEBUG("%s information:", src->source);
	DEBUG("vd.name: \"%s\"", vd->name);
	DEBUG("vd.type=0x%08x", vd->type);
	if(vd->type & VID_TYPE_CAPTURE)       DEBUG("- CAPTURE");
	if(vd->type & VID_TYPE_TUNER)         DEBUG("- TUNER");
	if(vd->type & VID_TYPE_TELETEXT)      DEBUG("- TELETEXT");
	if(vd->type & VID_TYPE_OVERLAY)       DEBUG("- OVERLAY");
	if(vd->type & VID_TYPE_CHROMAKEY)     DEBUG("- CHROMAKEY");
	if(vd->type & VID_TYPE_CLIPPING)      DEBUG("- CLIPPING");
	if(vd->type & VID_TYPE_FRAMERAM)      DEBUG("- FRAMERAM");
	if(vd->type & VID_TYPE_SCALES)        DEBUG("- SCALES");
	if(vd->type & VID_TYPE_MONOCHROME)    DEBUG("- MONOCHROME");
	if(vd->type & VID_TYPE_SUBCAPTURE)    DEBUG("- SUBCAPTURE");
	if(vd->type & VID_TYPE_MJPEG_ENCODER) DEBUG("- MJPEG_ENCODER");
	DEBUG("vd.maxwidth=%d", vd->maxwidth);
	DEBUG("vd.maxheight=%d", vd->maxheight);
	DEBUG("vd.minwidth=%d", vd->minwidth);
	DEBUG("vd.minheight=%d", vd->minheight);
	
	/* Make sure we can capture from this device. */
	if(!vd->type & VID_TYPE_CAPTURE)
	{
		ERROR("%s: Device does not support capturing.", src->source);
                return(-1);
        }
	
	/* Make sure we can capture at the requested resolution. */
	if(vd->minwidth && vd->minwidth > src->width)
	{
		WARN("Requested width too small. Adjusting %i -> %i.",
		     src->width, vd->minwidth);
		src->width = vd->minwidth;
	}
	
	if(vd->maxwidth && vd->maxwidth < src->width)
	{
		WARN("Requested width too large. Adjusting %i -> %i.",
		     src->width, vd->maxwidth);
		src->width = vd->maxwidth;
	}
	
	if(vd->minheight && vd->minheight > src->height)
	{
		WARN("Requested height too small. Adjusting %i -> %i.",
		     src->height, vd->minheight);
		src->height = vd->minheight;
	}
	
	if(vd->maxheight && vd->maxheight < src->height)
	{
		WARN("Requested height too large. Adjusting %i -> %i.",
		     src->height, vd->maxheight);
		src->height = vd->maxheight;
	}
	
	return(0);
}

int src_v4l_set_input(src_t *src, int fd,
                      struct video_capability *vd,
                      struct video_channel *vc)
{
 	int count = 0, i = -1;
	
	if(vd->channels == 0)
	{
		MSG("No inputs available.");
		return(-1);
	}
	
	if(src->list & SRC_LIST_INPUTS)
	{
		HEAD("--- Available inputs:");
		
		count = 0;
		vc->channel = count;
		while(!ioctl(fd, VIDIOCGCHAN, vc))
		{
			MSG("%i: %s", count, (char *) vc->name);
			vc->channel = ++count;
		}
	}
	
	/* If no input was specified, use input 0. */
	if(!src->input)
	{
		MSG("No input was specified, using the first.");
		count = 1;
		i = 0;
	}
	else
	{
		/* Check if the input is specified by name. */
		count = 0;
		vc->channel = count;
		while(!ioctl(fd, VIDIOCGCHAN, vc))
		{
			if(!strncasecmp((char *) vc->name, src->input, 32))
				i = count;
			vc->channel = ++count;
		}
		
		if(i == -1)
		{
			char *endptr;
			
			/* Is the input specified by number? */
			i = strtol(src->input, &endptr, 10);
			
			if(endptr == src->input) i = -1;
		}
	}
	
	if(i == -1 || i >= count)
	{
		/* The specified input wasn't found! */
		ERROR("Unrecognised input \"%s\"", src->input);
		return(-1);
	}
	
	/* Get data about the input channel. */
	vc->channel = i;
	if(ioctl(fd, VIDIOCGCHAN, vc) < 0)
	{
		ERROR("Error getting information for input \"%s\".",src->input);
		ERROR("VIDIOCGCHAN: %s", strerror(errno));
		return(-1);
	}
	
	if(src->input) DEBUG("Input %i (%s) information:", i, src->input);
	else DEBUG("Input %i information:", i, src->input);
	DEBUG("vc.channel=%d", vc->channel);
	DEBUG("vc.name=\"%s\"", vc->name);
	DEBUG("vc.tuners=%d",vc->tuners);
	DEBUG("vc.flags=0x%08x", vc->flags);
	if(vc->flags & VIDEO_VC_TUNER) DEBUG("- TUNER");
	if(vc->flags & VIDEO_VC_AUDIO) DEBUG("- AUDIO");
	DEBUG("vc.type=0x%08x", vc->type);
	if(vc->type & VIDEO_TYPE_TV) DEBUG("- TV");
	if(vc->type & VIDEO_TYPE_CAMERA) DEBUG("- CAMERA");
	DEBUG("vc.norm=%d", vc->norm);
	
	MSG("Setting input to %i.", i);
	
	/* Set the source. */
	if(ioctl(fd, VIDIOCSCHAN, vc) < 0)
	{
		ERROR("Error while setting input to %i.", i);
		ERROR("VIDIOCSCHAN: %s", strerror(errno));
		return(-1);
	}
	
	return(0);
}

int src_v4l_set_tuner(src_t *src, int fd,
                      struct video_channel *vc,
                      struct video_tuner *vt)
{
	char *range;
	
	if(vc->tuners == 0)
	{
		MSG("No tuners available.");
		return(-1);
	}
	
	/* Check the requested tuner exists. */
	if(src->tuner >= vc->tuners)
	{
		ERROR("Requested tuner %i and only %i tuner(s) found.",
		     src->tuner, vc->tuners - 1);
		return(-1);
	}
	
	/* Get data about the tuner. */
	vt->tuner = src->tuner;
	if(ioctl(fd, VIDIOCGTUNER, vt) < 0)
	{
		ERROR("Error getting information on tuner %i (input %i).",
		      src->tuner, src->input);
		ERROR("VIDIOCGCHAN: %s", strerror(errno));
		return(-1);
	}
	
	if(vt->flags & VIDEO_TUNER_LOW) range = "KHz";
	else range = "MHz";
	
	DEBUG("Tuner %i information:", src->tuner);
	DEBUG("vt.tuner=%d", vt->tuner);
	DEBUG("vt.name=\"%s\"", vt->name);
	DEBUG("vt.rangelow=%u (%.3f%s)", (unsigned int) vt->rangelow,
	      (float) vt->rangelow / 16, range);
	DEBUG("vt.rangehigh=%u (%.3f%s)", (unsigned int) vt->rangehigh,
	      (float) vt->rangehigh / 16, range);
	DEBUG("vt.flags=0x%08x", vt->flags);
	if(vt->flags & VIDEO_TUNER_PAL) DEBUG("- PAL");
	if(vt->flags & VIDEO_TUNER_NTSC) DEBUG("- NTSC");
	if(vt->flags & VIDEO_TUNER_SECAM) DEBUG("- SECAM");
	if(vt->flags & VIDEO_TUNER_LOW) DEBUG("- LOW");
	if(vt->flags & VIDEO_TUNER_NORM) DEBUG("- NORM");
	if(vt->flags & VIDEO_TUNER_STEREO_ON) DEBUG("- STEREO_ON");
	if(vt->flags & VIDEO_TUNER_RDS_ON) DEBUG("- RDS_ON");
	if(vt->flags & VIDEO_TUNER_MBS_ON) DEBUG("- MBS_ON");
	DEBUG("vt.mode=0x%08x", vt->mode);
	if(vt->mode & VIDEO_MODE_PAL) DEBUG("- PAL");
	if(vt->mode & VIDEO_MODE_NTSC) DEBUG("- NTSC");
	if(vt->mode & VIDEO_MODE_SECAM) DEBUG("- SECAM");
	if(vt->mode & VIDEO_MODE_AUTO) DEBUG("- AUTO");
	DEBUG("vt.signal=%d", vt->signal);
	
	MSG("Setting tuner to %i.", src->tuner);
	
	if(ioctl(fd, VIDIOCSTUNER, vt) < 0)
	{
		ERROR("Error setting tuner %i (input %i).",
		      src->tuner, src->input);
		ERROR("VIDIOCSTUNER: %s", strerror(errno));
		return(-1);
	}
	
	return(0);
}

int src_v4l_set_frequency(src_t *src, int fd, struct video_tuner *vt)
{
	char *range;
	unsigned long freq;
	float ff;
	
	if(vt->flags & VIDEO_TUNER_LOW) range = "KHz";
	else range = "MHz";
	
	/* Get the current frequency. */
	ff = 0;
	ioctl(fd, VIDIOCGFREQ, &freq);
	if(freq) ff = freq / 16;
	DEBUG("Current frequency is %.3f%s", ff, range);
	
	/* Don't set the frequency if it's 0. */
	if(src->frequency == 0) return(0);
	
	/* Calculate the frequency. */
	freq = (src->frequency / 1000) * 16;
	
	/* Ensure frequency is within range of the tuner. */
	if(freq < vt->rangelow)
	{
		WARN("Frequency is below tuners minimum. Using %.3f%s.",
		     (float) vt->rangelow / 16, range);
		
		freq = vt->rangelow;
	}
	
	if(freq > vt->rangehigh)
	{
		WARN("Frequency is above tuners maximum. Using %.3f%s.",
		     (float) vt->rangehigh / 16, range);
		
		freq = vt->rangehigh;
	}
	
	/* Set the frequency. */
	MSG("Setting frequency to %.3f%s.", (float) freq / 16, range);
	
	if(ioctl(fd, VIDIOCSFREQ, &freq) == -1)
	{
		ERROR("Error setting frequency.");
		ERROR("VIDIOCSFREQ: %s", strerror(errno));
		return(-1);
	}
	
	return(0);
}

int src_v4l_set_picture(src_t *src, int fd, struct video_capability *vd)
{
	src_v4l_t *s = (src_v4l_t *) src->state;
	struct video_picture vp;
	int v4l_pal;
	char *value;
	
	memset(&vp, 0, sizeof(vp));
	
	if(ioctl(fd, VIDIOCGPICT, &vp) < 0)
	{
		ERROR("Error getting picture information.");
		ERROR("VIDIOCGPICT: %s", strerror(errno));
		return(-1);
	}
	
	if(src->list & SRC_LIST_CONTROLS)
	{
		int i;
		
		HEAD("--- Available controls:");
		
		for(i = 0; i < 5; i++)
		{
			char *name = NULL;
			int val;
			char t[64];
			
			switch(i)
			{
			case 0:
				name = "brightness";
				val = vp.brightness;
				break;
			case 1:
				name = "hue";
				val = vp.hue;
				break;
			case 2:
				name = "colour";
				val = vp.colour;
				break;
			case 3:
				name = "contrast";
				val = vp.contrast;
				break;
			case 4:
				name = "whiteness";
				val = vp.whiteness;
				break;
			}
			
			snprintf(t, 63, "%i (%i%%)",
			        SCALE(-100, 100, 0x0000, 0xFFFF, val),
			        SCALE(0, 100, 0x0000, 0xFFFF, val));
			
			MSG("%-25s %-15s 100 - -100", name, t);
		}
	}
	
	if(!src_get_option_by_name(src->option, "brightness", &value))
		vp.brightness = SCALE(0x0000, 0xFFFF, -100, 100, atoi(value));
	
	if(!src_get_option_by_name(src->option, "hue", &value))
		vp.hue        = SCALE(0x0000, 0xFFFF, -100, 100, atoi(value));
	
	if(!src_get_option_by_name(src->option, "colour", &value))
		vp.colour     = SCALE(0x0000, 0xFFFF, -100, 100, atoi(value));
	
	if(!src_get_option_by_name(src->option, "contrast", &value))
		vp.contrast   = SCALE(0x0000, 0xFFFF, -100, 100, atoi(value));
	
	if(!src_get_option_by_name(src->option, "whiteness", &value))
		vp.whiteness  = SCALE(0x0000, 0xFFFF, -100, 100, atoi(value));
	
	/* MJPEG devices are a special case... */
	if(vd->type & VID_TYPE_MJPEG_ENCODER)
	{
		WARN("Device is MJPEG only. Forcing JPEG palette.");
		src->palette = SRC_PAL_JPEG;
	}
	
	if(src->palette == SRC_PAL_JPEG && !(vd->type & VID_TYPE_MJPEG_ENCODER))
	{
		ERROR("MJPEG palette requsted for a non-MJPEG device.");
		return(-1);
	}
	
	if(src->palette == SRC_PAL_JPEG)
	{
		struct mjpeg_params mparm;
		
		memset(&mparm, 0, sizeof(mparm));
		
		if(ioctl(s->fd, MJPIOC_G_PARAMS, &mparm))
		{
			ERROR("Error querying video parameters.");
			ERROR("MJPIOC_G_PARAMS: %s", strerror(errno));
			return(-1);
		}
		
		DEBUG("%s: Video parameters...", src->source);
		DEBUG("major_version  = %i", mparm.major_version);
		DEBUG("minor_version  = %i", mparm.minor_version);
		DEBUG("input          = %i", mparm.input);
		DEBUG("norm           = %i", mparm.norm);
		DEBUG("decimation     = %i", mparm.decimation);
		DEBUG("HorDcm         = %i", mparm.HorDcm);
		DEBUG("VerDcm         = %i", mparm.VerDcm);
		DEBUG("TmpDcm         = %i", mparm.TmpDcm);
		DEBUG("field_per_buff = %i", mparm.field_per_buff);
		DEBUG("img_x          = %i", mparm.img_x);
		DEBUG("img_y          = %i", mparm.img_y);
		DEBUG("img_width      = %i", mparm.img_width);
		DEBUG("img_height     = %i", mparm.img_height);
		DEBUG("quality        = %i", mparm.quality);
		DEBUG("odd_even       = %i", mparm.odd_even);
		DEBUG("APPn           = %i", mparm.APPn);
		DEBUG("APP_len        = %i", mparm.APP_len);
		DEBUG("APP_data       = \"%s\"", mparm.APP_data);
		DEBUG("COM_len        = %i", mparm.COM_len);
		DEBUG("COM_data       = \"%s\"", mparm.COM_data);
		DEBUG("jpeg_markers   = %i", mparm.jpeg_markers);
		if(mparm.jpeg_markers & JPEG_MARKER_DHT) DEBUG("- DHT");
		if(mparm.jpeg_markers & JPEG_MARKER_DQT) DEBUG("- DQT");
		if(mparm.jpeg_markers & JPEG_MARKER_DRI) DEBUG("- DRI");
		if(mparm.jpeg_markers & JPEG_MARKER_COM) DEBUG("- COM");
		if(mparm.jpeg_markers & JPEG_MARKER_APP) DEBUG("- APP");
		DEBUG("VFIFO_FB       = %i", mparm.VFIFO_FB);
		
		/* Set the usual parameters. */
		mparm.input = 0;
		mparm.norm = VIDEO_MODE_PAL;
		/* TODO: Allow user to select PAL/SECAM/NTSC */
		
		mparm.decimation = 0;
		mparm.quality = 80;
		
		mparm.HorDcm = 1;
		mparm.VerDcm = 1;
		mparm.TmpDcm = 1;
		mparm.field_per_buff = 2;
		
		/* Ask the driver to include the DHT with each frame. */
		mparm.jpeg_markers |= JPEG_MARKER_DHT;
		
		if(ioctl(s->fd, MJPIOC_S_PARAMS, &mparm))
		{
			ERROR("Error setting video parameters.");
			ERROR("MJPIOC_S_PARAMS: %s", strerror(errno));
			return(-1);
		}
		
		src->width = mparm.img_width / mparm.HorDcm;
		src->height = mparm.img_height / mparm.VerDcm * mparm.field_per_buff;
		
		return(0);
	}
	
	v4l_pal = 2; /* Skip the JPEG palette. */
	
	if(src->palette != -1)
	{
		v4l_pal = 0;
		
		while(v4l_palette[v4l_pal].depth)
		{
			if(v4l_palette[v4l_pal].src == src->palette) break;
			v4l_pal++;
		}
		
		if(!v4l_palette[v4l_pal].depth)
		{
			ERROR("Unable to handle palette format %s.",
			      src_palette[src->palette]);
			
			return(-1);
		}
	}
	
	while(v4l_palette[v4l_pal].depth)
	{
		vp.palette = v4l_palette[v4l_pal].v4l;
		vp.depth   = v4l_palette[v4l_pal].depth;
		
		if(!ioctl(fd, VIDIOCSPICT, &vp))
		{
			s->palette = v4l_pal;
			src->palette = v4l_palette[v4l_pal].src;
			
			MSG("Using palette %s.",src_palette[src->palette].name);
			
			return(0);
		}
		
		if(src->palette != -1) break;
		
		WARN("The device does not support palette %s.",
		     src_palette[v4l_palette[v4l_pal].src].name);
		
		v4l_pal++;
	}
	
	ERROR("Unable to find a compatible palette.");
	
	return(-1);
}

int src_v4l_free_mmap(src_t *src)
{
	src_v4l_t *s = (src_v4l_t *) src->state;
	
	if(src->palette == SRC_PAL_JPEG)
		munmap(s->map, s->mjpeg_breq.count * s->mjpeg_breq.size);
	else
		munmap(s->map, s->vm.size);
	s->map = NULL;
	
	return(0);
}

int src_v4l_set_mmap(src_t *src, int fd)
{
	src_v4l_t *s = (src_v4l_t *) src->state;
	uint32_t frame;
	
	if(src->palette == SRC_PAL_JPEG)
	{
		s->mjpeg_breq.count = 32;
		s->mjpeg_breq.size  = 256 * 1024;
		
		if(ioctl(s->fd, MJPIOC_REQBUFS, &s->mjpeg_breq))
		{
			ERROR("Error requesting video buffers.");
			ERROR("MJPIOC_REQBUFS: %s", strerror(errno));
		}
		
		DEBUG("Got %ld buffers of size %ld KB",
		      s->mjpeg_breq.count, s->mjpeg_breq.size / 1024);
		
		s->map = mmap(0, s->mjpeg_breq.count * s->mjpeg_breq.size,
		              PROT_READ | PROT_WRITE, MAP_SHARED, fd, 0);
		
		if(s->map == MAP_FAILED)
		{
			WARN("Error mmap'ing buffers.");
			WARN("mmap: %s", strerror(errno));
			return(-1);
		}
		
		return(0);
	}
	
	/* Find out how many buffers are available. */
	if(ioctl(fd, VIDIOCGMBUF, &s->vm) < 0)
	{
		WARN("Error while querying buffers.");
		WARN("VIDEOCGMBUF: %s", strerror(errno));
		return(-1);
	}
	
	DEBUG("mmap information:");
	DEBUG("size=%d", s->vm.size);
	DEBUG("frames=%d", s->vm.frames);
	
	/* mmap all available buffers. */
	s->map = mmap(0, s->vm.size, PROT_READ | PROT_WRITE, MAP_SHARED, fd, 0);
	if(s->map == MAP_FAILED)
	{
		WARN("Error mmap'ing all available buffers.");
		WARN("mmap: %s", strerror(errno));
		return(-1);
	}
	
	/* Setup capture options. */
	s->mm.format = v4l_palette[s->palette].v4l;
	s->mm.width  = src->width;
	s->mm.height = src->height;
	
	/* Request the maximum number of frames the device can handle. */
	for(frame = 0; frame < s->vm.frames; frame++)
	{
		s->mm.frame = frame;
		if(ioctl(fd, VIDIOCMCAPTURE, &s->mm) < 0)
		{
			WARN("Error while requesting buffer %i to capture an image.", frame);
			WARN("VIDIOCMCAPTURE: %s", strerror(errno));
			src_v4l_free_mmap(src);
			return(-1);
		}
	}
	
	return(0);
}

int src_v4l_set_read(src_t *src)
{
	src_v4l_t *s = (src_v4l_t *) src->state;
	struct video_window vwin;
	
	memset(&vwin, 0, sizeof(vwin));
	vwin.width = src->width;
	vwin.height = src->height;
	if(ioctl(s->fd, VIDIOCSWIN, &vwin) == -1)
	{
		ERROR("Error setting %ix%i resolution.",
		      src->width, src->height);
	}
	
	/* Ugly!, but should always be large enough. */
	s->buffer_length = src->width * src->height * 4;
	s->buffer = malloc(s->buffer_length);
	if(!s->buffer)
	{
		ERROR("Out of memory.");
		src_v4l_close(src);
		return(-1);
	}
	
	return(0);
}

int src_v4l_queue_buffers(src_t *src)
{
	src_v4l_t *s = (src_v4l_t *) src->state;
	int n;
	
	/* Queue all buffers. */
	for(n = 0; n < s->mjpeg_breq.count; n++)
	{
		if(ioctl(s->fd, MJPIOC_QBUF_CAPT, &n))
		{
			ERROR("Error queing buffer %i.", n);
			ERROR("MJPIOC_QBUF_CAPT: %s", strerror(errno));
			return(-1);
		}
	}
	
	return(0);
}

static int src_v4l_open(src_t *src)
{
	src_v4l_t *s;
	struct video_capability vd;
	struct video_channel vc;
	struct video_tuner vt;
	
	if(!src->source)
	{
		ERROR("No device name specified.");
		return(-2);
	}
	
	/* Allocate memory for state structure. */
	s = (src_v4l_t *) calloc(sizeof(src_v4l_t), 1);
	if(!s)
	{
		ERROR("Out of memory.");
		return(-2);
	}
	
	src->state = (void *) s;
	
	memset(&vd, 0, sizeof(vd));
	memset(&vc, 0, sizeof(vc));
	memset(&vt, 0, sizeof(vt));
	
	/* Open the device. */
	s->fd = open(src->source, O_RDWR | O_NONBLOCK);
	if(s->fd < 0)
	{
		ERROR("Error opening device: %s", src->source);
		ERROR("open: %s", strerror(errno));
		free(s);
		return(-2);
	}
	
	MSG("%s opened.", src->source);
	
	/* Get the device capabilities. */
	if(src_v4l_get_capability(src, s->fd, &vd))
	{
		src_v4l_close(src);
		return(-2);
	}
	
	/* Set the input. */
	if(src_v4l_set_input(src, s->fd, &vd, &vc))
	{
		src_v4l_close(src);
		return(-1);
	}
	
	if(vc.flags & VIDEO_VC_TUNER)
	{
		/* Set the tuner. */
		if(src_v4l_set_tuner(src, s->fd, &vc, &vt))
		{
			src_v4l_close(src);
			return(-1);
		}
		
		/* Set the frequency. */
		if(src_v4l_set_frequency(src, s->fd, &vt))
		{
			src_v4l_close(src);
			return(-1);
		}
	}
	
	/* Set the picture options. */
	if(src_v4l_set_picture(src, s->fd, &vd))
	{
		src_v4l_close(src);
		return(-1);
	}
	
	/* Delay to let the image settle down. */
	if(src->delay)
	{
		MSG("Delaying %i seconds.", src->delay);
		usleep(src->delay * 1000 * 1000);
	}
	
	/* Setup the mmap. */
	if(!src->use_read && src_v4l_set_mmap(src, s->fd))
	{
		WARN("Unable to use mmap. Using read instead.");
		src->use_read = -1;
	}
	
	if(src->use_read && src_v4l_set_read(src))
	{
		src_v4l_close(src);
		return(-1);
	}
	
	/* If this is an MJPEG device, queue the buffers. */
	if(src->palette == SRC_PAL_JPEG && src_v4l_queue_buffers(src))
	{
		src_v4l_close(src);
		return(-1);
	}
	
	s->frame  = 0;
	s->pframe = -1;
	
	return(0);
}

static int src_v4l_close(src_t *src)
{
	src_v4l_t *s = (src_v4l_t *) src->state;
	
	if(s->fd >= 0)
	{
		if(s->map) src_v4l_free_mmap(src);
		close(s->fd);
		MSG("%s closed.", src->source);
	}
	
	if(s->buffer) free(s->buffer);
	free(s);
	
	return(0);
}

int src_v4l_grab_mjpeg(src_t *src)
{
	src_v4l_t *s = (src_v4l_t *) src->state;
	
	/* Finished with the previous frame? */
	if(s->pframe >= 0)
	{
		if(ioctl(s->fd, MJPIOC_QBUF_CAPT, &s->mjpeg_bsync.frame))
		{
			ERROR("Error queing buffer.");
			ERROR("MJPIOC_QBUF_CAPT: %s", strerror(errno));
			return(-1);
		}
	}
	
	if(ioctl(s->fd, MJPIOC_SYNC, &s->mjpeg_bsync))
	{
		ERROR("Error sync'ing buffer.");
		ERROR("MJPIOC_SYNC: %s", strerror(errno));
		return(-1);
	}
	
	/* Deal with it! */
	src->img    = s->map + s->mjpeg_bsync.frame * s->mjpeg_breq.size;
	src->length = s->mjpeg_bsync.length;
	
	s->pframe++;
	
	return(0);
}

static int src_v4l_grab(src_t *src)
{
	src_v4l_t *s = (src_v4l_t *) src->state;
	
	/* MJPEG devices are handled differently. */
	if(src->palette == SRC_PAL_JPEG) return(src_v4l_grab_mjpeg(src));
	
	/* Wait for a frame. */
	if(src->timeout)
	{
		fd_set fds;
		struct timeval tv;
		int r;
		
		/* Is a frame ready? */
		FD_ZERO(&fds);
		FD_SET(s->fd, &fds);
		
		tv.tv_sec = src->timeout;
		tv.tv_usec = 0;
		
		r = select(s->fd + 1, &fds, NULL, NULL, &tv);
		
		if(r == -1)
		{
			ERROR("select: %s", strerror(errno));
			return(-1);
		}
		
		if(!r)
		{
			ERROR("Timed out waiting for frame!");
			return(-1);
		}
	}
	
	/* If using mmap... */
	if(s->map)
	{
		/* Finished with the previous frame? */
		if(s->pframe >= 0)
		{
			s->mm.frame = s->pframe;
			if(ioctl(s->fd, VIDIOCMCAPTURE, &s->mm) < 0)
			{
				ERROR("Error while requesting buffer %i to capture an image.", s->pframe);
				ERROR("VIDEOCMCAPTURE: %s", strerror(errno));
				return(-1);
			}
		}
		
		/* Wait for the frame to be captured. */
		if(ioctl(s->fd, VIDIOCSYNC, &s->frame) < 0)
		{
			WARN("Error synchronising with buffer %i.", s->frame);
			WARN("VIDIOCSYNC: %s", strerror(errno));
			return(-1);
		}
		
		/* Get the pointer to the current frame. */
		src->img = s->map + s->vm.offsets[s->frame];
		
		/* How big is the frame? */
		if(s->frame == s->vm.frames - 1)
			src->length = s->vm.size - s->vm.offsets[s->frame];
		else
			src->length = s->vm.offsets[s->frame + 1] -
			              s->vm.offsets[s->frame];
		
		s->pframe = s->frame;
		if(++s->frame == s->vm.frames) s->frame = 0;
	}
	else
	{
		ssize_t r = read(s->fd, s->buffer, s->buffer_length);
		
		if(r <= 0)
		{
			WARN("Didn't read a frame.");
			WARN("read: %s", strerror(errno));
			return(-1);
		}
		
		src->img = s->buffer;
		src->length = r;
	}
	
	return(0);
}

src_mod_t src_v4l1 = {
	"v4l1", SRC_TYPE_DEVICE,
	src_v4l_open,
	src_v4l_close,
	src_v4l_grab,
};

#else /* #ifdef HAVE_V4L1 */

src_mod_t src_v4l1 = {
        "", SRC_TYPE_NONE,
        NULL,
        NULL,
        NULL
};

#endif /* #ifdef HAVE_V4L1 */

