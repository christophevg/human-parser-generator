TARGET      = dump-ast
BUILD       = .build

ifdef DEBUG
CFLAGS     += -define:DEBUG
endif

ifdef CSHARP_VERSION
CFLAGS     += -langversion:$(CSHARP_VERSION)
endif

CC          = mcs
RUN         = mono
ARGUMENTS  ?= ../grammars/cobol-record-definition.bnf

SRCS        = ../generator/{grammar,parser,emitter}.cs

PARSER      = bnf-parser.cs

all: clean parser run

parser: $(PARSER)

$(PARSER):
	@(cd ../generator; make parser LANG=bnf OUT=../cobol/$(PARSER))

$(BUILD)/%.exe: *.cs
	@mkdir -p $(BUILD)
	@$(CC) $(CFLAGS) -out:$@ $^

run: $(BUILD)/$(TARGET).exe
	@$(RUN) $< $(ARGUMENTS)

clean:
	@rm -rf $(BUILD) TestResult.xml $(PARSER)

-include Makefile.local
