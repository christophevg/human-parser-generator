PARSER     ?= cobol.cs
GRAMMAR    ?= cobol-record-definition.bnf
GENERATOR  ?= ../../generator
TARGET     ?= parse-cobol.exe

CC          = mcs
RUN         = mono
NUNIT       = nunit-console -nologo
HPG         = $(GENERATOR)/hpg.exe

ifdef DEBUG
RUN        += --debug
CFLAGS     += -define:DEBUG -debug
endif

SRCS        = $(GENERATOR)/parsable.cs $(GENERATOR)/dump-ast.cs $(PARSER)
TEST_SRCS   = $(SRCS) test_*.cs

INPUT      ?= simple.cobol

all: clean test

test: test.dll
	@echo "*** executing unit tests"
	@$(NUNIT) $<

test.dll: $(TEST_SRCS)
	@echo "*** building unit tests"
	@$(CC) $(CFLAGS) -t:library -r:nunit.framework.dll -out:$@ $^

parse: $(TARGET)
	@echo "*** running $<"
	@$(RUN) $< $(INPUT)

$(TARGET): $(SRCS)
	@echo "*** building $@ from $^"
	@$(CC) $(CFLAGS) -out:$@ $^

$(PARSER): $(GRAMMAR) $(HPG)
	@echo "*** generating a Cobol parser from $<"
	@$(RUN) $(HPG) $< > $@

$(HPG):
	@(cd ../../generator; make)

clean:
	@rm -rf $(BUILD) TestResult.xml $(PARSER) test.dll $(TARGET)

-include Makefile.local
