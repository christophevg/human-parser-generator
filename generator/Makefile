# folder for generation demo
FOLDER     ?= demo
# example folder (for pascal example)
EXAMPLE     = ../example/pascal
# input file for pascal demo
PASCAL     ?= example.pas
# original pascal parser
PASPARSER  ?= pascal.cs

# folder to generate a generator in
GENERATOR  ?= generation

# filename of the generated parser
PARSER     ?= parser.cs
# language definition (aka grammar) to generate a parser for
LANG       ?= $(GRAMMARS)/hpg.bnf

# internal folders
RUNTIME     = runtime
GRAMMARS    = grammars
BUILD       = .build

CC          = mcs
RUN         = mono

ifdef DEBUG
RUN        += --debug
CFLAGS     += -define:DEBUG -debug
endif

SRCS        = generator.cs emitter.cs

ifneq ("$(wildcard bootstrap.cs)","")
SRCS       += grammar.cs bootstrap.cs
TARGET      = bootstrap
LANG        = bootstrap
else
SRCS       += $(RUNTIME)/parsable.cs $(PARSER) hpg.cs 
TARGET      = hpg
endif

ifdef OUT
OUTPUT      = > $(OUT)
OUTPUT_LBL  = " to $(OUT)"
endif

ASTYLE      = | LC_ALL="C" astyle -s2

all: clean pascal-demo
	
generation2:
	@echo "*** building second generation generator/parser"
	@(make generator && cd $(GENERATOR) && make generator)

parser: $(BUILD)/$(TARGET).exe
	@echo "*** generating parser from $(LANG)$(OUTPUT_LBL)"
	@$(RUN) $< $(LANG) $(ASTYLE) $(OUTPUT)

generator:
	@echo "*** generating parser"
	@mkdir -p $(GENERATOR)
	@(make parser OUT=$(GENERATOR)/$(PARSER))
	@(make generator-env FOLDER=$(GENERATOR))

generator-env:
	@echo "*** setting up generator environment"
	@cp -r Makefile $(RUNTIME) $(GRAMMARS) generator.cs emitter.cs hpg.cs $(FOLDER)

# Pascal demo

pascal-demo: diff execute

$(FOLDER)/$(PARSER): $(FOLDER) generation2
	@(cd $(GENERATOR)/$(GENERATOR) && make parser LANG=grammars/pascal-assignments.bnf OUT=../../$@)

execute: runtime
	@echo "*** running example with generated parser"
	@(cd $(FOLDER); make ARGUMENTS=$(PASCAL))

runtime: $(FOLDER)
	@echo "*** setting up runtime environment for parser"
	@cp $(RUNTIME)/Makefile    $<
	@cp $(RUNTIME)/parsable.cs $<
	@cp $(RUNTIME)/dump-ast.cs $<
	@cp $(EXAMPLE)/$(PASCAL)   $<

diff: $(FOLDER)/$(PARSER)
	@echo "*** comparing to manual version"
	@diff -u -w $(EXAMPLE)/$(PASPARSER) $(FOLDER)/$(PARSER)

$(BUILD)/%.exe: $(SRCS)
	@mkdir -p $(BUILD)
	@$(CC) $(CFLAGS) -out:$@ $^

$(FOLDER):
	@mkdir -p $@

clean:
	@rm -rf $(BUILD) $(FOLDER) $(OUT) $(GENERATOR) pascal
