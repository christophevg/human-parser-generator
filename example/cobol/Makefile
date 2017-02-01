TARGET      = main
BUILD       = .build

CC          = mcs
RUN         = mono

ifdef DEBUG
RUN        += --debug
CFLAGS     += -define:DEBUG -debug
endif

INPUT      ?= ../../generator/grammars/cobol-record-definition.bnf

PARSER     ?= parser.cs

all: run

run: $(BUILD)/$(TARGET).exe
	@$(RUN) $< $(INPUT) 

$(BUILD)/%.exe: $(PARSER) *.cs
	@echo "*** building $@ from $^"
	@mkdir -p $(BUILD)
	@$(CC) $(CFLAGS) -out:$@ $^

$(PARSER):
	@echo "*** generating HPG BNF-like parser..."
	@(cd ../../generator; make generation2)
	@cp ../../generator/generation/generation/parser.cs .

clean:
	@rm -rf $(BUILD) $(PARSER)
