namespace GLDemo {
    using System;
    using System.Runtime.InteropServices;
    using Tsunami.Bindings;
    using Tao.Sdl;

    public class GearsDemo {

	private static double rotx = 20.0;                       // View's X-Axis Rotation
	private static double roty = 30.0;                       // View's Y-Axis Rotation
	private static double rotz = 0.0;                        // View's Z-Axis Rotation
	private static uint gear1;                               // Display List For Red Gear
	private static uint gear2;                               // Display List For Green Gear
	private static uint gear3;                               // Display List For Blue Gear
	private static float rAngle = 0.0f;                      // Rotation Angle
	private static float[] pos = {5.0f, 5.0f, 10.0f, 0.0f};  // Light Position
	private static float[] red = {0.8f, 0.1f, 0.0f, 1.0f};   // Red Material
	private static float[] green = {0.0f, 0.8f, 0.2f, 1.0f}; // Green Material
	private static float[] blue = {0.2f, 0.2f, 1.0f, 1.0f};  // Blue Material

	private static int time = Environment.TickCount;
	private static int frame = 0;

	public static int Main (string[] args)
	{
	    if (Sdl.SDL_Init (Sdl.SDL_INIT_VIDEO | Sdl.SDL_INIT_TIMER | Sdl.SDL_INIT_NOPARACHUTE) == -1)
	    	throw new InvalidOperationException();

	    Sdl.SDL_GL_SetAttribute (Sdl.SDL_GLattr.SDL_GL_RED_SIZE, 8);
	    Sdl.SDL_GL_SetAttribute (Sdl.SDL_GLattr.SDL_GL_GREEN_SIZE, 8);
	    Sdl.SDL_GL_SetAttribute (Sdl.SDL_GLattr.SDL_GL_BLUE_SIZE, 8);
	    Sdl.SDL_GL_SetAttribute (Sdl.SDL_GLattr.SDL_GL_DEPTH_SIZE, 16);
	    Sdl.SDL_GL_SetAttribute (Sdl.SDL_GLattr.SDL_GL_DOUBLEBUFFER, 0);

	    Sdl.SDL_ShowCursor (Sdl.SDL_ENABLE);

	    System.IntPtr win = Sdl.SDL_SetVideoMode (300, 300, 24, Sdl.SDL_HWSURFACE | Sdl.SDL_OPENGL);
	    if (win == IntPtr.Zero)
	        throw new InvalidOperationException();

	    MakeGears();

	    Gl.Enable(Gl.CULL_FACE);
	    Gl.Enable(Gl.LIGHTING);
	    Gl.Lightfv(Gl.LIGHT0, Gl.POSITION, pos);
	    Gl.Enable(Gl.LIGHT0);
	    Gl.Enable(Gl.DEPTH_TEST);

	    Gl.Enable(Gl.NORMALIZE);

	    MainLoop();

	    return 0;
	}

	public static void MainLoop() {
	    bool quitting = false;
	    Sdl.SDL_Event ev;

	    while (!quitting) {
		while (Sdl.SDL_PollEvent (out ev) != 0) {
		    if (ev.type == Sdl.SDL_KEYDOWN &&
			ev.key.keysym.sym == Sdl.SDLK_ESCAPE)
			{
			    quitting = true;
			}
		}

		mydisp ();
	    }

	    Sdl.SDL_Quit();
	}

	public static void mydisp ()
	{
		Gl.Clear(Gl.COLOR_BUFFER_BIT | Gl.DEPTH_BUFFER_BIT);

		Gl.PushMatrix();
		Gl.Rotated(rotx, 1.0, 0.0, 0.0);                 // Position The World
		Gl.Rotated(roty, 0.0, 1.0, 0.0);
		Gl.Rotated(rotz, 0.0, 0.0, 1.0);

		Gl.PushMatrix();
		Gl.Translated(-3.0, -2.0, 0.0);                  // Position The Red Gear
		Gl.Rotatef(rAngle, 0.0f, 0.0f, 1.0f);            // Rotate The Red Gear
		Gl.Color3f(0.8f, 0.2f, 0.0f);
		Gl.CallList(gear1);                              // Draw The Red Gear
		Gl.PopMatrix();

		Gl.PushMatrix();
		Gl.Translated(3.1, -2.0, 0.0);                   // Position The Green Gear
		Gl.Rotated(-2.0 * rAngle - 9.0, 0.0, 0.0, 1.0);  // Rotate The Green Gear
		Gl.Color3f(0.0f, 0.8f, 0.2f);
		Gl.CallList(gear2);                              // Draw The Green Gear
		Gl.PopMatrix();

		Gl.PushMatrix();
		Gl.Translated(-3.1, 4.2, 0.0);                   // Position The Blue Gear
		Gl.Rotated(-2.0 * rAngle - 25.0, 0.0, 0.0, 1.0); // Rotate The Blue Gear
		Gl.Color3f(0.2f, 0.2f, 1.0f);
		Gl.CallList(gear3);                              // Draw The Blue Gear
		Gl.PopMatrix();
		Gl.PopMatrix();

		rAngle += 0.2f;                                 // Increase The Rotation

		Sdl.SDL_GL_SwapBuffers ();

		float tmp = (Environment.TickCount - time) / 1000.0f;
		float fps = frame / tmp;
		if (tmp >= 5) {
		    Console.Write("FPS: {0}\n", fps);
		    time = Environment.TickCount;
		    frame = 0;
		}
		frame++;
	}

	public static void reshape (int width, int height)
	{

	    float h = (float) height / (float) width;

	    Gl.Viewport(0, 0, width,  height);
	    Gl.MatrixMode(Gl.PROJECTION);
	    Gl.LoadIdentity();
	    Gl.Frustum(-1.0, 1.0, -h, h, 5.0, 60.0);
	    Gl.MatrixMode(Gl.MODELVIEW);
	    Gl.LoadIdentity();
	    Gl.Translatef(0.0f, 0.0f, -40.0f);
	}

	private static void MakeGear(double inner_radius, double outer_radius, double width, int teeth, double tooth_depth) {
	    int i;
	    double r0;
	    double r1;
	    double r2;
	    double angle;
	    double da;
	    double u;
	    double v;
	    double len;

	    r0 = inner_radius;
	    r1 = outer_radius - tooth_depth / 2.0;
	    r2 = outer_radius + tooth_depth / 2.0;

	    da = 2.0 * Math.PI / teeth / 4.0;
	    Gl.ShadeModel(Gl.FLAT);

	    Gl.Normal3d(0.0, 0.0, 1.0);

	    /* draw front face */
	    Gl.Begin(Gl.QUAD_STRIP);
	    for(i = 0; i <= teeth; i++) {
		angle = i * 2.0 * Math.PI / teeth;
		Gl.Vertex3d(r0 * Math.Cos(angle), r0 * Math.Sin(angle), width * 0.5);
		Gl.Vertex3d(r1 * Math.Cos(angle), r1 * Math.Sin(angle), width * 0.5);
		Gl.Vertex3d(r0 * Math.Cos(angle), r0 * Math.Sin(angle), width * 0.5);
		Gl.Vertex3d(r1 * Math.Cos(angle + 3 * da), r1 * Math.Sin(angle + 3 * da), width * 0.5);
	    }
	    Gl.End();

	    /* draw front sides of teeth */
	    Gl.Begin(Gl.QUADS);
	    da = 2.0 * Math.PI / teeth / 4.0;
	    for(i = 0; i < teeth; i++) {
		angle = i * 2.0 * Math.PI / teeth;

		Gl.Vertex3d(r1 * Math.Cos(angle), r1 * Math.Sin(angle), width * 0.5);
		Gl.Vertex3d(r2 * Math.Cos(angle + da), r2 * Math.Sin(angle + da), width * 0.5);
		Gl.Vertex3d(r2 * Math.Cos(angle + 2 * da), r2 * Math.Sin(angle + 2 * da), width * 0.5);
		Gl.Vertex3d(r1 * Math.Cos(angle + 3 * da), r1 *Math.Sin(angle + 3 * da), width * 0.5);
	    }
	    Gl.End();

	    Gl.Normal3d(0.0, 0.0, -1.0);

	    /* draw back face */
	    Gl.Begin(Gl.QUAD_STRIP);
	    for(i = 0; i <= teeth; i++) {
		angle = i * 2.0 * Math.PI / teeth;
		Gl.Vertex3d(r1 * Math.Cos(angle), r1 * Math.Sin(angle), -width * 0.5);
		Gl.Vertex3d(r0 * Math.Cos(angle), r0 * Math.Sin(angle), -width * 0.5);
		Gl.Vertex3d(r1 * Math.Cos(angle + 3 * da), r1 * Math.Sin(angle + 3 * da), -width * 0.5);
		Gl.Vertex3d(r0 * Math.Cos(angle), r0 * Math.Sin(angle), -width * 0.5);
	    }
	    Gl.End();

	    /* draw back sides of teeth */
	    Gl.Begin(Gl.QUADS);
	    da = 2.0 * Math.PI / teeth / 4.0;
	    for(i = 0; i < teeth; i++) {
		angle = i * 2.0 * Math.PI / teeth;

		Gl.Vertex3d(r1 * Math.Cos(angle + 3 * da), r1 * Math.Sin(angle + 3 * da), -width * 0.5);
		Gl.Vertex3d(r2 * Math.Cos(angle + 2 * da), r2 * Math.Sin(angle + 2 * da), -width * 0.5);
		Gl.Vertex3d(r2 * Math.Cos(angle + da), r2 * Math.Sin(angle + da), -width * 0.5);
		Gl.Vertex3d(r1 * Math.Cos(angle), r1 * Math.Sin(angle), -width * 0.5);
	    }
	    Gl.End();

	    /* draw outward faces of teeth */
	    Gl.Begin(Gl.QUAD_STRIP);
	    for(i = 0; i < teeth; i++) {
		angle = i * 2.0 * Math.PI / teeth;

		Gl.Vertex3d(r1 * Math.Cos(angle), r1 * Math.Sin(angle), width * 0.5);
		Gl.Vertex3d(r1 * Math.Cos(angle), r1 * Math.Sin(angle), -width * 0.5);
		u = r2 * Math.Cos(angle + da) - r1 * Math.Cos(angle);
		v = r2 * Math.Sin(angle + da) - r1 * Math.Sin(angle);
		len = Math.Sqrt(u * u + v * v);
		u /= len;
		v /= len;
		Gl.Normal3d(v, -u, 0.0);
		Gl.Vertex3d(r2 * Math.Cos(angle + da), r2 * Math.Sin(angle + da), width * 0.5);
		Gl.Vertex3d(r2 * Math.Cos(angle + da), r2 * Math.Sin(angle + da), -width * 0.5);
		Gl.Normal3d(Math.Cos(angle), Math.Sin(angle), 0.0);
		Gl.Vertex3d(r2 * Math.Cos(angle + 2 * da), r2 * Math.Sin(angle + 2 * da), width * 0.5);
		Gl.Vertex3d(r2 * Math.Cos(angle + 2 * da), r2 * Math.Sin(angle + 2 * da), -width * 0.5);
		u = r1 * Math.Cos(angle + 3 * da) - r2 * Math.Cos(angle + 2 * da);
		v = r1 * Math.Sin(angle + 3 * da) - r2 * Math.Sin(angle + 2 * da);
		Gl.Normal3d(v, -u, 0.0);
		Gl.Vertex3d(r1 * Math.Cos(angle + 3 * da), r1 * Math.Sin(angle + 3 * da), width * 0.5);
		Gl.Vertex3d(r1 * Math.Cos(angle + 3 * da), r1 * Math.Sin(angle + 3 * da), -width * 0.5);
		Gl.Normal3d(Math.Cos(angle), Math.Sin(angle), 0.0);
	    }

	    Gl.Vertex3d(r1 * Math.Cos(0), r1 * Math.Sin(0), width * 0.5);
	    Gl.Vertex3d(r1 * Math.Cos(0), r1 * Math.Sin(0), -width * 0.5);

	    Gl.End();

	    Gl.ShadeModel(Gl.SMOOTH);

	    /* draw inside radius cylinder */
	    Gl.Begin(Gl.QUAD_STRIP);
	    for(i = 0; i <= teeth; i++) {
		angle = i * 2.0 * Math.PI / teeth;
		Gl.Normal3d(-Math.Cos(angle), -Math.Sin(angle), 0.0);
		Gl.Vertex3d(r0 * Math.Cos(angle), r0 * Math.Sin(angle), -width * 0.5);
		Gl.Vertex3d(r0 * Math.Cos(angle), r0 * Math.Sin(angle), width * 0.5);
	    }
	    Gl.End();
	}

	private static void MakeGears() {
	    // Make The Gears
	    gear1 = Gl.GenLists(1);                                                  // Generate A Display List For The Red Gear
	    Gl.NewList(gear1, Gl.COMPILE);                                           // Create The Display List
	    Gl.Materialfv(Gl.FRONT, Gl.AMBIENT_AND_DIFFUSE, red);                    // Create A Red Material
	    Gl.Color3f(0.8f, 0.2f, 0.0f);
	    MakeGear(1.0, 4.0, 1.0, 20, 0.7);                                       // Make The Gear
	    Gl.EndList();                                                            // Done Building The Red Gear's Display List

	    gear2 = Gl.GenLists(1);                                                  // Generate A Display List For The Green Gear
	    Gl.NewList(gear2, Gl.COMPILE);                                           // Create The Display List
	    Gl.Materialfv(Gl.FRONT, Gl.AMBIENT_AND_DIFFUSE, green);                  // Create A Green Material
	    Gl.Color3f(0.0f, 0.8f, 0.2f);
	    MakeGear(0.5, 2.0, 2.0, 10, 0.7);                                       // Make The Gear
	    Gl.EndList();                                                            // Done Building The Green Gear's Display List

	    gear3 = Gl.GenLists(1);                                                  // Generate A Display List For The Blue Gear
	    Gl.NewList(gear3, Gl.COMPILE);                                           // Create The Display List
	    Gl.Materialfv(Gl.FRONT, Gl.AMBIENT_AND_DIFFUSE, blue);                   // Create A Blue Material
	    Gl.Color3f(0.2f, 0.2f, 1.0f);
	    MakeGear(1.3, 2.0, 0.5, 10, 0.7);                                       // Make The Gear
	    Gl.EndList();                                                            // Done Building The Blue Gear's Display List
	}

    }
}
