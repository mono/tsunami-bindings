//
//  gl_postprocess.cs
//
//  Copyright (C) 2004  Vladimir Vukicevic  <vladimir@pobox.com>
//
//  This file is licensed under the GNU GPL; for more information,
//  see the file COPYING.GPL
//


using System;
using System.Collections;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;

public class GLPostProcess {

  // duplicated from GlDetails due to a mcs bug
#if WIN32
    public const string GL_NATIVE_LIBRARY = "opengl32.dll";
    public const CallingConvention GL_NATIVE_CALLCONV = CallingConvention.StdCall;
#elif MACOSX
// I have no idea
//  public const string GL_NATIVE_LIBRARY = ;
//  public const CallingConvention GL_NATIVE_CALLCONV = CallingConvention.Cdecl;
#else
    public const string GL_NATIVE_LIBRARY = "libGL.so";
    public const CallingConvention GL_NATIVE_CALLCONV = CallingConvention.Cdecl;
#endif

  public static void Main(string [] args) {
    string inName = args[0];
    string outName = args[1];

#if WIN32
    // The MS runtime doesn't support a bunch of queries on
    // dynamic modules, so we have to track this stuff ourselves
    Hashtable field_hash = new Hashtable();
#endif
    
    AssemblyName outAsName = new AssemblyName();
    outAsName.Name = "TsunamiBindingsGl";

    AssemblyBuilder abuilder = AppDomain.CurrentDomain.DefineDynamicAssembly
      (outAsName, AssemblyBuilderAccess.Save);
    ModuleBuilder mbuilder = abuilder.DefineDynamicModule(outName, outName);
    TypeBuilder glbuilder = mbuilder.DefineType("Tsunami.Bindings.Gl",
						TypeAttributes.Public |
						TypeAttributes.Class |
    						TypeAttributes.Sealed);

    Assembly inputAssembly = Assembly.LoadFrom(inName);
    Type gltype = inputAssembly.GetType("Tsunami.Bindings.Gl");
    MemberInfo [] glMembers = gltype.GetMembers(BindingFlags.Instance |
						BindingFlags.Static |
						BindingFlags.Public |
						BindingFlags.NonPublic |
						BindingFlags.DeclaredOnly);

    foreach (MemberInfo qi in glMembers) {

      // Fields
      FieldInfo fi = qi as FieldInfo;
      if (fi != null) {
	// Console.WriteLine ("FIELD: " + fi.Name);
	FieldBuilder fb = glbuilder.DefineField (fi.Name, fi.FieldType, fi.Attributes);
	if (fi.FieldType != typeof(System.IntPtr)) {
	  fb.SetConstant (fi.GetValue (gltype));
	} else {
#if WIN32
	  // this is a slot to hold an extension addr,
	  // so we save it
	  field_hash[fi.Name] = fb;
#endif
	}
	continue;
      }

      // Methods
      MethodInfo mi = qi as MethodInfo;
      if (mi != null) {
	bool is_ext;
	bool is_dll;
	object [] extattrs = mi.GetCustomAttributes (typeof(Tsunami.Bindings.OpenGLExtensionImport), false);

	is_ext = (extattrs.Length > 0);
	is_dll = (mi.Attributes & MethodAttributes.PinvokeImpl) != 0;

	ParameterInfo [] parms = mi.GetParameters();
	Type [] methodSig = new Type [parms.Length];
	for (int i = 0; i < parms.Length; i++) {
	  methodSig[i] = parms[i].ParameterType;
	}

	if (is_ext && is_dll) {
	  throw new InvalidOperationException ("Something can't be both ext and dll");
	}

	if (is_dll) {
	  // this is a normal DLL import'd method
	  // Console.WriteLine ("DLL import method: " + mi.Name);
	  MethodBuilder mb = glbuilder.DefinePInvokeMethod (mi.Name, GL_NATIVE_LIBRARY, "gl" + mi.Name,
							    mi.Attributes,
							    CallingConventions.Standard,
							    mi.ReturnType, methodSig,
							    GL_NATIVE_CALLCONV,
							    CharSet.Ansi);
	} else if (is_ext) {
	  // this is an OpenGLExtensionImport method
	  // Console.WriteLine ("OpenGLExtensionImport method: " + mi.Name);
	  Tsunami.Bindings.OpenGLExtensionImport ogl = extattrs[0] as Tsunami.Bindings.OpenGLExtensionImport;

	  MethodBuilder mb = glbuilder.DefineMethod (mi.Name, mi.Attributes, mi.ReturnType, methodSig);
	  // put the custom attribute back, so that we can reference it
	  // at runtime for LoadExtension
	  mb.SetCustomAttribute (CreateGLExtCAB (ogl.ExtensionName, ogl.EntryPoint));
	  // now build the IL
	  string fieldname = "ext__" + ogl.ExtensionName + "__" + ogl.EntryPoint;
#if WIN32
	  FieldInfo addrfield = field_hash[fieldname] as FieldInfo;
#else
	  FieldInfo addrfield = glbuilder.GetField(fieldname,
						   BindingFlags.Instance |
						   BindingFlags.Static |
						   BindingFlags.Public |
						   BindingFlags.NonPublic |
						   BindingFlags.DeclaredOnly);
#endif

	  ILGenerator ilg = mb.GetILGenerator();
	  {
	    int numargs = methodSig.Length;
	    for (int i = 0; i < numargs; i++) {
	      switch (i) {
	      case 0: ilg.Emit(OpCodes.Ldarg_0); break;
	      case 1: ilg.Emit(OpCodes.Ldarg_1); break;
	      case 2: ilg.Emit(OpCodes.Ldarg_2); break;
	      case 3: ilg.Emit(OpCodes.Ldarg_3); break;
	      default:ilg.Emit(OpCodes.Ldarg_S, i); break;
	      }
	    }
	    ilg.Emit(OpCodes.Ldsfld, addrfield);
	    ilg.EmitCalli(OpCodes.Calli, GL_NATIVE_CALLCONV, mi.ReturnType, methodSig);
	  }
	  ilg.Emit(OpCodes.Ret);
	} else {
	  // this is a normal method
	  // this shouldn't happen
	  Console.WriteLine ("WARNING: Skipping non-DLL and non-Ogl method " + mi.Name);
	}

	continue;
      }
    }

    glbuilder.CreateType();
    mbuilder.CreateGlobalFunctions();

    abuilder.Save(outName);
  }

  static CustomAttributeBuilder CreateGLExtCAB (string extname, string procname) {
    Type [] ctorParams = new Type [] { typeof(string), typeof(string) };
    ConstructorInfo classCtorInfo = typeof(Tsunami.Bindings.OpenGLExtensionImport).GetConstructor (ctorParams);
    CustomAttributeBuilder cab = new CustomAttributeBuilder (classCtorInfo,
							     new object [] { extname, procname } );
    return cab;
  }
}
