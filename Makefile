MCS = mcs
MCSFLAGS = 
CSGENOPTS =
RUNTIME = mono
# MCS = csc
# MCSFLAGS = /d:WIN32
# CSGENOPTS = WIN32
# RUNTIME =
CORE_FILES = `cat CORE_files`
EXT_FILES = `cat EXT_files`

all: TsunamiBindingsGl.dll

TsunamiBindingsGl.dll: TsunamiBindingsGl-pre.dll gl_postprocess.exe
	$(RUNTIME) ./gl_postprocess.exe TsunamiBindingsGl-pre.dll TsunamiBindingsGl.dll

gl_postprocess.exe: gl_postprocess.cs TsunamiBindingsCore.dll
	$(MCS) $(MCSFLAGS) /r:TsunamiBindingsCore.dll gl_postprocess.cs

TsunamiBindingsCore.dll: TsunamiBindingsCore.cs
	$(MCS) $(MCSFLAGS) /target:library TsunamiBindingsCore.cs

TsunamiBindingsGl-pre.dll: TsunamiBindingsGl-pre.cs TsunamiBindingsCore.dll
	$(MCS) $(MCSFLAGS) /target:library /unsafe /r:TsunamiBindingsCore.dll TsunamiBindingsGl-pre.cs

TsunamiBindingsGl-pre.cs: TsunamiBindingsGl-part.cs
	cat gl_preamble.cs TsunamiBindingsGl-part.cs gl_postamble.cs > TsunamiBindingsGl-pre.cs

TsunamiBindingsGl-part.cs: csgen.pl
	perl csgen.pl $(CSGENOPTS) CORE $(CORE_FILES) EXT $(EXT_FILES) > TsunamiBindingsGl-part.cs

clean:
	rm -f gl_postprocess.exe TsunamiBindingsCore.dll TsunamiBindingsGl-pre.dll TsunamiBindingsGl-pre.cs TsunamiBindingsGl-part.cs TsunamiBindingsGl.dll

