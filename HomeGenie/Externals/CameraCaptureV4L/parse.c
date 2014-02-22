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
#include "parse.h"

#define ARG_EOS (-1) /* End of string   */
#define ARG_EOA (-2) /* End of argument */

typedef struct {
	
	char *s;
	char *sep;
	char opts;
	
	char quote;
	
} arg_t;

int arggetc(arg_t *arg)
{
	if(*arg->s == '\0') return(ARG_EOS);
	
	if((!(arg->opts & ARG_NO_ESCAPE) && *arg->s == '\\') &&
	    !(arg->quote && *(arg->s + 1) != '"'))
	{
		if(*(++arg->s) == '\0') return(ARG_EOS);
		return((int) *(arg->s++));
	}
	
	if(!(arg->opts & ARG_NO_QUOTE) && *arg->s == '"')
	{
		arg->s++;
		arg->quote = !arg->quote;
		
		return(arggetc(arg));
	}
	
	if(arg->quote) return((int) *(arg->s++));
	
	if(strchr(arg->sep, *arg->s))
	{
		arg->s++;
		return(ARG_EOA);
	}
	
	return((int) *(arg->s++));
}

int argopts(arg_t *arg, char *src, char *sep, int opts)
{
	memset(arg, 0, sizeof(arg_t));
	arg->s    = src;
	arg->sep  = sep;
	arg->opts = opts;
	return(0);
}

int argncpy(char *dst, size_t n, char *src, char *sep, int arg, int opt)
{
	arg_t s;
	char *d;
	size_t l;
	int r;
	
	argopts(&s, src, sep, opt);
	
	d = dst;
	*d = '\0';
	l = 0;
	
	while((r = arggetc(&s)) != ARG_EOS)
	{
		if(r == ARG_EOA)
		{
			if(!(opt & ARG_NO_TRIM) && !l) continue;
			if(!arg--) return(0);
			
			d = dst;
			*d = '\0';
			l = 0;
			
			continue;
		}
		
		if(n > l)
		{
			*(d++) = r;
			*d = '\0';
			l++;
		}
	}
	
	if(!arg && !(!(opt & ARG_NO_TRIM) && !l)) return(0);
	
	return(-1);
}

int arglen(char *src, char *sep, int arg, int opt)
{
	arg_t s;
	size_t l;
	int r;
	
	argopts(&s, src, sep, opt);
	
	l = 0;
	
	while((r = arggetc(&s)) != ARG_EOS)
	{
		if(r == ARG_EOA)
		{
			if(!(opt & ARG_NO_TRIM) && !l) continue;
			if(!arg--) return(l);
			
			l = 0;
			
			continue;
		}
		
		l++;
	}
	
	if(!arg && !(!(opt & ARG_NO_TRIM) && !l)) return(l);
	
	return(-1);
}

char *argdup(char *src, char *sep, int arg, int opt)
{
	char *dst;
	int l;
	
	l = arglen(src, sep, arg, opt);
	if(l < 0) return(NULL);
	
	dst = malloc(++l);
	if(!dst) return(NULL);
	
	argncpy(dst, l, src, sep, arg, opt);
	
	return(dst);
}

int argcount(char *src, char *sep, int opt)
{
	int count = 0;
	
	while(arglen(src, sep, count, opt) >= 0)
		count++;
	
	return(count);
}

long argtol(char *src, char *sep, int arg, int opt, int base)
{
	char *str;
	long val;
	
	str = argdup(src, sep, arg, opt);
	if(!str) return(-1);
	
	/* Catch any errors. */
	errno = 0;
	
	val = strtol(str, NULL, base);
	if(errno) val = -1;
	free(str);
	
	return(val);
}

int parse_font(char *src, char **font, int *fontsize)
{
	char  *p;
	size_t l;
	
	if(!src || !font || !fontsize) return(-1);
	
	l = strlen(src);
	
	/* Is the last element a font-size? */
	if((p = strrchr(src, ':')))
	{
		int i = atoi(p + 1);
		
		if(i > 0)
		{
			*fontsize = i;
			l = p - src;
		}
	}
	
	/* Copy the font name */
	if(*font) free(*font);
	*font = (char *) calloc(l + 1, 1);
	if(!*font) return(-1);
	strncpy(*font, src, l);
	
	return(0);
}

char *strtrim(char *str, char *ws)
{
	char *s = str;
	
	str += strspn(str, ws);
	
	while(*str != '\0' && strspn(str, ws) != strlen(str))
		*(s++) = *(str++);
	
	*s = '\0';
	
	return(str);
}

