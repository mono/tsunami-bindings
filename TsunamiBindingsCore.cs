//
//  Tsunami.Bindings core
//
//  Copyright (c) 2004  Vladimir Vukicevic  <vladimir@pobox.com>
//
//  Tsunami.Bindings code and DLLs are licensed under the MIT/X11
//  license.  Please see the file MIT.X11 for more information.
//

using System;
using System.Collections;
using System.Runtime.InteropServices;
using System.Reflection;

namespace Tsunami.Bindings {
  public class GlDetails {
#if WIN32
    public const string GL_NATIVE_LIBRARY = "opengl32.dll";
    public const string GL_EXTENSION_QUERY_PROC = "wglGetProcAddress";
    public const CallingConvention GL_NATIVE_CALLCONV = CallingConvention.StdCall;
#elif MACOSX
// I have no idea
//  public const string GL_NATIVE_LIBRARY = ;
//  public const string GL_EXTENSION_QUERY_PROC = "aglGetProcAddress";
//  public const CallingConvention GL_NATIVE_CALLCONV = CallingConvention.Cdecl;
#else
    public const string GL_NATIVE_LIBRARY = "libGL.so";
    public const string GL_EXTENSION_QUERY_PROC = "glXGetProcAddressARB";
    public const CallingConvention GL_NATIVE_CALLCONV = CallingConvention.Cdecl;
#endif
  }

  [AttributeUsage(AttributeTargets.Method)]
  public class OpenGLExtensionImport : Attribute {
    public string ExtensionName;
    public string EntryPoint;

    public OpenGLExtensionImport (string ExtensionName, string EntryPoint) {
      this.ExtensionName = ExtensionName;
      this.EntryPoint = EntryPoint;
    }
  }

  public class GlExtensionLoader {
    private static Type tsunamigl;
    private static GlExtensionLoader loaderInst;

    private Hashtable loadedExtensions;
    private Hashtable loadedFunctions;

    public static GlExtensionLoader GetInstance() {
      if (loaderInst == null) {
	loaderInst = new GlExtensionLoader();
      }
      return loaderInst;
    }

    [DllImport(GlDetails.GL_NATIVE_LIBRARY, EntryPoint=GlDetails.GL_EXTENSION_QUERY_PROC, CallingConvention=GlDetails.GL_NATIVE_CALLCONV)]
    internal static extern IntPtr GetProcAddress(string s);

    protected GlExtensionLoader () {
      loadedExtensions = new Hashtable();
      loadedFunctions = new Hashtable();
    }

    public bool LoadExtension (string extname) {
      if (loadedExtensions.ContainsKey (extname)) {
	return (bool) loadedExtensions[extname];
      }

      bool result = true;

      Type glt = null;
      Assembly [] asss = AppDomain.CurrentDomain.GetAssemblies();
      foreach (Assembly a in asss) {
	glt = a.GetType("Tsunami.Bindings.Gl");
	if (glt != null)
	  break;
      }

      if (glt == null) {
	Console.WriteLine ("Couldn't find Tsunami.Bindings.Gl assembly in current AppDomain!");
	return false;
      }

      MethodInfo [] mis = glt.GetMethods (BindingFlags.Public |
					  BindingFlags.Static |
					  BindingFlags.DeclaredOnly);
      foreach (MethodInfo mi in mis) {
	object [] atts = mi.GetCustomAttributes (typeof(OpenGLExtensionImport), false);
	if (atts.Length == 0) {
	  continue;
	}

	OpenGLExtensionImport oglext = (OpenGLExtensionImport) atts[0];
	if (oglext.ExtensionName == extname) {
	  string fieldname = "ext__" + extname + "__" + oglext.EntryPoint;
	  if (loadedFunctions.ContainsKey (fieldname)) {
	    continue;
	  }

	  Console.WriteLine ("Loading " + oglext.EntryPoint + " for " + extname);
	  FieldInfo fi = glt.GetField (fieldname,
				       BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);
	  if (fi == null) {
	    Console.WriteLine ("Can't get extension field!");
	    continue;
	  }

	  IntPtr procaddr = GetProcAddress (oglext.EntryPoint);
	  if (procaddr == IntPtr.Zero) {
	    Console.WriteLine ("Failed for " + oglext.EntryPoint);
	    result = false;
	    break;
	  }

	  fi.SetValue (glt, procaddr);
	  loadedFunctions[fieldname] = true;
	}
      }

      loadedExtensions[extname] = result;
      return result;
    }
  }
}
