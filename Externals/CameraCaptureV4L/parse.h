/* fswebcam - FireStorm.cx's webcam generator                 */
/*============================================================*/
/* Copyright (C)2005-2011 Philip Heron <phil@sanslogic.co.uk> */
/*                                                            */
/* This program is distributed under the terms of the GNU     */
/* General Public License, version 2. You may use, modify,    */
/* and redistribute it under the terms of this license. A     */
/* copy should be included with this source.                  */

#ifndef INC_PARSE_H
#define INC_PARSE_H

#define ARG_NONE      (0)
#define ARG_NO_QUOTE  (1 << 1)
#define ARG_NO_ESCAPE (1 << 2)
#define ARG_NO_TRIM   (1 << 3)

#define WHITESPACE " \t\n\r"

/* src: Source string to be parsed.
 * sep: String containing characters to use as separators.
 * arg: Argument to retreve.
 * opt: Options for parser:
 *
 *      ARG_NO_QUOTE:  Ignore quotes.
 *      ARG_NO_ESCAPE: Ignore escape characters.
 *      ARG_NO_TRIM:   Do not ignore empty arguments.
*/

/* Copies an argument to a user provided buffer.
 * At most 'n' bytes are copied (including null-terminator).
 *
 * Returns 0 if successful, or -1 if failed.
*/
extern int argncpy(char *dst, size_t n, char *src, char *sep, int arg, int opt);

/* Returns the length of an argument (not including null-terminator).
 * On failure returns -1.
*/
extern int arglen(char *src, char *sep, int arg, int opt);

/* Returns a copy of an argument. The memory is allocated with malloc()
 * and must be free()'ed when done.
 *
 * Returns NULL on failure.
*/
extern char *argdup(char *src, char *sep, int arg, int opt);

/* Returns the number of parseable arguments. */
extern int argcount(char *src, char *sep, int opt);

/* Converts the argument to a long integer. */
extern long argtol(char *src, char *sep, int arg, int opt, int base);

/* Helpers. */
extern int parse_font(char *src, char **font, int *fontsize);

/* Strips the string *str of any preceeding or trailing
 * characters that are present in the string *ws. */
extern char *strtrim(char *str, char *ws);

#endif

