// unit tests for Generator Model Factory
// author: Christophe VG <contact@christophe.vg>

using System;
using System.IO;
using NUnit.Framework;

using System.Collections.Generic;

using HumanParserGenerator.Generator; // for Model, Factory

[TestFixture]
public class GeneratorModelFactoryTests {

  private void importAndCompare(Model model, string expected) {
    Assert.AreEqual(
      expected.Replace(" ", "").Replace("\n", ""),
      model.ToString()
    );
  }
  
  private void importAndCompare(Grammar grammar, Model expected) {
    Model model = new Factory().Import(grammar).Model;
    Assert.AreEqual( expected.ToString(), model.ToString() );
  }

  [Test]
  public void testMinimalModelWithoutProperty() {
    // rule ::= "a" ;
    this.importAndCompare(
      new Grammar() {
        Rules = new List<Rule>() {
          new Rule() {
            Identifier = "rule",
            Expression = new StringExpression() { String = "a" }
          }
        }
      },
      new Model() {
        Entities = new List<Entity>() {
          new Entity() {
            Rule = new Rule() { 
              Identifier = "rule",
              Expression = new StringExpression() { 
                Name = null,
                String = "a"
              }
            },
            Name = "rule",
            Properties = new List<Property>().AsReadOnly(),
            ParseAction = new ConsumeString() {
              String = "a"
            },
            Supers = new HashSet<string>(),
            Subs = new HashSet<string>()
          }
        },
        RootName = "rule"
      }
    );
  }

  [Test]
  public void testMinimalModelWithProperty() {
    // rule ::= StringProperty @ "a" ;
    this.importAndCompare(
      new Grammar() {
        Rules = new List<Rule>() {
          new Rule() {
            Identifier = "rule",
            Expression = new StringExpression() {
              Name   = "StringProperty",
              String = "a"
            }
          }
        }
      },
      new Model() {
        Entities = new List<Entity>() {
          new Entity() {
            Rule = new Rule() { 
              Identifier = "rule",
              Expression = new StringExpression() { 
                Name = "StringProperty",
                String = "a"
              }
            },
            Name = "rule",
            Properties = new List<Property>() {
              new Property() {
                Name = "StringProperty",
                Source = new ConsumeString() { String = "a" }
              }
            }.AsReadOnly(),
            ParseAction = new ConsumeString() { String = "a" },
            Supers = new HashSet<string>(),
            Subs = new HashSet<string>()
          }
        },
        RootName = "rule"
      }
    );
  }

  [Test]
  public void testSimpleIdentifierExpressionIndirection() {
    // rule1 ::= rule2 ;
    // rule2 ::= ? /a/ ? ;
    this.importAndCompare(
      new Grammar() {
        Rules = new List<Rule>() {
          new Rule() {
            Identifier = "rule1",
            Expression = new IdentifierExpression() { Identifier = "rule2" }
          },
          new Rule() {
            Identifier = "rule2",
            Expression = new ExtractorExpression() { Pattern = "a" }
          }
        }
      },
      new Model() {
        Entities = new List<Entity>() {
          new Entity() {
            Rule = new Rule() {
              Identifier = "rule1",
              Expression = new IdentifierExpression() {
                Name = null,
                Identifier = "rule2"
              }
            },
            Name = "rule1",
            Properties = new List<Property>() {
              new Property() {
                Name = "rule2",
                Source = new ConsumeEntity() { Reference = "rule2" }
              }
            } .AsReadOnly(),
            ParseAction = new ConsumeEntity() { Reference = "rule2" },
            Supers = new HashSet<string>(),
            Subs = new HashSet<string>() { "rule2" }
          },
          // virtual
          new Entity() {
            Rule = new Rule() {
              Identifier = "rule2",
              Expression = new ExtractorExpression() {
                Name = null,
                Pattern = "a"
              }
            },
            Name = "rule2",
            Properties = new List<Property>() {
              new Property() {
                Name = "rule2",
                Source = new ConsumePattern() { Pattern = "a" }
              }
            } .AsReadOnly(),
            ParseAction = new ConsumePattern() { Pattern = "a" },
            Supers = new HashSet<string>() { "rule1" },
            Subs = new HashSet<string>()
          }
        },
        RootName = "rule1"
      }
    );
  }

  [Test]
  public void testMinimalExtractorWithoutProperty() {
    // rule ::= ? /[A-Za-z0-9-]*/ ? ;
    this.importAndCompare(
      new Grammar() {
        Rules = new List<Rule>() {
          new Rule() {
            Identifier = "rule",
            Expression = new ExtractorExpression() { Pattern = "[A-Za-z0-9-]*" }
          }
        }
      },
      new Model() {
        Entities = new List<Entity>() {
          new Entity() {
            Rule = new Rule() {
              Identifier = "rule",
              Expression = new ExtractorExpression() {
                Name = null,
                Pattern = "[A-Za-z0-9-]*"
              }
            },
            Name = "rule",
            Properties = new List<Property>() {
              new Property() {
                Name = "rule",
                Source = new ConsumePattern() { Pattern = "[A-Za-z0-9-]*" }
              }
            } .AsReadOnly(),
            ParseAction = new ConsumePattern() { Pattern = "[A-Za-z0-9-]*" },
            Supers = new HashSet<string>(),
            Subs = new HashSet<string>()
          }
        },
        RootName = "rule"
      }
    );
  }

  [Test]
  public void testMinimalExtractorWithProperty() {
    // rule ::= PatternProperty @ ? /[A-Za-z0-9-]*/ ? ;
    this.importAndCompare(
      new Grammar() {
        Rules = new List<Rule>() {
          new Rule() {
            Identifier = "rule",
            Expression = new ExtractorExpression() {
              Name  = "PatternProperty",
              Pattern = "[A-Za-z0-9-]*"
            }
          }
        }
      },
      new Model() {
        Entities = new List<Entity>() {
          new Entity() {
            Rule = new Rule() {
              Identifier = "rule",
              Expression = new ExtractorExpression() {
                Name = "PatternProperty",
                Pattern = "[A-Za-z0-9-]*"
              }
            },
            Name = "rule",
            Properties = new List<Property>() {
              new Property() {
                Name = "PatternProperty",
                Source = new ConsumePattern() {
                  Pattern = "[A-Za-z0-9-]*"
                }
              }
            } .AsReadOnly(),
            ParseAction = new ConsumePattern() {
              Pattern = "[A-Za-z0-9-]*"
            },
            Supers = new HashSet<string>(),
            Subs = new HashSet<string>()
          }
        },
        RootName = "rule"
      }
    );
  }

  [Test]
  public void testNamedIdentifierExpression() {
    // rule1 ::= IdentifierProperty @ rule2 ;
    // rule2 ::= ? /a/ ? ;
    this.importAndCompare(
      new Grammar() {
        Rules = new List<Rule>() {
          new Rule() {
            Identifier = "rule1",
            Expression = new IdentifierExpression() {
              Name       = "IdentifierProperty",
              Identifier = "rule2"
            }
          },
          new Rule() {
            Identifier = "rule2",
            Expression = new ExtractorExpression() { Pattern = "a" }
          }
        }
      },
      new Model() {
        Entities = new List<Entity>() {
          new Entity() {
            Rule = new Rule() {
              Identifier = "rule1",
              Expression = new IdentifierExpression() {
                Name = "IdentifierProperty",
                Identifier = "rule2"
              }
            },
            Name = "rule1",
            Properties = new List<Property>() {
              new Property() {
                Name = "IdentifierProperty",
                Source = new ConsumeEntity() { Reference = "rule2" }
              }
            } .AsReadOnly(),
            ParseAction = new ConsumeEntity() { Reference = "rule2" },
            Supers = new HashSet<string>(),
            Subs = new HashSet<string>() { "rule2" }
          },
          // virtual
          new Entity() {
            Rule = new Rule() {
              Identifier = "rule2",
              Expression = new ExtractorExpression() {
                Name = null,
                Pattern = "a"
              }
            },
            Name = "rule2",
            Properties = new List<Property>() {
              new Property() {
                Name = "rule2",
                Source = new ConsumePattern() { Pattern = "a" }
              }
            } .AsReadOnly(),
            ParseAction = new ConsumePattern() { Pattern = "a" },
            Supers = new HashSet<string>() { "rule1" },
            Subs = new HashSet<string>()
          }
        },
        RootName = "rule1"
      }
    );
  }

  [Test]
  public void testOptionalString() {
    // rule ::= [ "a" ] ;
    this.importAndCompare(
      new Grammar() {
        Rules = new List<Rule>() {
          new Rule() {
            Identifier = "rule",
            Expression = new OptionalExpression() {
              Expression = new StringExpression() { String = "a" }
            }
          }
        }
      },
      new Model() {
        Entities = new List<Entity>() {
          new Entity() {
            Rule = new Rule() {
              Identifier = "rule",
              Expression = new OptionalExpression() {
                Expression = new StringExpression() {
                  Name = null,
                  String = "a"
                }
              }
            },
            Name = "rule",
            Properties = new List<Property>() {
              new Property() {
                Name = "has-a",
                Source = new ConsumeString() {
                  IsOptional = true, ReportSuccess = true,
                  String = "a"
                }
              }
            } .AsReadOnly(),
            ParseAction = new ConsumeString() {
              IsOptional = true, ReportSuccess = true,
              String = "a"
            },
            Supers = new HashSet<string>(),
            Subs = new HashSet<string>()
          }
        },
        RootName = "rule"
      }
    );
  }

  [Test]
  public void testSimpleSequentialExpression() {
    // rule1 ::= rule2 "." rule2 ;
    // rule2 ::= ? /[a-z]+/ ? ;
    this.importAndCompare(
      new Grammar() {
        Rules = new List<Rule>() {
          new Rule() {
            Identifier = "rule1",
            Expression = new SequentialExpression() {
              AtomicExpression          = new IdentifierExpression() { Identifier = "rule2" },
              NonAlternativesExpression = new SequentialExpression() {
                AtomicExpression          = new StringExpression() { String = "." },
                NonAlternativesExpression = new IdentifierExpression() { Identifier = "rule2" }
              }
            }
          },
          new Rule() {
            Identifier = "rule2",
            Expression = new ExtractorExpression() { Pattern = "[a-z]+" }
          }
        }
      },
      new Model() {
        Entities = new List<Entity>() {
          new Entity() {
            Rule = new Rule() {
              Identifier = "rule1",
              Expression = new SequentialExpression() {
                AtomicExpression = new IdentifierExpression() {
                  Name = null,
                  Identifier = "rule2"
                },
                NonAlternativesExpression = new SequentialExpression() {
                  AtomicExpression = new StringExpression() {
                    Name = null,
                    String = "."
                  },
                  NonAlternativesExpression = new IdentifierExpression() {
                    Name = null,
                    Identifier = "rule2"
                  }
                }
              }
            },
            Name = "rule1",
            Properties = new List<Property>() {
              new Property() {
                Name = "rule2",
                Source = new ConsumeEntity() { Reference = "rule2" }
              },
              new Property() {
                Name = "rule2",
                Source = new ConsumeEntity() { Reference = "rule2" }
              }
            } .AsReadOnly(),
            ParseAction = new ConsumeAll() {
              Actions = new List<ParseAction>() {
                new ConsumeEntity() { Reference = "rule2" },
                new ConsumeString() { String = "." },
                new ConsumeEntity() { Reference = "rule2" }
              }
            },
            Supers = new HashSet<string>(),
            Subs = new HashSet<string>()
          },
          // virtual
          new Entity() {
            Rule = new Rule() {
              Identifier = "rule2",
              Expression = new ExtractorExpression() {
                Name = null,
                Pattern = "[a-z]+"
              }
            },
            Name = "rule2",
            Properties = new List<Property>() {
              new Property() {
                Name = "rule2",
                Source = new ConsumePattern() { Pattern = "[a-z]+" }
              }
            } .AsReadOnly(),
            ParseAction = new ConsumePattern() { Pattern = "[a-z]+" },
            Supers = new HashSet<string>(),
            Subs = new HashSet<string>()
          }
        },
        RootName = "rule1"
      }
    );
  }

  [Test]
  public void testNamedIdentifierDefinition() {
    // rule ::= [ name ] id ;
    // name ::= id "@" ;
    // id   ::= ? /[a-z]+/ ? ;
    this.importAndCompare(
      new Grammar() {
        Rules = new List<Rule>() {
          new Rule() {
            Identifier = "rule",
            Expression = new SequentialExpression() {
              AtomicExpression = new OptionalExpression() {
                Expression = new IdentifierExpression() { Identifier = "name" }
              },
              NonAlternativesExpression = new IdentifierExpression() { Identifier = "id" }
            }
          },
          new Rule() {
            Identifier = "name",
            Expression = new SequentialExpression() {
              AtomicExpression          = new IdentifierExpression() { Identifier = "id" },
              NonAlternativesExpression = new StringExpression() { String = "@" }
            }
          },
          new Rule() {
            Identifier = "id",
            Expression = new ExtractorExpression() { Pattern = "[a-z]+" }
          }
        }
      },
      new Model() {
        Entities = new List<Entity>() {
          new Entity() {
            Rule = new Rule() {
              Identifier = "rule",
              Expression = new SequentialExpression() {
                AtomicExpression = new OptionalExpression() {
                  Expression = new IdentifierExpression() {
                    Name = null,
                    Identifier = "name"
                  }
                },
                NonAlternativesExpression = new IdentifierExpression() {
                  Name = null,
                  Identifier = "id"
                }
              }
            },
            Name = "rule",
            Properties = new List<Property>() {
              new Property() {
                Name = "name",
                Source = new ConsumeEntity() {
                  IsOptional = true,
                  Reference = "name"
                }
              },
              new Property() {
                Name = "id",
                Source = new ConsumeEntity() { Reference = "id" }
              }
            } .AsReadOnly(),
            ParseAction = new ConsumeAll() {
              Actions = new List<ParseAction>() {
                new ConsumeEntity() {
                  IsOptional = true,
                  Reference = "name"
                },
                new ConsumeEntity() { Reference = "id" }
              }
            },
            Supers = new HashSet<string>(),
            Subs = new HashSet<string>()
          },
          // virtual
          new Entity() {
            Rule = new Rule() {
              Identifier = "name",
              Expression = new SequentialExpression() {
                AtomicExpression = new IdentifierExpression() {
                  Name = null,
                  Identifier = "id"
                },
                NonAlternativesExpression = new StringExpression() {
                  Name = null,
                  String = "@"
                }
              }
            },
            Name = "name",
            Properties = new List<Property>() {
              new Property() {
                Name = "id",
                Source = new ConsumeEntity() { Reference = "id" }
              }
            } .AsReadOnly(),
            ParseAction = new ConsumeAll() {
              Actions = new List<ParseAction>() {
                new ConsumeEntity() { Reference = "id" },
                new ConsumeString() { String = "@" }
              }
            },
            Supers = new HashSet<string>(),
            Subs = new HashSet<string>() { "id" }
          },
          // virtual
          new Entity() {
            Rule = new Rule() {
              Identifier = "id",
              Expression = new ExtractorExpression() {
                Name = null,
                Pattern = "[a-z]+"
              }
            },
            Name = "id",
            Properties = new List<Property>() {
              new Property() {
                Name = "id",
                Source = new ConsumePattern() { Pattern = "[a-z]+" }
              }
            } .AsReadOnly(),
            ParseAction = new ConsumePattern() { Pattern = "[a-z]+" },
            Supers = new HashSet<string>() { "name" },
            Subs = new HashSet<string>()
          }
        },
        RootName = "rule"
      }
    );
  }

  [Test]
  public void testAlternativeCharacters() {
    // rule ::= "a" | "b" | "c" ;
    this.importAndCompare(
      new Grammar() {
        Rules = new List<Rule>() {
          new Rule() {
            Identifier = "rule",
            Expression = new AlternativesExpression() {
              NonAlternativesExpression = new StringExpression() { String = "a" },
              Expression                = new AlternativesExpression() {
                NonAlternativesExpression = new StringExpression() { String = "b" },
                Expression                = new StringExpression() { String = "c" }
              }
            }
          }
        }
      },
      new Model() {
        Entities = new List<Entity>() {
          new Entity() {
            Rule = new Rule() {
              Identifier = "rule",
              Expression = new AlternativesExpression() {
                NonAlternativesExpression = new StringExpression() {
                  Name = null,
                  String = "a"
                },
                Expression = new AlternativesExpression() {
                  NonAlternativesExpression = new StringExpression() {
                    Name = null,
                    String = "b"
                  },
                  Expression = new StringExpression() {
                    Name = null,
                    String = "c"
                  }
                }
              }
            },
            Name = "rule",
            Properties = new List<Property>() {
              new Property() {
                Name = "has-a",
                Source = new ConsumeString() {
                  ReportSuccess = true,
                  String = "a"
                }
              },
              new Property() {
                Name = "has-b",
                Source = new ConsumeString() {
                  ReportSuccess = true,
                  String = "b"
                }
              },
              new Property() {
                Name = "has-c",
                Source = new ConsumeString() {
                  ReportSuccess = true,
                  String = "c"
                }
              }
            } .AsReadOnly(),
            ParseAction = new ConsumeAny() {
              Actions = new List<ParseAction>() {
                new ConsumeString() {
                  ReportSuccess = true,
                  String = "a"
                },
                new ConsumeString() {
                  ReportSuccess = true,
                  String = "b"
                },
                new ConsumeString() {
                  ReportSuccess = true,
                  String = "c"
                }
              }
            },
            Supers = new HashSet<string>(),
            Subs = new HashSet<string>()
          }
        },
        RootName = "rule"
      }
    );
  }

  [Test]
  public void testAlternativeGroupedCharacters() {
    // rule ::= ( "a" "b" ) | ( "c" "d" ) ;
    this.importAndCompare(
      new Grammar() {
        Rules = new List<Rule>() {
          new Rule() {
            Identifier = "rule",
            Expression = new AlternativesExpression() {
              NonAlternativesExpression = new GroupExpression() {
                Expression = new SequentialExpression() {
                  AtomicExpression          = new StringExpression() { String = "a" },
                  NonAlternativesExpression = new StringExpression() { String = "b" }
                }
              },
              Expression = new GroupExpression() {
                Expression = new SequentialExpression() {
                  AtomicExpression          = new StringExpression() { String = "c" },
                  NonAlternativesExpression = new StringExpression() { String = "d" }
                }
              }
            }
          }
        }
      },
      new Model() {
        Entities = new List<Entity>() {
          new Entity() {
            Rule = new Rule() {
              Identifier = "rule",
              Expression = new AlternativesExpression() {
                NonAlternativesExpression = new GroupExpression() {
                  Expression = new SequentialExpression() {
                    AtomicExpression = new StringExpression() {
                      Name = null,
                      String = "a"
                    },
                    NonAlternativesExpression = new StringExpression() {
                      Name = null,
                      String = "b"
                    }
                  }
                },
                Expression = new GroupExpression() {
                  Expression = new SequentialExpression() {
                    AtomicExpression = new StringExpression() {
                      Name = null,
                      String = "c"
                    },
                    NonAlternativesExpression = new StringExpression() {
                      Name = null,
                      String = "d"
                    }
                  }
                }
              }
            },
            Name = "rule",
            Properties = new List<Property>().AsReadOnly(),
            ParseAction = new ConsumeAny() {
              Actions = new List<ParseAction>() {
                new ConsumeAll() {
                  Actions = new List<ParseAction>() {
                    new ConsumeString() {
                      String = "a"
                    },
                    new ConsumeString() {
                      String = "b"
                    }
                  }
                },
                new ConsumeAll() {
                  Actions = new List<ParseAction>() {
                    new ConsumeString() {
                      String = "c"
                    },
                    new ConsumeString() {
                      String = "d"
                    }
                  }
                }
              }
            },
            Supers = new HashSet<string>(),
            Subs = new HashSet<string>()
          }
        },
        RootName = "rule"
      }
    );
  }

  [Test]
  public void testRepeatedString() {
    // rule ::= { "a" } ;
    this.importAndCompare(
      new Grammar() {
        Rules = new List<Rule>() {
          new Rule() {
            Identifier = "rule",
            Expression = new RepetitionExpression() {
              Expression = new StringExpression() { String = "a" }
            }
          }
        }
      },
      new Model() {
        Entities = new List<Entity>() {
          new Entity() {
            Rule = new Rule() {
              Identifier = "rule",
              Expression = new RepetitionExpression() {
                Expression = new StringExpression() {
                  Name = null,
                  String = "a"
                }
              }
            },
            Name = "rule",
            Properties = new List<Property>().AsReadOnly(),
            ParseAction = new ConsumeString() {
              IsPlural = true,
              String = "a"
            },
            Supers = new HashSet<string>(),
            Subs = new HashSet<string>()
          }
        },
        RootName = "rule"
      }
    );
  }

  [Test]
  public void testRepeatedIdentifier() {
    // rules ::= { rule } ;
    // rule  ::= id "x" id "=" id ;
    // id    ::= ? /[a-z]+/ ? ;
    this.importAndCompare(
      new Grammar() {
        Rules = new List<Rule>() {
          new Rule() {
            Identifier = "rules",
            Expression = new RepetitionExpression() {
              Expression = new IdentifierExpression() { Identifier = "rule" }
            }
          },
          new Rule() {
            Identifier = "rule",
            Expression = new SequentialExpression() {
              AtomicExpression          = new IdentifierExpression() { Identifier = "id" },
              NonAlternativesExpression = new SequentialExpression() {
                AtomicExpression          = new StringExpression() { String = "x" },
                NonAlternativesExpression = new SequentialExpression() {
                  AtomicExpression          = new IdentifierExpression() { Identifier = "id" },
                  NonAlternativesExpression = new SequentialExpression() {
                    AtomicExpression          = new StringExpression() { String = "=" },
                    NonAlternativesExpression = new IdentifierExpression() { Identifier = "id" }
                  }
                }
              }
            }
          },
          new Rule() {
            Identifier = "id",
            Expression = new ExtractorExpression() { Pattern = "[a-z]+" }
          }
        }
      },
      new Model() {
        Entities = new List<Entity>() {
          new Entity() {
            Rule = new Rule() {
              Identifier = "rules",
              Expression = new RepetitionExpression() {
                Expression = new IdentifierExpression() {
                  Name = null,
                  Identifier = "rule"
                }
              }
            },
            Name = "rules",
            Properties = new List<Property>() {
              new Property() {
                Name = "rule",
                Source = new ConsumeEntity() {
                  IsPlural = true,
                  Reference = "rule"
                }
              }
            } .AsReadOnly(),
            ParseAction = new ConsumeEntity() {
              IsPlural = true,
              Reference = "rule"
            },
            Supers = new HashSet<string>(),
            Subs = new HashSet<string>()
          },
          new Entity() {
            Rule = new Rule() {
              Identifier = "rule",
              Expression = new SequentialExpression() {
                AtomicExpression = new IdentifierExpression() {
                  Name = null,
                  Identifier = "id"
                },
                NonAlternativesExpression = new SequentialExpression() {
                  AtomicExpression = new StringExpression() {
                    Name = null,
                    String = "x"
                  },
                  NonAlternativesExpression = new SequentialExpression() {
                    AtomicExpression = new IdentifierExpression() {
                      Name = null,
                      Identifier = "id"
                    },
                    NonAlternativesExpression = new SequentialExpression() {
                      AtomicExpression = new StringExpression() {
                        Name = null,
                        String = "="
                      },
                      NonAlternativesExpression = new IdentifierExpression() {
                        Name = null,
                        Identifier = "id"
                      }
                    }
                  }
                }
              }
            },
            Name = "rule",
            Properties = new List<Property>() {
              new Property() {
                Name = "id",
                Source = new ConsumeEntity() { Reference = "id" }
              },
              new Property() {
                Name = "id",
                Source = new ConsumeEntity() { Reference = "id" }
              },
              new Property() {
                Name = "id",
                Source = new ConsumeEntity() { Reference = "id" }
              }
            } .AsReadOnly(),
            ParseAction = new ConsumeAll() {
              Actions = new List<ParseAction>() {
                new ConsumeEntity() { Reference = "id" },
                new ConsumeString() { String = "x" },
                new ConsumeEntity() { Reference = "id" },
                new ConsumeString() { String = "=" },
                new ConsumeEntity() { Reference = "id" }
              }
            },
            Supers = new HashSet<string>(),
            Subs = new HashSet<string>()
          },
          // virtual
          new Entity() {
            Rule = new Rule() {
              Identifier = "id",
              Expression = new ExtractorExpression() {
                Name = null,
                Pattern = "[a-z]+"
              }
            },
            Name = "id",
            Properties = new List<Property>() {
              new Property() {
                Name = "id",
                Source = new ConsumePattern() {
                  Pattern = "[a-z]+"
                }
              }
            } .AsReadOnly(),
            ParseAction = new ConsumePattern() {
              Pattern = "[a-z]+"
            },
            Supers = new HashSet<string>(),
            Subs = new HashSet<string>()
          }
        },
        RootName = "rules"
      }
    );
  }

  [Test]
  public void testAlternativeCharactersIdentifier() {
    // rule1 ::= { rule2 } ;
    // rule2 ::= "a" | "b" | "c" ;
    this.importAndCompare(
      new Grammar() {
        Rules = new List<Rule>() {
          new Rule() {
            Identifier = "rule1",
            Expression = new RepetitionExpression() {
              Expression = new IdentifierExpression() { Identifier = "rule2" }
            }
          },
          new Rule() {
            Identifier = "rule2",
            Expression = new AlternativesExpression() {
              NonAlternativesExpression = new StringExpression() { String = "a" },
              Expression                = new AlternativesExpression() {
                NonAlternativesExpression = new StringExpression() { String = "b" },
                Expression =                new StringExpression() { String = "c" }
              }
            }
          }
        }
      },
      new Model() {
        Entities = new List<Entity>() {
          new Entity() {
            Rule = new Rule() {
              Identifier = "rule1",
              Expression = new RepetitionExpression() {
                Expression = new IdentifierExpression() {
                  Name = null,
                  Identifier = "rule2"
                }
              }
            },
            Name = "rule1",
            Properties = new List<Property>() {
              new Property() {
                Name = "rule2",
                Source = new ConsumeEntity() {
                  IsPlural = true,
                  Reference = "rule2"
                }
              }
            } .AsReadOnly(),
            ParseAction = new ConsumeEntity() {
              IsPlural = true,
              Reference = "rule2"
            },
            Supers = new HashSet<string>(),
            Subs = new HashSet<string>()
          },
          new Entity() {
            Rule = new Rule() {
              Identifier = "rule2",
              Expression = new AlternativesExpression() {
                NonAlternativesExpression = new StringExpression() {
                  Name = null,
                  String = "a"
                },
                Expression = new AlternativesExpression() {
                  NonAlternativesExpression = new StringExpression() {
                    Name = null,
                    String = "b"
                  },
                  Expression = new StringExpression() {
                    Name = null,
                    String = "c"
                  }
                }
              }
            },
            Name = "rule2",
            Properties = new List<Property>() {
              new Property() {
                Name = "has-a",
                Source = new ConsumeString() {
                  ReportSuccess = true,
                  String = "a"
                }
              },
              new Property() {
                Name = "has-b",
                Source = new ConsumeString() {
                  ReportSuccess = true,
                  String = "b"
                }
              },
              new Property() {
                Name = "has-c",
                Source = new ConsumeString() {
                  ReportSuccess = true,
                  String = "c"
                }
              }
            } .AsReadOnly(),
            ParseAction = new ConsumeAny() {
              Actions = new List<ParseAction>() {
                new ConsumeString() {
                  ReportSuccess = true,
                  String = "a"
                },
                new ConsumeString() {
                  ReportSuccess = true,
                  String = "b"
                },
                new ConsumeString() {
                  ReportSuccess = true,
                  String = "c"
                }
              }
            },
            Supers = new HashSet<string>(),
            Subs = new HashSet<string>()
          }
        },
        RootName = "rule1"
      }
    );
  }

  [Test]
  public void testAlternativeIdentifiers() {
    // assign ::= exp ;
    // exp ::= a | b ;
    // a ::= ? /aa/ ? ;
    // b ::= ? /bb/ ?;
    this.importAndCompare(
      new Grammar() {
        Rules = new List<Rule>() {
          new Rule() {
            Identifier = "assign",
            Expression = new IdentifierExpression() { Identifier = "exp" }
          },
          new Rule() {
            Identifier = "exp",
            Expression = new AlternativesExpression() {
              NonAlternativesExpression = new IdentifierExpression() { Identifier = "a" },
              Expression                = new IdentifierExpression() { Identifier = "b"}
            }
          },
          new Rule() {
            Identifier = "a",
            Expression = new ExtractorExpression() { Pattern = "aa" }
          },
          new Rule() {
            Identifier = "b",
            Expression = new ExtractorExpression() { Pattern = "bb" }
          }
        }
      },
      new Model() {
        Entities = new List<Entity>() {
          new Entity() {
            Rule = new Rule() {
              Identifier = "assign",
              Expression = new IdentifierExpression() {
                Name = null,
                Identifier = "exp"
              }
            },
            Name = "assign",
            Properties = new List<Property>() {
              new Property() {
                Name = "exp",
                Source = new ConsumeEntity() {
                  Reference = "exp"
                }
              }
            } .AsReadOnly(),
            ParseAction = new ConsumeEntity() {
              Reference = "exp"
            },
            Supers = new HashSet<string>(),
            Subs = new HashSet<string>() { "exp" }
          },
          // virtual
          new Entity() {
            Rule = new Rule() {
              Identifier = "exp",
              Expression = new AlternativesExpression() {
                NonAlternativesExpression = new IdentifierExpression() {
                  Name = null,
                  Identifier = "a"
                },
                Expression = new IdentifierExpression() {
                  Name = null,
                  Identifier = "b"
                }
              }
            },
            Name = "exp",
            Properties = new List<Property>() {
              new Property() {
                Name = "alternative",
                Source = new ConsumeAny() {
                  Actions = new List<ParseAction>() {
                    new ConsumeEntity() {
                      Reference = "a"
                    },
                    new ConsumeEntity() {
                      Reference = "b"
                    }
                  }
                }
              }
            } .AsReadOnly(),
            ParseAction = new ConsumeAny() {
              Actions = new List<ParseAction>() {
                new ConsumeEntity() {
                  Reference = "a"
                },
                new ConsumeEntity() {
                  Reference = "b"
                }
              }
            },
            Supers = new HashSet<string>() {
              "assign"
            },
            Subs = new HashSet<string>() {
              "a", "b"
            }
          },
          new Entity() {
            Rule = new Rule() {
              Identifier = "a",
              Expression = new ExtractorExpression() {
                Name = null,
                Pattern = "aa"
              }
            },
            Name = "a",
            Properties = new List<Property>() {
              new Property() {
                Name = "a",
                Source = new ConsumePattern() {
                  Pattern = "aa"
                }
              }
            } .AsReadOnly(),
            ParseAction = new ConsumePattern() {
              Pattern = "aa"
            },
            Supers = new HashSet<string>() {
              "exp"
            },
            Subs = new HashSet<string>()
          },
          new Entity() {
            Rule = new Rule() {
              Identifier = "b",
              Expression = new ExtractorExpression() {
                Name = null,
                Pattern = "bb"
              }
            },
            Name = "b",
            Properties = new List<Property>() {
              new Property() {
                Name = "b",
                Source = new ConsumePattern() {
                  Pattern = "bb"
                }
              }
            } .AsReadOnly(),
            ParseAction = new ConsumePattern() {
              Pattern = "bb"
            },
            Supers = new HashSet<string>() {
              "exp"
            },
            Subs = new HashSet<string>()
          }
        },
        RootName = "assign"
      }
    );
  }

  [Test]
  public void testPropertyTypesWithInheritance() {
    // assign ::= exp ;
    // exp ::= a | b ;
    // a ::= b exp ;
    // b ::= x | y ;
    // x ::= ? /xx/ ? ;
    // y ::= ? /yy/ ? ;
    this.importAndCompare(
      new Grammar() {
        Rules = new List<Rule>() {
          new Rule() {
            Identifier = "assign",
            Expression = new IdentifierExpression() { Identifier = "exp" }
          },
          new Rule() {
            Identifier = "exp",
            Expression = new AlternativesExpression() {
              NonAlternativesExpression = new IdentifierExpression() { Identifier = "a" },
              Expression                = new IdentifierExpression() { Identifier = "b"}
            }
          },
          new Rule() {
            Identifier = "a",
            Expression = new SequentialExpression() {
              AtomicExpression          = new IdentifierExpression() { Identifier = "b"   },
              NonAlternativesExpression = new IdentifierExpression() { Identifier = "exp" }
            }
          },
          new Rule() {
            Identifier = "b",
            Expression = new AlternativesExpression() {
              NonAlternativesExpression = new IdentifierExpression() { Identifier = "x" },
              Expression                = new IdentifierExpression() { Identifier = "y"}
            }
          },
          new Rule() {
            Identifier = "x",
            Expression = new ExtractorExpression() { Pattern = "xx" }
          },
          new Rule() {
            Identifier = "y",
            Expression = new ExtractorExpression() { Pattern = "yy" }
          }
        }
      },
      new Model() {
        Entities = new List<Entity>() {
          new Entity() {
            Rule = new Rule() {
              Identifier = "assign",
              Expression = new IdentifierExpression() {
                Name = null,
                Identifier = "exp"
              }
            },
            Name = "assign",
            Properties = new List<Property>() {
              new Property() {
                Name = "exp",
                Source = new ConsumeEntity() { Reference = "exp" }
              }
            } .AsReadOnly(),
            ParseAction = new ConsumeEntity() { Reference = "exp" },
            Supers = new HashSet<string>(),
            Subs = new HashSet<string>() { "exp" }
          },
          // virtual
          new Entity() {
            Rule = new Rule() {
              Identifier = "exp",
              Expression = new AlternativesExpression() {
                NonAlternativesExpression = new IdentifierExpression() {
                  Name = null,
                  Identifier = "a"
                },
                Expression = new IdentifierExpression() {
                  Name = null,
                  Identifier = "b"
                }
              }
            },
            Name = "exp",
            Properties = new List<Property>() {
              new Property() {
                Name = "alternative",
                Source = new ConsumeAny() {
                  Actions = new List<ParseAction>() {
                    new ConsumeEntity() { Reference = "a" },
                    new ConsumeEntity() { Reference = "b" }
                  }
                }
              }
            } .AsReadOnly(),
            ParseAction = new ConsumeAny() {
              Actions = new List<ParseAction>() {
                new ConsumeEntity() { Reference = "a" },
                new ConsumeEntity() { Reference = "b" }
              }
            },
            Supers = new HashSet<string>() { "assign" },
            Subs = new HashSet<string>() { "a", "b" }
          },
          new Entity() {
            Rule = new Rule() {
              Identifier = "a",
              Expression = new SequentialExpression() {
                AtomicExpression = new IdentifierExpression() {
                  Name = null,
                  Identifier = "b"
                },
                NonAlternativesExpression = new IdentifierExpression() {
                  Name = null,
                  Identifier = "exp"
                }
              }
            },
            Name = "a",
            Properties = new List<Property>() {
              new Property() {
                Name = "b",
                Source = new ConsumeEntity() {
                  Reference = "b"
                }
              },
              new Property() {
                Name = "exp",
                Source = new ConsumeEntity() {
                  Reference = "exp"
                }
              }
            } .AsReadOnly(),
            ParseAction = new ConsumeAll() {
              Actions = new List<ParseAction>() {
                new ConsumeEntity() { Reference = "b"   },
                new ConsumeEntity() { Reference = "exp" }
              }
            },
            Supers = new HashSet<string>() { "exp" },
            Subs = new HashSet<string>()
          },
          // virtual
          new Entity() {
            Rule = new Rule() {
              Identifier = "b",
              Expression = new AlternativesExpression() {
                NonAlternativesExpression = new IdentifierExpression() {
                  Name = null,
                  Identifier = "x"
                },
                Expression = new IdentifierExpression() {
                  Name = null,
                  Identifier = "y"
                }
              }
            },
            Name = "b",
            Properties = new List<Property>() {
              new Property() {
                Name = "alternative",
                Source = new ConsumeAny() {
                  Actions = new List<ParseAction>() {
                    new ConsumeEntity() { Reference = "x" },
                    new ConsumeEntity() { Reference = "y" }
                  }
                }
              }
            } .AsReadOnly(),
            ParseAction = new ConsumeAny() {
              Actions = new List<ParseAction>() {
                new ConsumeEntity() { Reference = "x" },
                new ConsumeEntity() { Reference = "y" }
              }
            },
            Supers = new HashSet<string>() { "exp" },
            Subs = new HashSet<string>() { "x", "y" }
          },
          new Entity() {
            Rule = new Rule() {
              Identifier = "x",
              Expression = new ExtractorExpression() {
                Name = null,
                Pattern = "xx"
              }
            },
            Name = "x",
            Properties = new List<Property>() {
              new Property() {
                Name = "x",
                Source = new ConsumePattern() { Pattern = "xx" }
              }
            } .AsReadOnly(),
            ParseAction = new ConsumePattern() { Pattern = "xx" },
            Supers = new HashSet<string>() { "b" },
            Subs = new HashSet<string>()
          },
          new Entity() {
            Rule = new Rule() {
              Identifier = "y",
              Expression = new ExtractorExpression() {
                Name = null,
                Pattern = "yy"
              }
            },
            Name = "y",
            Properties = new List<Property>() {
              new Property() {
                Name = "y",
                Source = new ConsumePattern() { Pattern = "yy" }
              }
            } .AsReadOnly(),
            ParseAction = new ConsumePattern() { Pattern = "yy" },
            Supers = new HashSet<string>() { "b" },
            Subs = new HashSet<string>()
          }
        },
        RootName = "assign"
      }
    );
  }

  [Test]
  public void testMixedStringIdentifierAlternatives() {
    // record     ::= { value } ;
    // value      ::= literal | variable ;
    // literal    ::= number | string;
    // variable   ::= identifier "x" number ;
    // number     ::= /[0-9]+/
    // string     ::= /[a-z]+/
    // identifier ::= /[a-z]+/
    this.importAndCompare(
      new Grammar() {
        Rules = new List<Rule>() {
          new Rule() {
            Identifier = "record",
            Expression = new RepetitionExpression() {
              Expression = new IdentifierExpression() { Identifier = "value" }
            }
          },
          new Rule() {
            Identifier = "value",
            Expression = new AlternativesExpression() {
              NonAlternativesExpression = new IdentifierExpression() { Identifier = "literal" },
              Expression                = new IdentifierExpression() { Identifier = "variable"}
            }
          },
          new Rule() {
            Identifier = "literal",
            Expression = new AlternativesExpression() {
              NonAlternativesExpression = new IdentifierExpression() { Identifier = "number" },
              Expression                = new IdentifierExpression() { Identifier = "string"}
            }
          },
          new Rule() {
            Identifier = "variable",
            Expression = new SequentialExpression() {
              AtomicExpression          = new IdentifierExpression() { Identifier = "identifier"},
              NonAlternativesExpression = new SequentialExpression() {
                AtomicExpression          = new StringExpression() { String = "x" },
                NonAlternativesExpression = new IdentifierExpression() { Identifier = "number" }
              }
            }
          },
          new Rule() {
            Identifier = "number",
            Expression = new ExtractorExpression() { Pattern = "[0-9]+" }
          },
          new Rule() {
            Identifier = "string",
            Expression = new ExtractorExpression() { Pattern = "[a-z]+" }
          },
          new Rule() {
            Identifier = "identifier",
            Expression = new ExtractorExpression() { Pattern = "[a-z]+" }
          }
        }
      },
      new Model() {
        Entities = new List<Entity>() {
          new Entity() {
            Rule = new Rule() {
              Identifier = "record",
              Expression = new RepetitionExpression() {
                Expression = new IdentifierExpression() {
                  Name = null,
                  Identifier = "value"
                }
              }
            },
            Name = "record",
            Properties = new List<Property>() {
              new Property() {
                Name = "value",
                Source = new ConsumeEntity() {
                  IsPlural = true,
                  Reference = "value"
                }
              }
            } .AsReadOnly(),
            ParseAction = new ConsumeEntity() {
              IsPlural = true,
              Reference = "value"
            },
            Supers = new HashSet<string>(),
            Subs = new HashSet<string>()
          },
          // virtual
          new Entity() {
            Rule = new Rule() {
              Identifier = "value",
              Expression = new AlternativesExpression() {
                NonAlternativesExpression = new IdentifierExpression() {
                  Name = null,
                  Identifier = "literal"
                },
                Expression = new IdentifierExpression() {
                  Name = null,
                  Identifier = "variable"
                }
              }
            },
            Name = "value",
            Properties = new List<Property>() {
              new Property() {
                Name = "alternative",
                Source = new ConsumeAny() {
                  Actions = new List<ParseAction>() {
                    new ConsumeEntity() {
                      Reference = "literal"
                    },
                    new ConsumeEntity() {
                      Reference = "variable"
                    }
                  }
                }
              }
            } .AsReadOnly(),
            ParseAction = new ConsumeAny() {
              Actions = new List<ParseAction>() {
                new ConsumeEntity() {
                  Reference = "literal"
                },
                new ConsumeEntity() {
                  Reference = "variable"
                }
              }
            },
            Supers = new HashSet<string>(),
            Subs = new HashSet<string>() {
              "literal", "variable"
            }
          },
          // virtual
          new Entity() {
            Rule = new Rule() {
              Identifier = "literal",
              Expression = new AlternativesExpression() {
                NonAlternativesExpression = new IdentifierExpression() {
                  Name = null,
                  Identifier = "number"
                },
                Expression = new IdentifierExpression() {
                  Name = null,
                  Identifier = "string"
                }
              }
            },
            Name = "literal",
            Properties = new List<Property>() {
              new Property() {
                Name = "alternative",
                Source = new ConsumeAny() {
                  Actions = new List<ParseAction>() {
                    new ConsumeEntity() {
                      Reference = "number"
                    },
                    new ConsumeEntity() {
                      Reference = "string"
                    }
                  }
                }
              }
            } .AsReadOnly(),
            ParseAction = new ConsumeAny() {
              Actions = new List<ParseAction>() {
                new ConsumeEntity() {
                  Reference = "number"
                },
                new ConsumeEntity() {
                  Reference = "string"
                }
              }
            },
            Supers = new HashSet<string>() {
              "value"
            },
            Subs = new HashSet<string>() {
              "number", "string"
            }
          },
          new Entity() {
            Rule = new Rule() {
              Identifier = "variable",
              Expression = new SequentialExpression() {
                AtomicExpression = new IdentifierExpression() {
                  Name = null,
                  Identifier = "identifier"
                },
                NonAlternativesExpression = new SequentialExpression() {
                  AtomicExpression = new StringExpression() {
                    Name = null,
                    String = "x"
                  },
                  NonAlternativesExpression = new IdentifierExpression() {
                    Name = null,
                    Identifier = "number"
                  }
                }
              }
            },
            Name = "variable",
            Properties = new List<Property>() {
              new Property() {
                Name = "identifier",
                Source = new ConsumeEntity() {
                  Reference = "identifier"
                }
              },
              new Property() {
                Name = "number",
                Source = new ConsumeEntity() {
                  Reference = "number"
                }
              }
            } .AsReadOnly(),
            ParseAction = new ConsumeAll() {
              Actions = new List<ParseAction>() {
                new ConsumeEntity() {
                  Reference = "identifier"
                },
                new ConsumeString() {
                  String = "x"
                },
                new ConsumeEntity() {
                  Reference = "number"
                }
              }
            },
            Supers = new HashSet<string>() {
              "value"
            },
            Subs = new HashSet<string>()
          },
          new Entity() {
            Rule = new Rule() {
              Identifier = "number",
              Expression = new ExtractorExpression() {
                Name = null,
                Pattern = "[0-9]+"
              }
            },
            Name = "number",
            Properties = new List<Property>() {
              new Property() {
                Name = "number",
                Source = new ConsumePattern() {
                  Pattern = "[0-9]+"
                }
              }
            } .AsReadOnly(),
            ParseAction = new ConsumePattern() {
              Pattern = "[0-9]+"
            },
            Supers = new HashSet<string>() {
              "literal"
            },
            Subs = new HashSet<string>()
          },
          new Entity() {
            Rule = new Rule() {
              Identifier = "string",
              Expression = new ExtractorExpression() {
                Name = null,
                Pattern = "[a-z]+"
              }
            },
            Name = "string",
            Properties = new List<Property>() {
              new Property() {
                Name = "string",
                Source = new ConsumePattern() {
                  Pattern = "[a-z]+"
                }
              }
            } .AsReadOnly(),
            ParseAction = new ConsumePattern() {
              Pattern = "[a-z]+"
            },
            Supers = new HashSet<string>() {
              "literal"
            },
            Subs = new HashSet<string>()
          },
          // virtual
          new Entity() {
            Rule = new Rule() {
              Identifier = "identifier",
              Expression = new ExtractorExpression() {
                Name = null,
                Pattern = "[a-z]+"
              }
            },
            Name = "identifier",
            Properties = new List<Property>() {
              new Property() {
                Name = "identifier",
                Source = new ConsumePattern() {
                  Pattern = "[a-z]+"
                }
              }
            } .AsReadOnly(),
            ParseAction = new ConsumePattern() {
              Pattern = "[a-z]+"
            },
            Supers = new HashSet<string>(),
            Subs = new HashSet<string>()
          }
        },
        RootName = "record"
      }
    );
  }

}
