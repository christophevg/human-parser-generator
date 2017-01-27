TARGET      = main
BUILD       = .build

ifdef DEBUG
CFLAGS     += -define:DEBUG
endif

CC          = mcs
RUN         = mono
ARGUMENTS  ?= example.pas

all: run

$(BUILD)/%.exe: *.cs
	@mkdir -p $(BUILD)
	@$(CC) $(CFLAGS) -out:$@ $^

run: $(BUILD)/$(TARGET).exe
	@$(RUN) $< $(ARGUMENTS)

clean:
	@rm -rf $(BUILD)
