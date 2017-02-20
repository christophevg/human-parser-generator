# filename of the bootstrap/generated parser
GEN1PARSER ?= parser.gen1.cs

# name of the (self-hosted) parser to generate
PARSER     ?= parser.cs

# by default our own HPG EBNF-like grammar
EBNF       ?= hpg.bnf
LANG       ?= $(EBNF)

# name of the parser generator executable
HPG        ?= hpg.exe

# configuration of generations of sources
COMMONSRCS  = generator.cs emitter.cs
GEN1SRCS    = $(COMMONSRCS) grammar.cs bootstrap.cs
GEN2SRCS    = $(COMMONSRCS) parsable.cs $(GEN1PARSER) hpg.cs 
SRCS       ?= $(COMMONSRCS) parsable.cs $(PARSER) hpg.cs 

ifdef OUT
OUTPUT      = > $(OUT)
OUTPUT_LBL  = " to $(OUT)"
endif

GRAMMAR     = hpg.bnf

-include Makefile.support

all: $(HPG)

$(GEN1PARSER): $(GEN1SRCS)
	@echo "*** building $@"
	@(make parser SRCS="$^" OUT=$@ LANG=$(EBNF))
	@rm $(HPG)

$(PARSER): $(GEN2SRCS)
	@echo "*** building $@"
	@(make parser SRCS="$^" OUT=$@ LANG=$(EBNF))
	@rm $(HPG)

parser: $(HPG)
	@echo "*** generating parser from $(LANG)$(OUTPUT_LBL)"
	@$(RUN) $< $(LANG) | $(ASTYLE) $(OUTPUT)

%.exe: $(SRCS)
	@echo "*** compiling $@ from $^"
	@$(CC) $(CFLAGS) -out:$@ $^

parse: $(HPG)
	@echo "*** parsing grammar from stdin"
	@$(RUN) $< -m | $(FORMATTER)

clean:
	@rm -rf $(HPG) $(GEN1PARSER) $(PARSER) *.mdb
