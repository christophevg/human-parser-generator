GRAMMAR    ?= grammars/cobol-record-definition.bnf

TARGET      = main.exe
BUILD       = .build

CC          = mcs
RUN         = mono

ifdef DEBUG
RUN        += --debug
CFLAGS     += -define:DEBUG -debug
endif

ifdef CSHARP_VERSION
CFLAGS     += -langversion:$(CSHARP_VERSION)
endif

ARGUMENTS  ?=
NUNIT       = nunit-console -nologo

SRCS        = generator/{grammar,parser,emitter}.cs

PARSER      = parser.cs

TEST_SRCS   = *.cs $(PARSER)
PARSE_SRCS  = dump-ast.cs parsable.cs $(PARSER)

INPUT      ?= copybook.cobol

all: clean test

parse: $(BUILD)/$(TARGET)
	@echo "*** running $<"
	@$(RUN) $< $(INPUT)

$(PARSER): ../../generator/$(GRAMMAR)
	@echo "*** generating parser generator, including EBNF-like parser"
	@(cd ../../generator; make clean generation2)
	@(cd ../../generator/generation/generation && make parser LANG=$(GRAMMAR) OUT=../../../example/cobol/$(PARSER))

$(BUILD)/%.exe: $(PARSE_SRCS)
	@echo "*** building $@"
	@mkdir -p $(BUILD)
	@$(CC) $(CFLAGS) -out:$@ $^

$(BUILD)/test.dll: $(TEST_SRCS)
	@echo "*** building unit tests"
	@mkdir -p $(BUILD)
	@$(CC) $(CFLAGS) -t:library -r:nunit.framework.dll -out:$@ $^

test: $(BUILD)/test.dll
	@echo "*** executing unit tests"
	@$(NUNIT) $<

clean:
	@rm -rf $(BUILD) TestResult.xml $(PARSER)

-include Makefile.local
