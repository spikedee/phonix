This file contains build instructions and requirements for building Phonix.

The Phonix build assumes that you have the usual suite of Linux and GNU dev
utils, which are standard on almost any distribution of Linux or POSIX these
days. Aside from those, you need the following:

* A Java Runtime v. 1.5 or greater

* The Mono .NET runtime and build tools, including gmcs

* The NUnit framework for running unit tests

There are debian packages that install all of these things, so if you're
running any debian variant you shouldn't have any problem. If something is
broken in your build, make sure that you have all of these things before
continuing.

Once you have all of the requirements, building should be very simple:

make 
make test 
sudo make install

There are a variety of other targets in the Makefile, but you probably won't
need them except under unusual circumstances. Note that the "install" target is
kind of shaky right now, and might fail if you have an unusual system
configuration. Also note that the relative paths in the Makefile all assume
that you are building from the root. Running "make" in any subdirectory is not
supported. Since the directory structure is currently very flat, this is rarely
a problem.
