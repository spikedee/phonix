# Look at how awesome and short my makefile is

VERSION=0.8.0
TARFILE=phonix.$(VERSION).tar
DEB_DIR=phonix-$(VERSION)

CORE_DIR=Core
RESOURCE_DIR=Resx
PARSER_DIR=Parser
TEST_DIR=Test
BIN_DIR=bin
DOC_DIR=doc
EX_DIR=examples
PERF_DIR=perf
ANTLR_DLL=Antlr3.Runtime.dll

PHONIX=$(BIN_DIR)/phonix.exe
PHONIX_TEST=$(BIN_DIR)/phonix.test.dll
ANTLR=$(BIN_DIR)/$(ANTLR_DLL)
NUNIT=$(shell which nunit-console)

DOC_SRC=$(DOC_DIR)/phonix.texinfo
DOC_PDF=$(DOC_DIR)/PhonixManual.pdf
DOC_HTML=$(DOC_DIR)/PhonixManual.html
DOC_INFO=$(DOC_DIR)/phonix.info

RESX_FILES=$(wildcard $(RESOURCE_DIR)/*)
CS_FILES=$(wildcard $(CORE_DIR)/*.cs $(PARSER_DIR)/*.cs)
TEST_FILES=$(CS_FILES) $(wildcard *.cs $(TEST_DIR)/*.cs)
PARSER_FILES=$(PARSER_DIR)/PhonixLexer.cs $(PARSER_DIR)/PhonixParser.cs
DOC_FILES=$(DOC_PDF) $(DOC_HTML) $(DOC_INFO)
EX_FILES=$(wildcard $(EX_DIR)/*)

LINK_RESOURCES=$(foreach resource,$(RESX_FILES),-resource:$(resource))

GMCS=gmcs $(CS_DEBUG) -nowarn:2002 -warn:4 -warnaserror -r:Antlr3.Runtime.dll -lib:./lib -out:$@ $(LINK_RESOURCES)

$(PHONIX): parser $(CS_FILES) $(RESX_FILES) $(ANTLR)
	$(GMCS) -optimize+ $(CS_FILES) $(PARSER_FILES)

parser: $(PARSER_FILES)

test: parser $(PHONIX_TEST) $(ANTLR)
	$(NUNIT) $(PHONIX_TEST) -labels 1>TestLog.txt 2>&1

prof: $(PHONIX)
	if [ -e prof.txt ]; then mv prof.txt base_prof.txt; fi
	time -o prof.txt mono --profile=logging:c,ts,a,o=phonix.mprof $(PHONIX) $(PERF_DIR)/perf.phonix -i $(PERF_DIR)/perf.lex -o $(PERF_DIR)/perf.out
	mprof-decoder phonix.mprof >> prof.txt
	if [ -e prof_parse.txt ]; then mv prof_parse.txt base_prof_parse.txt; fi
	time -o prof_parse.txt mono --profile=logging:c,ts,a,o=parse.mprof $(PHONIX) $(PERF_DIR)/perf.phonix -i /dev/null
	mprof-decoder parse.mprof >> prof_parse.txt

$(PARSER_FILES): $(PARSER_DIR)/Phonix.g
	java -jar lib/antlr-3.1.3.jar -make $(PARSE_DEBUG) -print $< > $(BIN_DIR)/Phonix.parser.g
	mv Phonix.tokens $(BIN_DIR)/Phonix.tokens

$(ANTLR): lib/$(ANTLR_DLL)
	cp $< $@

debug: PARSE_DEBUG=-debug
debug: CS_DEBUG=-debug -define:debug
debug: clean $(PHONIX)

$(PHONIX_TEST): $(TEST_FILES) $(RESX_FILES)
	$(GMCS) -optimize- -debug -d:test -r:nunit.framework.dll -lib:/usr/lib/cli/nunit.framework-2.4/ $(TEST_FILES) $(PARSER_FILES)

doc: $(DOC_FILES)

$(DOC_PDF): $(DOC_SRC)
	texi2pdf -b -c -o $@ $<

$(DOC_HTML): $(DOC_SRC)
	makeinfo --html --no-headers --no-split -o $@ $<

$(DOC_INFO): $(DOC_SRC)
	makeinfo -o $@ $<

all: clean $(PHONIX) $(ANTLR) doc

$(BIN_DIR)/phonix.zip: clean $(PHONIX) $(ANTLR) $(DOC_PDF) $(EX_FILES)
	rm -f $@
	zip $@ $+

zip: $(BIN_DIR)/phonix.zip

install: DESTDIR=/usr/local
install: EXEC_DESTDIR=$(DESTDIR)
install: LIBDIR=$(EXEC_DESTDIR)/lib
install: BINDIR=$(EXEC_DESTDIR)/bin
install: $(PHONIX) $(ANTLR) doc
	install -m 0755 -d $(LIBDIR)/phonix
	install -m 0755 -t $(LIBDIR)/phonix $(PHONIX) $(ANTLER)
	install -m 0755 -D phonix.sh $(BINDIR)/phonix
	sed -i "s!PREFIX!$(LIBDIR)!" $(BINDIR)/phonix
	#cp $(DOC_INFO) /usr/share/info
	#ginstall-info $(DOC_INFO) /usr/share/info/dir

deb:
	rm -rf $(TARFILE) $(DEB_DIR)
	tar -cf $(TARFILE) --exclude-vcs *
	mkdir $(DEB_DIR)
	cd $(DEB_DIR); tar -xf ../$(TARFILE); debuild -uc -us

clean:
	rm -f phonix Test*.* *NUnitPrimaryTrace.txt $(PARSER_FILES) $(DOC_FILES) $(TARFILE) phonix_$(VERSION)*
	rm -rf $(BIN_DIR)/* $(DEB_DIR) 

tags:
	ctags -f .tags -R

