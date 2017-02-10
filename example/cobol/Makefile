PARSER     ?= cobol.cs
GRAMMAR    ?= cobol-record-definition.bnf
GENERATOR  ?= ../../generator
TARGET     ?= parse-cobol.exe
INPUT      ?= simple.cobol

-include ../../generator/Makefile.support

SRCS        = $(GENERATOR)/parsable.cs $(GENERATOR)/dump-ast.cs $(PARSER)
TEST_SRCS   = $(SRCS) test_*.cs

all: clean test

test: test.dll
	@echo "*** executing unit tests"
	@$(NUNIT) $<

test.dll: $(TEST_SRCS)
	@echo "*** building unit tests"
	@$(CC) $(CFLAGS) -t:library -r:nunit.framework.dll -out:$@ $^

parse: $(TARGET)
	@echo "*** running $<"
	@$(RUN) $< $(INPUT) | $(FORMATTER)

$(TARGET): $(GRAMMAR) $(SRCS)
	@echo "*** building $@ from $(filter-out $<,$^)"
	@$(CC) $(CFLAGS) -out:$@ $(filter-out $<,$^)

$(PARSER): $(GRAMMAR) $(HPG)
	@echo "*** generating a Cobol parser from $<"
	@$(RUN) $(HPG) $< > $@

$(HPG):
	@(cd $(GENERATOR); make)

clean:
	@rm -rf $(BUILD) TestResult.xml $(PARSER) test.dll $(TARGET)

-include Makefile.local
