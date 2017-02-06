TARGET      = main
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

all: clean test

$(PARSER):
	@echo "*** generating parser generator, including EBNF-like parser"
	@(cd ../generator; make clean generation2)
	@cp ../generator/generation/generation/parser.cs .

$(BUILD)/test.dll: *.cs # $(PARSER)
	@echo "*** building unit tests"
	@mkdir -p $(BUILD)
	@$(CC) $(CFLAGS) -t:library -r:nunit.framework.dll -out:$@ $^

test: $(BUILD)/test.dll
	@echo "*** executing unit tests"
	@$(NUNIT) $<

clean:
	@rm -rf $(BUILD) TestResult.xml $(PARSER)

-include Makefile.local
