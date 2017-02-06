TARGET      = hpg
BUILD       = .build

CC          = mcs
RUN         = mono

ifdef DEBUG
RUN        += --debug
CFLAGS     += -define:DEBUG -debug
endif

GRAMMAR    ?= grammars/cobol-record-definition.bnf

PARSER     ?= cobol-parser.cs

INPUT      ?= simple.cobol

all: clean run

run: $(BUILD)/$(TARGET).exe
	@$(RUN) $< $(INPUT)

$(BUILD)/%.exe: $(PARSER) *.cs
	@echo "*** building $@ from $^"
	@mkdir -p $(BUILD)
	@$(CC) $(CFLAGS) -out:$@ $^

$(PARSER):
	@echo "*** generating parser generator, including EBNF-like parser"
	@(cd ../../generator; make clean generation2)
	@(cd ../../generator/generation/generation && make parser LANG=$(GRAMMAR) OUT=../../../example/cobol/$(PARSER))

clean:
	@rm -rf $(BUILD) $(PARSER)
