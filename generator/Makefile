ARGUMENTS  ?=
FOLDER     ?= generated

TARGET      = generate-parser
BUILD       = .build

CC          = mcs
RUN         = mono --debug

CFLAGS     += -define:DEBUG -debug

PARSER      = parser.cs
PASCAL      = example.pas

LANG        = pascal

EXAMPLE     = ../example
RUNTIME     = ../runtime

ifdef OUT
OUTPUT      = > $(OUT)
OUTPUT_LBL  = " to $(OUT)"
endif

all: diff execute
	
parser: $(BUILD)/$(TARGET).exe
	@echo "*** generating parser for $(LANG)$(OUTPUT_LBL)"
	@$(RUN) $< $(LANG) $(OUTPUT)
	
$(FOLDER): clean
	@mkdir -p $(FOLDER)

parser-file: $(BUILD)/$(TARGET).exe $(FOLDER)
	@(make parser LANG=$(LANG) OUT=$(FOLDER)/$(PARSER))

diff: parser-file
	@echo "*** comparing to manual version"
	@diff -u -w $(EXAMPLE)/pascal.cs $(FOLDER)/$(PARSER)

execute: parser-file runtime
	@echo "*** running example with generated code"
	@(cd $(FOLDER); make ARGUMENTS=$(PASCAL))

runtime: $(FOLDER)
	@echo "*** setting up runtime environment for parser"
	@cp $(RUNTIME)/Makefile    $<
	@cp $(RUNTIME)/parsable.cs $<
	@cp $(RUNTIME)/dump-ast.cs $<
	@cp $(EXAMPLE)/$(PASCAL)   $<

$(BUILD)/%.exe: *.cs
	@mkdir -p $(BUILD)
	@$(CC) $(CFLAGS) -out:$@ $^

clean:
	@rm -rf $(BUILD) $(FOLDER) $(OUT)
