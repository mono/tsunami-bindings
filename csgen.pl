##
## csgen.pl
##  Copyright (C) 2004  Vladimir Vukicevic  <vladimir@pobox.com>
##
## Portions taken from GLEW (http://glew.sourceforge.net/), which is
##  Copyright (C) 2004, 2003 Marcelo E. Magallon <mmagallo[at]debian org>
##  Copyright (C) 2004, 2003 Milan Ikits <milan ikits[at]ieee org>
##
## This program is distributed under the terms and conditions of the GNU
## General Public License Version 2 as published by the Free Software
## Foundation or, at your option, any later version.
##
## Please see the file "COPYING" for more information
##
use Data::Dumper;

$strip_prefix = 1;
#$cconv = "GlDetails.GL_NATIVE_CALLCONV";
$cconv = "CallingConvention.Cdecl";

my %regex = (extname  => qr/^[A-Z][A-Za-z0-9_]+$/,
	     exturl   => qr/^http.+$/,
	     function => qr/^(.+) ([a-z][a-z0-9_]*) \((.+)\)( *= .*)?$/i, 
	     token    => qr/^([A-Z][A-Z0-9_]*)\s+((?:0x)?[0-9A-F]+|[A-Z][A-Z0-9_]*)$/,
	     type     => qr/^typedef\s+(.+)\s+([\*A-Za-z0-9_]+)$/,
	     exact    => qr/.*;$/,
	    );

my %typemap = ('GLenum'		=> 'unsigned int',
	       'GLboolean'	=> 'bool', # uchar
	       'GLbitfield'	=> 'unsigned int',
	       'GLvoid'		=> 'void',
	       'GLbyte'		=> 'byte',
	       'GLshort'	=> 'short',
	       'GLint'		=> 'int',
	       'GLubyte'	=> 'unsigned byte',
	       'GLushort'	=> 'unsigned short',
	       'GLuint'		=> 'unsigned int',
	       'GLsizei'	=> 'int',
	       'GLfloat'	=> 'float',
	       'GLdouble'	=> 'double',
	       'GLclampd'	=> 'double',

	       'GLsizeiptrARB'	=> 'IntPtr',
	       'GLintptrARB'	=> 'IntPtr',
	       'GLhalf'		=> 'unsigned short',
	       'GLcharARB'	=> 'byte',
	       'GLhandleARB'	=> 'int',


	       'GLXContextID'	=> 'int',
	       'GLXFBConfigIDSGIX' => 'int',
	       'GLXPbufferSGIX'	=> 'int',
);

sub parse_ext($)
{
  my $filename = shift;
  my @functions;
  my %tokens = ();
  my %types = ();
  my @exacts = ();
  my $extname = "";
  my $exturl = "";

  open EXT, "<$filename" or return;

  while (<EXT>) {
    chomp;
    if (/$regex{extname}/) {
      $extname = $_;
      next;
    } elsif (/$regex{exturl}/) {
      $exturl = $_;
    } elsif (s/^\s+//) {
      if (/$regex{exact}/) {
	push @exacts, $_;
      } elsif (/$regex{type}/) {
	my ($value, $name) = ($1, $2);
	$types{$name} = $value;
      } elsif (/$regex{token}/) {
	my ($name, $value) = ($1, $2);
	$tokens{$name} = $value;
      } elsif (/$regex{function}/) {
	my ($return, $name, $parms, $realname) = ($1, $2, $3, $4);

	$realname =~ s/^ *= *//;
	$realname =~ s/ *$//;

	if ($realname eq "") {
	  $realname = $name;
	}

	push (@functions, {name => $name,
			   rtype => $return,
			   parms => $parms,
			   realname => $realname,
			  });
      }
    }
  }

  close EXT;

  return ($extname, $exturl, \%types, \%tokens, \@functions, \@exacts);
}

sub output_header ($$)
{
  $extname = shift;
  $exturl = shift;

  print "    //\n";
  print "    // $extname\n";
  print "    // $exturl\n";
  print "    //\n";
}

%enum_hash = ();

sub output_enum ($$)
{
  $name = shift;
  $val = shift;

  if ($strip_prefix && !($name =~ /^GL_[0-9]/)) {
    $name =~ s/^GL_//;
  }

  if ($strip_prefix && !($val =~ /^GL_[0-9]/)) {
    $val =~ s/^GL_//;
  }

  if (defined($enum_hash{$name})) {
    if (!($enum_hash{$name} eq $val)) {
      print STDERR "$name previously defined as %enum_hash{$name} and now $val\n";
    }
    return;
  }

  $enum_hash{$name} = $val;
  print "    public const uint " . $name . " = " . $val . ";\n";
}

%seenextfields = ();

sub output_func ($$)
{
  my $extname = shift;
  my $fdata = shift;

  $fname = $fdata->{'name'};
  $usefname = $fname;
  $frtype = $fdata->{'rtype'};
  $fallparms = $fdata->{'parms'};
  $unsafe = "";

  # fix up some things that are keywords, argh
  $fallparms =~ s/GLvoid/void/g;
  $fallparms =~ s/const//g;
  $fallparms =~ s/params/paramz/g;
  $fallparms =~ s/ ref,/ glref,/g;
  $fallparms =~ s/ ref *$/ glref/g;
  $fallparms =~ s/ base/ glbase/g;
  $fallparms =~ s/ out,/ glout,/g;
  $fallparms =~ s/ out *$/ glout/g;
  $fallparms =~ s/ in,/ glin,/g;
  $fallparms =~ s/ in *$/ glin/g;
  $fallparms =~ s/ object/ globj/g;
  $fallparms =~ s/ string/ glstring/g;
  # sigh
  $fallparms =~ s/ m\[16\]/ *m/g;
  $fallparms =~ s/ v\[\]/ *v/g;

  # this function leaves off a name for the last parameter.
  # sigh++
  if ($usefname eq "glWindowPos4dMESA") {
    $fallparms .= " w";
  }

  # nuke const's from the resturn type
  $frtype =~ s/const//g;
  # change GLvoid's to voids, as void can't
  # be aliased
  $frtype =~ s/GLvoid/void/g;

  if ($strip_prefix) {
    $usefname =~ s/^gl//;
  }

  if (defined $fdata->{'realname'}) {
    $fname = $fdata->{'realname'};
  }

  # if the entire param list is void, then we "" it
  if ($fallparms =~ /^ *void *$/i) {
    $fallparms = "";
  }

  if ($fallparms =~ m/\*/ || $frtype =~ m/\*/) {
    $unsafe = "unsafe";
  }

  ## The idea here is to automatically take things like
  ## "GLfloat *f" and spit out bindings for
  ## "GLfloat *f" and "GLfloat [] f"; also
  ## if the name ended in something like "3fv", spit out
  ## "ref Vector3 v" too.  But, it's probably easiest to
  ## just do these by hand, especially since we don't
  ## really care to support glVertex*, glNormal*, etc.
  if (0) {
    # now rebuild the param lists
    @paramlists = [];
    @fparms = split (/, */, $fallparms);

    foreach my $fp (@fparms) {
      # A little gross -- we want types in $ft, but * placement
      # is inconsistent.. so we strip any leading *'s from $fn
      # and tack them onto the end of $ft
      $fp =~ /(.*) ([^ ]*)/;
      ($ft, $fn) = ($1, $2);
      if ($fn =~ /^(\*+)(.*)/) {
	$ft .= $1;
	$fn = $2;
      }

      $ft =~ /( *\*+)*$/;
      $stars = $1;
    }
  }

  # if extname is 0, it means it's a core GL function
  # and shouldn't get ExtensionAttribute info, but instead
  # a straight DllImport
  if ($extname eq "CORE") {
    print "    [DllImport(GlDetails.GL_NATIVE_LIBRARY, EntryPoint=\"$fname\", CallingConvention=$cconv, ExactSpelling=true)]\n";
    print "    public static extern $unsafe $frtype $usefname ($fallparms);\n";
  } else {
    # else this is an extension, so we need to not have a DllImport, but an attribute here
    $extfield = "ext__" . $extname . "__" . $fname;
    if (!defined($seenextfields{$extfield})) {
      print "    public static IntPtr $extfield = IntPtr.Zero;\n";
      $seenextfields{$extfield} = 1;
    }

    print "    [OpenGLExtensionImport(\"$extname\", \"$fname\")]\n";
    print "    public static $unsafe $frtype $usefname ($fallparms) {\n";
    print "        throw new InvalidOperationException(\"binding error\");\n";
    print "    }\n";
  }


  print "\n";
}

##
## main
##

$is_core = 0;

foreach my $extfile (@ARGV)
{
  if ($extfile eq "WIN32") {
    $cconv = "CallingConvention.StdCall";
    next;
  } elsif ($extfile eq "CORE") {
    $is_core = 1;
    next;
  } elsif ($extfile eq "EXT") {
    $is_core = 0;
    next;
  }

  my ($extname, $exturl, $types, $tokens, $funcs, $exacts) = parse_ext($extfile);

  # write a header
  output_header ($extname, $exturl);

  print "\n";

  # write the constant tokens
  foreach my $tok (keys %$tokens) {
    output_enum ($tok, $tokens->{$tok});
  }

  print "\n";

  # now write funcs
  foreach my $func (@$funcs) {
    output_func ($is_core ? "CORE" : $extname, $func);
  }
}
