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
	@cp $(SRC)/$(PASCAL)   $(PACKAGE)
	@cp $(SRC)/main.cs     $(PACKAGE)
	@echo "*** running example with generated code"
	@(cd $(PACKAGE); make ARGUMENTS=$(PASCAL))

$(BUILD)/%.exe: *.cs
	@mkdir -p $(BUILD)
	@$(CC) $(CFLAGS) -out:$@ $^

clean:
	@rm -rf $(BUILD) $(PACKAGE)
