GENERATED  ?= generated
PARSER     ?= pascal.cs
GENERATOR  ?= ../../generator
GRAMMAR    ?= pascal-assignments.bnf
INPUT      ?= example.pas

-include $(GENERATOR)/Makefile.support

SRCS        = $(GENERATOR)/dump-ast.cs $(GENERATOR)/parsable.cs $(PARSER)

all: clean run-manual $(GENERATED)/$(PARSER) diff run-generated

run-manual: manual.exe
	@echo "*** running manual Pascal parser"
	@$(RUN) $< $(INPUT)

$(GENERATED)/$(PARSER): $(GRAMMAR) $(HPG)
	@echo "*** generating a Pascal parser from $<"
	@mkdir -p $(GENERATED)
	@$(RUN) $(HPG) $< | $(ASTYLE) > $@

diff: $(GENERATED)/$(PARSER)
	@echo "*** comparing to manual version"
	@diff -u -w $(PARSER) $(GENERATED)/$(PARSER) && \
		echo "    manual and generated parsers are identical"

run-generated: $(GENERATED)/$(PARSER)
	@echo "*** setting up generated environment for $<"
	@cp $(GENERATOR)/Makefile.runtime $(GENERATED)/Makefile
	@ls -l $(GENERATED)
	@echo "*** compiling and running $<"
	@(cd $(GENERATED); make INPUT=../$(INPUT) GENERATOR=../$(GENERATOR))

%.exe: $(SRCS)
	@echo "*** compiling $@ from $^"
	@$(CC) $(CFLAGS) -out:$@ $^

clean:
	@rm -rf $(GENERATED) manual.exe
