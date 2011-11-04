#!/bin/sh

DIR=`dirname $0`
exec mono $DIR/../lib/phonix/phonix.exe $@
