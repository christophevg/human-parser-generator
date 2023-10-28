# Human Parser Generator [![Build Status](https://circleci.com/gh/christophevg/human-parser-generator.png)](https://circleci.com/gh/christophevg/human-parser-generator)

A straightforward recursive descent Parser Generator with a focus on "human" code generation and ease of use.  
Christophe VG (<contact@christophe.vg>)  
[https://github.com/christophevg/human-parser-generator](https://github.com/christophevg/human-parser-generator)

> [!IMPORTANT]  
> This repo has been [archived](https://docs.github.com/en/repositories/archiving-a-github-repository/archiving-repositories#)

# Rationale

Although many parser generators exist, I feel like there is room for one more, which generates a parser in a more "human" way.

The objectives are:

* start from a **standard EBNF grammar**, e.g. allow copy pasting existing grammars and (maybe almost) be done with it.
* generate code, as if it were **written by a human developer**:
	* generate functional classes to construct the AST
	* generate parser logic that is readable and understandable
* be **self hosting**: the generator should be able to generate a parser for itself.

> [EBNF](https://en.wikipedia.org/wiki/Extended_Backus–Naur_form) is a (meta-)syntax that can be used to express (context-free) grammars. EBNF is an "extension" to [BNF](https://en.wikipedia.org/wiki/Backus–Naur_form).
> The Human Parser Generator takes EBNF grammars as input to generate parsers for the language expressed by the grammar.

> The project initially targets C#, which is the language of the generator itself. Once the generator is stable, support for generating other languages can be added.

# Current Status - Version 1.1

* The generator is capable of generating a parser for its own EBNF-like definition language, which means it's [self-hosting](https://github.com/christophevg/human-parser-generator/wiki/Bootstrapping). 
* Parsers for a more complex grammars, e.g. [Cobol record definitions (aka Copybooks)](https://github.com/christophevg/human-parser-generator/wiki/Example Cobol) and (a subset of) DB2 DDL, live up to the expectations.
* Generated parsers are very readable and apply a [fluid parsing API/DSL](https://github.com/christophevg/human-parser-generator/wiki/Parsing DSL).

# Get the Human Parser Generator

> We provide downloads for the repository and a binary build of `hpg.exe` from our [releases GitHub page](https://github.com/christophevg/human-parser-generator/releases).

**Minimal Survival Commands:**

```bash
$ git clone https://github.com/christophevg/human-parser-generator
$ cd human-parser-generator
$ msbuild
Microsoft (R) Build Engine version 14.1.0.0
Copyright (C) Microsoft Corporation. All rights reserved.

Build started 3/6/2017 1:46:48 PM.
Project "/Users/xtof/Workspace/human-parser-generator/hpg.csproj" on node 1 (default targets).
MakeBuildDirectory:
  Creating directory "bin/Debug/".
Gen0Parser:
  /Library/Frameworks/Mono.framework/Versions/4.6.2/lib/mono/4.5/csc.exe /debug+ /out:bin/Debug/hpg.gen0.exe /target:exe generator/parsable.cs generator/generator.cs generator/factory.cs generator/emitter.csharp.cs generator/emitter.bnf.cs generator/format.csharp.cs generator/AssemblyInfo.cs generator/grammar.cs generator/bootstrap.cs
Gen1Source:
  mono bin/Debug/hpg.gen0.exe generator/hpg.bnf | LC_ALL="C" astyle -s2 -xt0 -xe -Y -xC80 > generator/parser.gen1.cs
Gen1Parser:
  /Library/Frameworks/Mono.framework/Versions/4.6.2/lib/mono/4.5/csc.exe /debug+ /out:bin/Debug/hpg.gen1.exe /target:exe generator/parsable.cs generator/generator.cs generator/factory.cs generator/emitter.csharp.cs generator/emitter.bnf.cs generator/format.csharp.cs generator/AssemblyInfo.cs generator/parser.gen1.cs generator/hpg.cs
HPGSource:
  mono bin/Debug/hpg.gen1.exe generator/hpg.bnf | LC_ALL="C" astyle -s2 -xt0 -xe -Y -xC80 > generator/parser.cs
Build:
  /Library/Frameworks/Mono.framework/Versions/4.6.2/lib/mono/4.5/csc.exe /debug+ /out:bin/Debug/hpg.exe /target:exe generator/parsable.cs generator/generator.cs generator/factory.cs generator/emitter.csharp.cs generator/emitter.bnf.cs generator/format.csharp.cs generator/AssemblyInfo.cs generator/parser.cs generator/hpg.cs
Done Building Project "/Users/xtof/Workspace/human-parser-generator/hpg.csproj" (default targets).

Build succeeded.
    0 Warning(s)
    0 Error(s)

Time Elapsed 00:00:02.38
```
```bash
$ mono bin/Debug/hpg.exe --help
Human Parser Generator version 1.1.6274.24805
Usage: hpg.exe [options] [file ...]

    --help, -h              Show usage information
    --version, -v           Show version information

    --output, -o FILENAME   Output to file, not stdout

Output options.
Select one of the following:
    --parser, -p            Generate parser (DEFAULT)
    --ast, -a               Show AST
    --model, -m             Show parser model
    --grammar, -g           Show grammar
Formatting options.
    --text, -t              Generate textual output (DEFAULT).
    --dot, -d               Generate Graphviz/Dot format output. (model)
Emission options.
    --info, -i              Suppress generation of info header
    --rule, -r              Suppress generation of rule comment
    --namespace, -n NAME    Embed parser in namespace
```

> When running on a unix-like environment (e.g. macOS, Linux, ...) the generated parsers are styled using [AStyle](http://astyle.sourceforge.net). On Windows this dependency is suppressed by default. To avoid using AStyle, set the `AStyle` build property to an empty string: ` msbuild /Property:AStyle=`.

# A Complete Example

The following example is taken from [the Wikipedia page on EBNF](https://en.wikipedia.org/wiki/Extended_Backus–Naur_form):

```ebnf
(* a simple program syntax in EBNF − Wikipedia *)
 program = 'PROGRAM', white space, identifier, white space, 
            'BEGIN', white space, 
            { assignment, ";", white space }, 
            'END.' ;
 identifier = alphabetic character, { alphabetic character | digit } ;
 number = [ "-" ], digit, { digit } ;
 string = '"' , { all characters - '"' }, '"' ;
 assignment = identifier , ":=" , ( number | identifier | string ) ;
 alphabetic character = "A" | "B" | "C" | "D" | "E" | "F" | "G"
                      | "H" | "I" | "J" | "K" | "L" | "M" | "N"
                      | "O" | "P" | "Q" | "R" | "S" | "T" | "U"
                      | "V" | "W" | "X" | "Y" | "Z" ;
 digit = "0" | "1" | "2" | "3" | "4" | "5" | "6" | "7" | "8" | "9" ;
 white space = ? white space characters ? ;
 all characters = ? all visible characters ? ;
```

This grammar allows to parse a Pascal program with assignments:

```pascal
 PROGRAM DEMO1
 BEGIN
   A:=3;
   B:=45;
   H:=-100023;
   C:=A;
   D123:=B34A;
   BABOON:=GIRAFFE;
   TEXT:="Hello world!";
 END.
```

To take advantage of the [extended grammar features of the Human Parser Generator](https://github.com/christophevg/human-parser-generator/wiki/HPG Grammar), the grammar above can be rewritten to:

```ebnf
(* a simple program syntax in HPG-flavoured EBNF - based on example from Wikipedia *)

program      = "PROGRAM" identifier
               "BEGIN"
               { assignment ";" }
               "END."
             ;

assignment   = identifier ":=" expression ;

expression   = identifier
             | string
             | number
             ;

identifier   = name  @ ? /([A-Z][A-Z0-9]*)/ ? ;
string       = text  @ ? /"([^"]*)"|'([^']*)'/ ? ;
number       = value @ ? /(-?[1-9][0-9]*)/ ? ;
```

We can now feed this grammar to the Human Parser Generator

```bash
$ mono hpg.exe example/pascal/pascal.bnf
```

The generated parser is returned on standard output:

```csharp
// DO NOT EDIT THIS FILE
// This file was generated using the Human Parser Generator
// (https://github.com/christophevg/human-parser-generator)
// on Monday, March 6, 2017 at 1:10:56 PM
// Source : example/pascal/pascal.bnf

using System;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;

// program ::= "PROGRAM" identifier "BEGIN" { assignment ";" } "END." ;
public class Program {
  public Identifier Identifier { get; set; }
  public List<Assignment> Assignments { get; set; }
  public Program() {
    this.Assignments = new List<Assignment>();
  }
  // ...
}
// ...
public class Parser : ParserBase<Program> {

  // program ::= "PROGRAM" identifier "BEGIN" { assignment ";" } "END." ;
  public override Program Parse() {
    Program program = new Program();
    Log( "ParseProgram" );
    Parse( () => {
      Consume("PROGRAM");
      program.Identifier = ParseIdentifier();
      Consume("BEGIN");
      Repeat( () => {
        program.Assignments.Add(ParseAssignment());
        Consume(";");
      });
      Consume("END.");
    }).OrThrow("Failed to parse Program");
    return program;
  }
// ...
}
```

> If no `file` is provided, input is read from standard input.

Combine this generated parser with `parsable.cs` and add a minimal driver application:

```csharp
// run.cs - a minimal driver application of HPG generated parsers
using System;
using System.IO;

public class Runner {
  public static void Main(string[] args) {
    string source = File.ReadAllText(args[0]);

    Parser parser = new Parser();
    parser.Parse(source);

    Console.WriteLine(parser.AST);
  }
}
```

Compile and run ...

```bash
$ mcs run.cs pascal.cs generator/parsable.cs 
$ mono run.exe example/pascal/example.pascal
```

The output is a string representation of the resulting AST:

```csharp
new Program() {
  Identifier = new Identifier() { Name = "DEMO1"},
  Assignments = new List<Assignment>() {
    new Assignment() {
      Identifier = new Identifier() { Name = "A"},
      Expression = new Number() { Value = "3" }
    },
    new Assignment() {
      Identifier = new Identifier() { Name = "B"},
      Expression = new Number() { Value = "45" }
    },
    new Assignment() {
      Identifier = new Identifier() { Name = "H"},
      Expression = new Number() { Value = "-100023" }
    },
    new Assignment() {
      Identifier = new Identifier() { Name = "C"},
      Expression = new Identifier() { Name = "A" }
    },
    new Assignment() {
      Identifier = new Identifier() { Name = "D123"},
      Expression = new Identifier() { Name = "B34A" }
    },
    new Assignment() {
      Identifier = new Identifier() { Name = "BABOON"},
      Expression = new Identifier() { Name = "GIRAFFE" }
    },
    new Assignment() {
      Identifier = new Identifier() { Name = "TEXT"},
        Expression = new String() { Text = "Hello world!" }
    }
  }
}
```

# Documentation

Consult the [repository's wiki](https://github.com/christophevg/human-parser-generator/wiki) for more background, tutorials and annotated examples.
