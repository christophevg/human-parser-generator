TARGET      = main
BUILD       = .build

ifdef DEBUG
CFLAGS     += -define:DEBUG
endif

ifdef CSHARP_VERSION
CFLAGS     += -langversion:$(CSHARP_VERSION)
endif

CC          = mcs
RUN         = mono
ARGUMENTS  ?=
NUNIT       = nunit-console -nologo

SRCS        = generator/{grammar,parser,emitter}.cs

PARSER      = parser.cs

all: clean test

$(PARSER):
	@echo "*** generating HPG BNF-like parser..."
	@(cd ../generator; make parser LANG=bnf > ../test/$(PARSER))

$(BUILD)/test.dll: *.cs $(PARSER)
	@echo "*** building unit tests"
	@mkdir -p $(BUILD)
	@$(CC) $(CFLAGS) -t:library -r:nunit.framework.dll -out:$@ $^

test: $(BUILD)/test.dll
	@echo "*** executing unit tests"
	@$(NUNIT) $<

clean:
	@rm -rf $(BUILD) TestResult.xml $(PARSER)

-include Makefile.local
