//
//  Tsunami.Bindings.Gl
//
//  Copyright (c) 2004  Vladimir Vukicevic  <vladimir@pobox.com>
//
//  Tsunami.Bindings code and DLLs are licensed under the MIT/X11
//  license.  Please see the file MIT.X11 for more information.
//

using System;
using System.Runtime.InteropServices;

using GLenum = System.UInt32;
using GLboolean = System.Boolean;
using GLbitfield = System.UInt32;
using GLbyte = System.Byte;
using GLubyte = System.Byte;
using GLshort = System.Int16;
using GLushort = System.UInt16;
using GLint = System.Int32;
using GLuint = System.UInt32;
using GLsizei = System.Int32;
using GLfloat = System.Single;
using GLclampf = System.Single;
using GLdouble = System.Double;
using GLclampd = System.Double;

//using GLsizeiptr = System.IntPtr;
//using GLintptr = System.IntPtr;
//using GLsizeiptrARB = System.IntPtr;
//using GLintptrARB = System.IntPtr;
using GLsizeiptr = System.UInt32;
using GLintptr = System.UInt32;
using GLsizeiptrARB = System.UInt32;
using GLintptrARB = System.UInt32;
using GLhalf = System.UInt16;
using GLchar = System.Byte;
using GLcharARB = System.Byte;
using GLhandle = System.Int32;
using GLhandleARB = System.Int32;

namespace Tsunami.Bindings {

  public sealed class Gl {
