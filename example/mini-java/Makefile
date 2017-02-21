TARGET     ?= mini-java
PARSER     ?= $(TARGET).cs
GRAMMAR    ?= $(TARGET).bnf
GENERATOR  ?= ../../generator
EXE        ?= parse-$(TARGET).exe
INPUT      ?= example.$(TARGET)

-include ../../generator/Makefile.support

SRCS        = $(GENERATOR)/parsable.cs $(GENERATOR)/dump-ast.cs $(PARSER)
TEST_SRCS   = $(SRCS) test_*.cs

all: parse

test: test.dll
	@echo "*** executing unit tests"
	@$(NUNIT) $<

test.dll: $(TEST_SRCS)
	@echo "*** building unit tests"
	@$(CC) $(CFLAGS) -t:library -r:nunit.framework.dll -out:$@ $^

parse: $(EXE)
	@echo "*** running $<"
	@$(RUN) $< $(INPUT) | $(FORMATTER)

$(EXE): $(GRAMMAR) $(SRCS)
	@echo "*** building $@ from $(filter-out $<,$^)"
	@$(CC) $(CFLAGS) -out:$@ $(filter-out $<,$^)

$(PARSER): $(GRAMMAR) $(HPG)
	@echo "*** generating a $(TARGET) parser from $<"
	@$(RUN) $(HPG) $< | $(ASTYLE) > $@

clean:
	@rm -rf $(BUILD) TestResult.xml $(PARSER) test.dll $(EXE) *.dot *.mdb

-include Makefile.local