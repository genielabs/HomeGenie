/* fswebcam - FireStorm.cx's webcam generator                 */
/*============================================================*/
/* Copyright (C)2005-2011 Philip Heron <phil@sanslogic.co.uk> */
/*                                                            */
/* This program is distributed under the terms of the GNU     */
/* General Public License, version 2. You may use, modify,    */
/* and redistribute it under the terms of this license. A     */
/* copy should be included with this source.                  */

#ifndef INC_LOG_H
#define INC_LOG_H

#define FLOG_MESSAGE (0)
#define FLOG_ERROR   (1)
#define FLOG_WARN    (2)
#define FLOG_DEBUG   (3)
#define FLOG_HEAD    (4)
#define FLOG_INFO    (5)

#define LOG(l, s, args...) \
	log_msg(__FILE__, (char *) __FUNCTION__, __LINE__, l, s, ## args)

#define MSG(s, args...)   LOG(FLOG_MESSAGE, s, ## args)
#define WARN(s, args...)  LOG(FLOG_WARN, s, ## args)
#define ERROR(s, args...) LOG(FLOG_ERROR, s, ## args)
#define DEBUG(s, args...) LOG(FLOG_DEBUG, s, ## args)
#define HEAD(s, args...)  LOG(FLOG_HEAD, s, ## args)
#define INFO(s, args...)  LOG(FLOG_INFO, s, ## args)

extern void log_set_fd(int fd);
extern int  log_open(char *f);
extern void log_close(void);
extern void log_quiet(char v);
extern void log_verbose(char v);
extern void log_debug(char v);
extern void log_syslog(char v);

extern void log_msg(char *file, char *function, int line, char l, char *s, ... );

#endif

