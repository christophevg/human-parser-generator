TARGET      = main
BUILD       = .build

CC          = mcs
RUN         = mono --debug
ARGUMENTS  ?=

CFLAGS     += -define:DEBUG -debug

PACKAGE     = demo
SRC         = ../example
PARSER      = parser.cs
PASCAL      = example.pas


all: run
	
run: $(BUILD)/$(TARGET).exe
	@$(RUN) $< $(ARGUMENTS)
	
demo: clean package generate diff execute

package:
	@mkdir -p $(PACKAGE)

generate: $(BUILD)/$(TARGET).exe
	@echo "*** generating parser"
	@(make run) > $(PACKAGE)/$(PARSER)

diff:
	@echo "*** comparing to manual version"
	@diff -u -w $(SRC)/pascal.cs $(PACKAGE)/$(PARSER)

execute:
	@echo "*** adding parsable and supporting files"
	@cp $(SRC)/Makefile    $(PACKAGE)
	@cp $(SRC)/parsable.cs $(PACKAGE)
	@cp $(SRC)/main.cs     $(PACKAGE)
	@cp $(SRC)/$(PASCAL)   $(PACKAGE)
	@echo "*** running example with generated code"
	@(cd $(PACKAGE); make ARGUMENTS=$(PASCAL))

hosting: clean
	@echo "*** generating BNF parser from C# Model"
	@mkdir -p hosting
	@(make run ARGUMENTS=bnf) > hosting/parser.cs
	@echo "*** adding parsable and supporting files"
	@cp $(SRC)/parsable.cs  hosting
	@cp $(SRC)/Makefile     hosting
	@cp $(SRC)/main.cs      hosting
	@cp ../grammars/hpg.bnf hosting
	@(cd hosting; make ARGUMENTS=hpg.bnf DEBUG=1)

cobol: clean
	@echo "*** generating BNF parser from C# Model"
	@mkdir -p cobol
	@(make run ARGUMENTS=bnf) > cobol/parser.cs
	@echo "*** adding parsable and supporting files"
	@cp $(SRC)/parsable.cs  cobol
	@cp $(SRC)/Makefile     cobol
	@cp $(SRC)/main.cs      cobol
	@cp ../grammars/cobol-record-definition.bnf cobol
	@(cd cobol; make ARGUMENTS=cobol-record-definition.bnf DEBUG=1)

$(BUILD)/%.exe: *.cs
	@mkdir -p $(BUILD)
	@$(CC) $(CFLAGS) -out:$@ $^

clean:
	@rm -rf $(BUILD) $(PACKAGE) hosting cobol
