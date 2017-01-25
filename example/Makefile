TARGET      = main
BUILD       = .build

CC          = mcs
RUN         = mono
ARGUMENTS  ?=

all: run

$(BUILD)/%.exe: *.cs
	@mkdir -p $(BUILD)
	@$(CC) $(CFLAGS) -out:$@ $^

run: $(BUILD)/$(TARGET).exe
	@$(RUN) $< $(ARGUMENTS)

clean:
	@rm -rf $(BUILD)
