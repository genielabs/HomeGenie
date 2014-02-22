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
#include <stdarg.h>
#include <string.h>
#include <syslog.h>
#include <unistd.h>
#include <fcntl.h>
#include <errno.h>
#include <sys/stat.h>
#include "log.h"

#define RESET     (0)
#define BOLD      (1)
#define FG_BLACK  (30)
#define FG_RED    (31)
#define FG_GREEN  (32)
#define FG_BROWN  (33)
#define FG_BLUE   (34)
#define FG_PURPLE (35)
#define FG_CYAN   (36)
#define FG_GREY   (37)

#define PACKAGE_NAME "RaspberryCam"


char use_syslog = 0;
int fd_log = STDERR_FILENO;

char quiet = 0;
char verbose = 0;

void log_set_fd(int fd)
{
	fd_log = fd;
}

int log_open(char *f)
{
	int fd;
	
	if(!f || use_syslog) return(0);
	
	fd = open(f, O_CREAT | O_WRONLY | O_APPEND | O_SYNC,
	          S_IRUSR | S_IWUSR);
	
	if(fd == -1)
	{
		ERROR("Error opening log file '%s'...", f);
		ERROR("open: %s", strerror(errno));
		return(-1);
	}
	
	fd_log = fd;
	
	return(0);
}

void log_close()
{
	if(fd_log == STDERR_FILENO || use_syslog) return;
	
	close(fd_log);
	fd_log = STDERR_FILENO;
}

void log_verbose(char v)
{
	verbose = v;
}

void log_quiet(char v)
{
	quiet = v;
}

void log_syslog(char v)
{
	use_syslog = v;
	
	if(use_syslog) openlog(PACKAGE_NAME, LOG_PID | LOG_CONS, LOG_USER);
	else closelog();
}

/* Taken from the GCC manual and cleaned up a bit. */
char *vmake_message(const char *fmt, va_list ap)
{
	/* Start with 100 bytes. */
	int n, size = 100;
	char *p;
	
	p = (char *) malloc(size);
	if(!p) return(NULL);
	
	while(1)
	{
		char *np;
		va_list apc;
		
		/* Try to print in the allocated space. */
		va_copy(apc, ap);
		n = vsnprintf(p, size, fmt, apc);
		
		/* If that worked, return the string. */
		if(n > -1 && n < size) return(p);
		
		/* Else try again with more space. */
		if(n > -1) size = n + 1; /* gibc 2.1: exactly what is needed */
		else size *= 2; /* glibc 2.0: twice the old size */
		
		np = (char *) realloc(p, size);
		if(!np)
		{
			free(p);
			return(NULL);
		}
		
		p = np;
	}
	
	free(p);
        return(NULL);
}

char *make_message(const char *fmt, ... )
{
	va_list ap;
	char *msg;
	
	va_start(ap, fmt);
	msg = vmake_message(fmt, ap);
	va_end(ap);
	
	return(msg);
}

void log_msg(char *file, char *function, int line, char l, char *s, ... )
{
	va_list ap;
	char *msg, *o;
	
	/* Is logging enabled? */
	if(fd_log == -1) return;
	
	/* Is this message visible in the current mode? */
	if(l == FLOG_MESSAGE && quiet) return;
	if(l == FLOG_HEAD && quiet) return;
	if(l == FLOG_INFO && !verbose) return;
	if(l == FLOG_WARN && quiet) return;
	if(l == FLOG_DEBUG && !verbose) return;
	
	/* Format the message. */
	va_start(ap, s);
	msg = vmake_message(s, ap);
	va_end(ap);
	
	if(!msg) return;
	
	/* Format the output. */
	o = NULL;
	
	if(l == FLOG_DEBUG) o = make_message("%s,%i: %s\n", function, line, msg);
	else o = make_message("%s\n", msg);
	
	if(!o)
	{
		free(msg);
		return;
	}
	
	/* Use text formatting if logging to stdout. */
	if(fd_log == STDERR_FILENO && !use_syslog)
	{
		int colour = -1;
		
		if(l == FLOG_ERROR)      colour = FG_RED;
		else if(l == FLOG_HEAD)  colour = BOLD;
		else if(l == FLOG_DEBUG) colour = FG_CYAN;
		else colour = RESET;
		
		if(!use_syslog) fprintf(stderr, "\033[%im", colour);
	}
	
	if(!use_syslog) write(fd_log, o, strlen(o));
	else
	{
		int p = LOG_INFO;
		
		switch(l)
		{
		case FLOG_ERROR: p = LOG_ERR; break;
		case FLOG_WARN:  p = LOG_WARNING; break;
		case FLOG_DEBUG: p = LOG_DEBUG; break;
		}
		
		syslog(p, "%s", o);
	}
	
	/* Reset console colour. */
	if(fd_log == STDERR_FILENO && !use_syslog) fprintf(stderr, "\033[%im", RESET);
	
	free(msg);
	free(o);
}

