GENERATOR  ?= ../generator
TEST_SRCS   = $(GENERATOR)/generator.cs \
              $(GENERATOR)/parsable.cs \
              $(GENERATOR)/parser.cs \
							test_*.cs

CC          = mcs
RUN         = mono
NUNIT       = nunit-console -nologo

ifdef DEBUG
RUN        += --debug
CFLAGS     += -define:DEBUG -debug
endif

ifdef CSHARP_VERSION
CFLAGS     += -langversion:$(CSHARP_VERSION)
endif


all: clean test

test: test.dll
	@echo "*** executing unit tests"
	@$(NUNIT) $<

test.dll: $(TEST_SRCS)
	@echo "*** building unit tests"
	@$(CC) $(CFLAGS) -t:library -r:nunit.framework.dll -out:$@ $^

$(GENERATOR)/parser.cs:
	@(cd $(GENERATOR); make)

clean:
	@rm -rf $(BUILD) TestResult.xml test.dll
	@(cd $(GENERATOR); make clean)

-include Makefile.local
