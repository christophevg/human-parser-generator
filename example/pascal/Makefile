TARGET      = main
BUILD       = .build

CC          = mcs
RUN         = mono

ifdef DEBUG
RUN        += --debug
CFLAGS     += -define:DEBUG -debug
endif

INPUT      ?= example.pas

all: run

run: $(BUILD)/$(TARGET).exe
	@$(RUN) $< $(INPUT) 

$(BUILD)/%.exe: *.cs
	@mkdir -p $(BUILD)
	@$(CC) $(CFLAGS) -out:$@ $^

clean:
	@rm -rf $(BUILD)
