# Look at how awesome and short my makefile is

CORE_DIR=Core
RESOURCE_DIR=Resx
PARSER_DIR=Parser
TEST_DIR=UnitTest
BIN_DIR=bin
DOC_DIR=doc
EX_DIR=examples
ANTLR_DLL=Antlr3.Runtime.dll

PHONIX=$(BIN_DIR)/phonix.exe
PHONIX_TEST=$(BIN_DIR)/phonix.test.dll
ANTLR=$(BIN_DIR)/$(ANTLR_DLL)

DOC_SRC=$(DOC_DIR)/phonix.texinfo
DOC_PDF=$(DOC_DIR)/PhonixManual.pdf
DOC_HTML=$(DOC_DIR)/phonix_manual.html
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
	rm -f *NUnitPrimaryTrace.txt
	mono --debug /usr/lib/nunit/nunit-console.exe $(PHONIX_TEST) -labels

prof: $(PHONIX)
	mono --profile $(PHONIX) $(EX_DIR)/romanian.phonix -i $(EX_DIR)/romanian.input -o $(EX_DIR)/romanian.output > prof.txt

$(PARSER_FILES): $(PARSER_DIR)/Phonix.g
	java -jar lib/antlr-3.1.3.jar -make $(PARSE_DEBUG) -print $< > $(BIN_DIR)/Phonix.parser.g
	mv Phonix.tokens $(BIN_DIR)/Phonix.tokens

$(ANTLR): lib/$(ANTLR_DLL)
	cp $< $@

debug: PARSE_DEBUG=-debug
debug: CS_DEBUG=-debug -define:debug
debug: clean $(PHONIX)

$(PHONIX_TEST): $(TEST_FILES) $(RESX_FILES)
	$(GMCS) -optimize- -debug -r:nunit.framework.dll -lib:/usr/lib/cli/nunit-2.4/ $(TEST_FILES) $(PARSER_FILES)

doc: $(DOC_FILES)

$(DOC_PDF): $(DOC_SRC)
	texi2pdf -b -c -o $@ $<

$(DOC_HTML): $(DOC_SRC)
	makeinfo --html --no-headers --no-split -o $@ $<

$(DOC_INFO): $(DOC_SRC)
	makeinfo -o $@ $<

$(BIN_DIR)/phonix.zip: $(PHONIX) $(ANTLR) $(DOC_PDF) $(EX_FILES)
	rm -f $@
	zip $@ $+

zip: $(BIN_DIR)/phonix.zip

install: prefix?=/usr/local
install: exec_prefix?=$(prefix)
install: libdir?=$(exec_prefix)/lib
install: bindir?=$(exec_prefix)/bin
install: $(PHONIX) $(ANTLR) $(DOC_INFO)
	mkdir -p $(libdir)/phonix
	cp $(PHONIX) $(libdir)/phonix
	cp $(ANTLR) $(libdir)/phonix
	cp phonix.sh phonix
	sed -i "s!PREFIX!$(libdir)!" phonix
	chmod 755 phonix
	cp phonix $(bindir)
	cp $(DOC_INFO) /usr/share/info
	install-info $(DOC_INFO)

uninstall: prefix?=/usr/local
uninstall: exec_prefix?=$(prefix)
uninstall: libdir?=$(exec_prefix)/lib
uninstall: bindir?=$(exec_prefix)/bin
uninstall:
	rm -rf $(libdir)/phonix
	rm -f $(prefix)/bin/phonix
	install-info --remove phonix

clean:
	rm -f TestResult.xml
	rm -f *NUnitPrimaryTrace.txt
	rm -f $(PARSER_FILES)
	rm -f $(BIN_DIR)/*
	rm -f $(DOC_FILES)
	rm -f phonix

tags:
	ctags -f .tags -R
