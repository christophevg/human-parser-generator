// unit test for self hosting
// author: Christophe VG <contact@christophe.vg>

using System;
using System.IO;
using NUnit.Framework;

using System.Collections.Generic;

using HumanParserGenerator.Generator;

[TestFixture]
public class SelfHostingTests {

  private void processAndCompare(string input, Grammar grammar, Model model ) {
    Grammar g = new Parser().Parse(input).AST;
    Assert.AreEqual( grammar.ToString(), g.ToString() );

    Model m = new Factory().Import(grammar).Model;
    Assert.AreEqual( model.ToString(), m.ToString() );
  }

  [Test]
  public void testPascalGrammar() {
    this.processAndCompare(
      File.ReadAllText("../../generator/hpg.bnf"),
      new Grammar() {
        Rules = new List<Rule>() {
          new Rule() {
            Identifier = "grammar",
            Expression = new RepetitionExpression() {
              Expression = new IdentifierExpression() {
                Name = null,
                Identifier = "rule"
              }
            }
          },new Rule() {
            Identifier = "rule",
            Expression = new SequentialExpression() {
              AtomicExpression = new OptionalExpression() {
                Expression = new StringExpression() {
                  Name = "_",
                  String = "<"
                }
              },
              NonAlternativesExpression = new SequentialExpression() {
                AtomicExpression = new IdentifierExpression() {
                  Name = null,
                  Identifier = "identifier"
                },
                NonAlternativesExpression = new SequentialExpression() {
                  AtomicExpression = new OptionalExpression() {
                    Expression = new StringExpression() {
                      Name = "_",
                      String = ">"
                    }
                  },
                  NonAlternativesExpression = new SequentialExpression() {
                    AtomicExpression = new GroupExpression() {
                      Expression = new AlternativesExpression() {
                        NonAlternativesExpression = new StringExpression() {
                          Name = "_",
                          String = "::="
                        },
                        Expression = new StringExpression() {
                          Name = "_",
                          String = "="
                        }
                      }
                    },
                    NonAlternativesExpression = new SequentialExpression() {
                      AtomicExpression = new IdentifierExpression() {
                        Name = null,
                        Identifier = "expression"
                      },
                      NonAlternativesExpression = new GroupExpression() {
                        Expression = new AlternativesExpression() {
                          NonAlternativesExpression = new StringExpression() {
                            Name = "_",
                            String = ";"
                          },
                          Expression = new StringExpression() {
                            Name = "_",
                            String = "."
                          }
                        }
                      }
                    }
                  }
                }
              }
            }
          },new Rule() {
            Identifier = "expression",
            Expression = new AlternativesExpression() {
              NonAlternativesExpression = new IdentifierExpression() {
                Name = null,
                Identifier = "alternatives-expression"
              },
              Expression = new IdentifierExpression() {
                Name = null,
                Identifier = "non-alternatives-expression"
              }
            }
          },new Rule() {
            Identifier = "alternatives-expression",
            Expression = new SequentialExpression() {
              AtomicExpression = new IdentifierExpression() {
                Name = null,
                Identifier = "non-alternatives-expression"
              },
              NonAlternativesExpression = new SequentialExpression() {
                AtomicExpression = new StringExpression() {
                  Name = null,
                  String = "|"
                },
                NonAlternativesExpression = new IdentifierExpression() {
                  Name = null,
                  Identifier = "expression"
                }
              }
            }
          },new Rule() {
            Identifier = "non-alternatives-expression",
            Expression = new AlternativesExpression() {
              NonAlternativesExpression = new IdentifierExpression() {
                Name = null,
                Identifier = "sequential-expression"
              },
              Expression = new IdentifierExpression() {
                Name = null,
                Identifier = "atomic-expression"
              }
            }
          },new Rule() {
            Identifier = "sequential-expression",
            Expression = new SequentialExpression() {
              AtomicExpression = new IdentifierExpression() {
                Name = null,
                Identifier = "atomic-expression"
              },
              NonAlternativesExpression = new SequentialExpression() {
                AtomicExpression = new OptionalExpression() {
                  Expression = new StringExpression() {
                    Name = "_",
                    String = ","
                  }
                },
                NonAlternativesExpression = new IdentifierExpression() {
                  Name = null,
                  Identifier = "non-alternatives-expression"
                }
              }
            }
          },new Rule() {
            Identifier = "atomic-expression",
            Expression = new AlternativesExpression() {
              NonAlternativesExpression = new IdentifierExpression() {
                Name = null,
                Identifier = "nested-expression"
              },
              Expression = new IdentifierExpression() {
                Name = null,
                Identifier = "terminal-expression"
              }
            }
          },new Rule() {
            Identifier = "nested-expression",
            Expression = new AlternativesExpression() {
              NonAlternativesExpression = new IdentifierExpression() {
                Name = null,
                Identifier = "optional-expression"
              },
              Expression = new AlternativesExpression() {
                NonAlternativesExpression = new IdentifierExpression() {
                  Name = null,
                  Identifier = "repetition-expression"
                },
                Expression = new IdentifierExpression() {
                  Name = null,
                  Identifier = "group-expression"
                }
              }
            }
          },new Rule() {
            Identifier = "optional-expression",
            Expression = new SequentialExpression() {
              AtomicExpression = new StringExpression() {
                Name = null,
                String = "["
              },
              NonAlternativesExpression = new SequentialExpression() {
                AtomicExpression = new IdentifierExpression() {
                  Name = null,
                  Identifier = "expression"
                },
                NonAlternativesExpression = new StringExpression() {
                  Name = null,
                  String = "]"
                }
              }
            }
          },new Rule() {
            Identifier = "repetition-expression",
            Expression = new SequentialExpression() {
              AtomicExpression = new StringExpression() {
                Name = null,
                String = "{"
              },
              NonAlternativesExpression = new SequentialExpression() {
                AtomicExpression = new IdentifierExpression() {
                  Name = null,
                  Identifier = "expression"
                },
                NonAlternativesExpression = new StringExpression() {
                  Name = null,
                  String = "}"
                }
              }
            }
          },new Rule() {
            Identifier = "group-expression",
            Expression = new SequentialExpression() {
              AtomicExpression = new StringExpression() {
                Name = null,
                String = "("
              },
              NonAlternativesExpression = new SequentialExpression() {
                AtomicExpression = new IdentifierExpression() {
                  Name = null,
                  Identifier = "expression"
                },
                NonAlternativesExpression = new StringExpression() {
                  Name = null,
                  String = ")"
                }
              }
            }
          },new Rule() {
            Identifier = "terminal-expression",
            Expression = new AlternativesExpression() {
              NonAlternativesExpression = new IdentifierExpression() {
                Name = null,
                Identifier = "identifier-expression"
              },
              Expression = new AlternativesExpression() {
                NonAlternativesExpression = new IdentifierExpression() {
                  Name = null,
                  Identifier = "string-expression"
                },
                Expression = new IdentifierExpression() {
                  Name = null,
                  Identifier = "extractor-expression"
                }
              }
            }
          },new Rule() {
            Identifier = "identifier-expression",
            Expression = new SequentialExpression() {
              AtomicExpression = new OptionalExpression() {
                Expression = new IdentifierExpression() {
                  Name = null,
                  Identifier = "name"
                }
              },
              NonAlternativesExpression = new SequentialExpression() {
                AtomicExpression = new OptionalExpression() {
                  Expression = new StringExpression() {
                    Name = "_",
                    String = "<"
                  }
                },
                NonAlternativesExpression = new SequentialExpression() {
                  AtomicExpression = new IdentifierExpression() {
                    Name = null,
                    Identifier = "identifier"
                  },
                  NonAlternativesExpression = new OptionalExpression() {
                    Expression = new StringExpression() {
                      Name = "_",
                      String = ">"
                    }
                  }
                }
              }
            }
          },new Rule() {
            Identifier = "string-expression",
            Expression = new SequentialExpression() {
              AtomicExpression = new OptionalExpression() {
                Expression = new IdentifierExpression() {
                  Name = null,
                  Identifier = "name"
                }
              },
              NonAlternativesExpression = new IdentifierExpression() {
                Name = null,
                Identifier = "string"
              }
            }
          },new Rule() {
            Identifier = "extractor-expression",
            Expression = new SequentialExpression() {
              AtomicExpression = new OptionalExpression() {
                Expression = new IdentifierExpression() {
                  Name = null,
                  Identifier = "name"
                }
              },
              NonAlternativesExpression = new SequentialExpression() {
                AtomicExpression = new StringExpression() {
                  Name = null,
                  String = "?"
                },
                NonAlternativesExpression = new SequentialExpression() {
                  AtomicExpression = new StringExpression() {
                    Name = null,
                    String = "/"
                  },
                  NonAlternativesExpression = new SequentialExpression() {
                    AtomicExpression = new IdentifierExpression() {
                      Name = null,
                      Identifier = "pattern"
                    },
                    NonAlternativesExpression = new SequentialExpression() {
                      AtomicExpression = new StringExpression() {
                        Name = null,
                        String = "/"
                      },
                      NonAlternativesExpression = new StringExpression() {
                        Name = null,
                        String = "?"
                      }
                    }
                  }
                }
              }
            }
          },new Rule() {
            Identifier = "name",
            Expression = new SequentialExpression() {
              AtomicExpression = new IdentifierExpression() {
                Name = null,
                Identifier = "identifier"
              },
              NonAlternativesExpression = new StringExpression() {
                Name = null,
                String = "@"
              }
            }
          },new Rule() {
            Identifier = "identifier",
            Expression = new ExtractorExpression() {
              Name = null,
              Pattern = "([A-Za-z_][A-Za-z0-9-_]*)"
            }
          },new Rule() {
            Identifier = "string",
            Expression = new ExtractorExpression() {
              Name = null,
              Pattern = "\"([^\"]*)\"|^'([^']*)'"
            }
          },new Rule() {
            Identifier = "pattern",
            Expression = new ExtractorExpression() {
              Name = null,
              Pattern = "(.*?)(?<keep>/\\s*\\?)"
            }
          },new Rule() {
            Identifier = "_",
            Expression = new ExtractorExpression() {
              Name = null,
              Pattern = "\\(\\*.*?\\*\\)"
            }
          }
        }
      },
      new Model() {
        Entities = new List<Entity>() {
          new Entity() {
            Rule = new Rule() {
              Identifier = "grammar",
              Expression = new RepetitionExpression() {
                Expression = new IdentifierExpression() {
                  Name = null,
                  Identifier = "rule"
                }
              }
            },
            Name = "grammar",
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
                AtomicExpression = new OptionalExpression() {
                  Expression = new StringExpression() {
                    Name = "_",
                    String = "<"
                  }
                },
                NonAlternativesExpression = new SequentialExpression() {
                  AtomicExpression = new IdentifierExpression() {
                    Name = null,
                    Identifier = "identifier"
                  },
                  NonAlternativesExpression = new SequentialExpression() {
                    AtomicExpression = new OptionalExpression() {
                      Expression = new StringExpression() {
                        Name = "_",
                        String = ">"
                      }
                    },
                    NonAlternativesExpression = new SequentialExpression() {
                      AtomicExpression = new GroupExpression() {
                        Expression = new AlternativesExpression() {
                          NonAlternativesExpression = new StringExpression() {
                            Name = "_",
                            String = "::="
                          },
                          Expression = new StringExpression() {
                            Name = "_",
                            String = "="
                          }
                        }
                      },
                      NonAlternativesExpression = new SequentialExpression() {
                        AtomicExpression = new IdentifierExpression() {
                          Name = null,
                          Identifier = "expression"
                        },
                        NonAlternativesExpression = new GroupExpression() {
                          Expression = new AlternativesExpression() {
                            NonAlternativesExpression = new StringExpression() {
                              Name = "_",
                              String = ";"
                            },
                            Expression = new StringExpression() {
                              Name = "_",
                              String = "."
                            }
                          }
                        }
                      }
                    }
                  }
                }
              }
            },
            Name = "rule",
            Properties = new List<Property>() {
              new Property() {
                Name = "identifier",
                Source = new ConsumeEntity() {
                  Reference = "identifier"
                }
              },
              new Property() {
                Name = "expression",
                Source = new ConsumeEntity() {
                  Reference = "expression"
                }
              }
            } .AsReadOnly(),
            ParseAction = new ConsumeAll() {
              Actions = new List<ParseAction>() {
                new ConsumeString() {
                  IsOptional = true,
                  String = "<"
                },
                new ConsumeEntity() {
                  Reference = "identifier"
                },
                new ConsumeString() {
                  IsOptional = true,
                  String = ">"
                },
                new ConsumeAny() {
                  Actions = new List<ParseAction>() {
                    new ConsumeString() {
                      String = "::="
                    },
                    new ConsumeString() {
                      String = "="
                    }
                  }
                },
                new ConsumeEntity() {
                  Reference = "expression"
                },
                new ConsumeAny() {
                  Actions = new List<ParseAction>() {
                    new ConsumeString() {
                      String = ";"
                    },
                    new ConsumeString() {
                      String = "."
                    }
                  }
                }
              }
            },
            Supers = new HashSet<string>(),
            Subs = new HashSet<string>()
          },
          // virtual
          new Entity() {
            Rule = new Rule() {
              Identifier = "expression",
              Expression = new AlternativesExpression() {
                NonAlternativesExpression = new IdentifierExpression() {
                  Name = null,
                  Identifier = "alternatives-expression"
                },
                Expression = new IdentifierExpression() {
                  Name = null,
                  Identifier = "non-alternatives-expression"
                }
              }
            },
            Name = "expression",
            Properties = new List<Property>() {
              new Property() {
                Name = "alternative",
                Source = new ConsumeAny() {
                  Actions = new List<ParseAction>() {
                    new ConsumeEntity() {
                      Reference = "alternatives-expression"
                    },
                    new ConsumeEntity() {
                      Reference = "non-alternatives-expression"
                    }
                  }
                }
              }
            } .AsReadOnly(),
            ParseAction = new ConsumeAny() {
              Actions = new List<ParseAction>() {
                new ConsumeEntity() {
                  Reference = "alternatives-expression"
                },
                new ConsumeEntity() {
                  Reference = "non-alternatives-expression"
                }
              }
            },
            Supers = new HashSet<string>(),
            Subs = new HashSet<string>() {
              "alternatives-expression", "non-alternatives-expression"
            }
          },
          new Entity() {
            Rule = new Rule() {
              Identifier = "alternatives-expression",
              Expression = new SequentialExpression() {
                AtomicExpression = new IdentifierExpression() {
                  Name = null,
                  Identifier = "non-alternatives-expression"
                },
                NonAlternativesExpression = new SequentialExpression() {
                  AtomicExpression = new StringExpression() {
                    Name = null,
                    String = "|"
                  },
                  NonAlternativesExpression = new IdentifierExpression() {
                    Name = null,
                    Identifier = "expression"
                  }
                }
              }
            },
            Name = "alternatives-expression",
            Properties = new List<Property>() {
              new Property() {
                Name = "non-alternatives-expression",
                Source = new ConsumeEntity() {
                  Reference = "non-alternatives-expression"
                }
              },
              new Property() {
                Name = "expression",
                Source = new ConsumeEntity() {
                  Reference = "expression"
                }
              }
            } .AsReadOnly(),
            ParseAction = new ConsumeAll() {
              Actions = new List<ParseAction>() {
                new ConsumeEntity() {
                  Reference = "non-alternatives-expression"
                },
                new ConsumeString() {
                  String = "|"
                },
                new ConsumeEntity() {
                  Reference = "expression"
                }
              }
            },
            Supers = new HashSet<string>() {
              "expression"
            },
            Subs = new HashSet<string>()
          },
          // virtual
          new Entity() {
            Rule = new Rule() {
              Identifier = "non-alternatives-expression",
              Expression = new AlternativesExpression() {
                NonAlternativesExpression = new IdentifierExpression() {
                  Name = null,
                  Identifier = "sequential-expression"
                },
                Expression = new IdentifierExpression() {
                  Name = null,
                  Identifier = "atomic-expression"
                }
              }
            },
            Name = "non-alternatives-expression",
            Properties = new List<Property>() {
              new Property() {
                Name = "alternative",
                Source = new ConsumeAny() {
                  Actions = new List<ParseAction>() {
                    new ConsumeEntity() {
                      Reference = "sequential-expression"
                    },
                    new ConsumeEntity() {
                      Reference = "atomic-expression"
                    }
                  }
                }
              }
            } .AsReadOnly(),
            ParseAction = new ConsumeAny() {
              Actions = new List<ParseAction>() {
                new ConsumeEntity() {
                  Reference = "sequential-expression"
                },
                new ConsumeEntity() {
                  Reference = "atomic-expression"
                }
              }
            },
            Supers = new HashSet<string>() {
              "expression"
            },
            Subs = new HashSet<string>() {
              "sequential-expression", "atomic-expression"
            }
          },
          new Entity() {
            Rule = new Rule() {
              Identifier = "sequential-expression",
              Expression = new SequentialExpression() {
                AtomicExpression = new IdentifierExpression() {
                  Name = null,
                  Identifier = "atomic-expression"
                },
                NonAlternativesExpression = new SequentialExpression() {
                  AtomicExpression = new OptionalExpression() {
                    Expression = new StringExpression() {
                      Name = "_",
                      String = ","
                    }
                  },
                  NonAlternativesExpression = new IdentifierExpression() {
                    Name = null,
                    Identifier = "non-alternatives-expression"
                  }
                }
              }
            },
            Name = "sequential-expression",
            Properties = new List<Property>() {
              new Property() {
                Name = "atomic-expression",
                Source = new ConsumeEntity() {
                  Reference = "atomic-expression"
                }
              },
              new Property() {
                Name = "non-alternatives-expression",
                Source = new ConsumeEntity() {
                  Reference = "non-alternatives-expression"
                }
              }
            } .AsReadOnly(),
            ParseAction = new ConsumeAll() {
              Actions = new List<ParseAction>() {
                new ConsumeEntity() {
                  Reference = "atomic-expression"
                },
                new ConsumeString() {
                  IsOptional = true,
                  String = ","
                },
                new ConsumeEntity() {
                  Reference = "non-alternatives-expression"
                }
              }
            },
            Supers = new HashSet<string>() {
              "non-alternatives-expression"
            },
            Subs = new HashSet<string>()
          },
          // virtual
          new Entity() {
            Rule = new Rule() {
              Identifier = "atomic-expression",
              Expression = new AlternativesExpression() {
                NonAlternativesExpression = new IdentifierExpression() {
                  Name = null,
                  Identifier = "nested-expression"
                },
                Expression = new IdentifierExpression() {
                  Name = null,
                  Identifier = "terminal-expression"
                }
              }
            },
            Name = "atomic-expression",
            Properties = new List<Property>() {
              new Property() {
                Name = "alternative",
                Source = new ConsumeAny() {
                  Actions = new List<ParseAction>() {
                    new ConsumeEntity() {
                      Reference = "nested-expression"
                    },
                    new ConsumeEntity() {
                      Reference = "terminal-expression"
                    }
                  }
                }
              }
            } .AsReadOnly(),
            ParseAction = new ConsumeAny() {
              Actions = new List<ParseAction>() {
                new ConsumeEntity() {
                  Reference = "nested-expression"
                },
                new ConsumeEntity() {
                  Reference = "terminal-expression"
                }
              }
            },
            Supers = new HashSet<string>() {
              "non-alternatives-expression"
            },
            Subs = new HashSet<string>() {
              "nested-expression", "terminal-expression"
            }
          },
          // virtual
          new Entity() {
            Rule = new Rule() {
              Identifier = "nested-expression",
              Expression = new AlternativesExpression() {
                NonAlternativesExpression = new IdentifierExpression() {
                  Name = null,
                  Identifier = "optional-expression"
                },
                Expression = new AlternativesExpression() {
                  NonAlternativesExpression = new IdentifierExpression() {
                    Name = null,
                    Identifier = "repetition-expression"
                  },
                  Expression = new IdentifierExpression() {
                    Name = null,
                    Identifier = "group-expression"
                  }
                }
              }
            },
            Name = "nested-expression",
            Properties = new List<Property>() {
              new Property() {
                Name = "alternative",
                Source = new ConsumeAny() {
                  Actions = new List<ParseAction>() {
                    new ConsumeEntity() {
                      Reference = "optional-expression"
                    },
                    new ConsumeEntity() {
                      Reference = "repetition-expression"
                    },
                    new ConsumeEntity() {
                      Reference = "group-expression"
                    }
                  }
                }
              }
            } .AsReadOnly(),
            ParseAction = new ConsumeAny() {
              Actions = new List<ParseAction>() {
                new ConsumeEntity() {
                  Reference = "optional-expression"
                },
                new ConsumeEntity() {
                  Reference = "repetition-expression"
                },
                new ConsumeEntity() {
                  Reference = "group-expression"
                }
              }
            },
            Supers = new HashSet<string>() {
              "atomic-expression"
            },
            Subs = new HashSet<string>() {
              "optional-expression", "repetition-expression", "group-expression"
            }
          },
          new Entity() {
            Rule = new Rule() {
              Identifier = "optional-expression",
              Expression = new SequentialExpression() {
                AtomicExpression = new StringExpression() {
                  Name = null,
                  String = "["
                },
                NonAlternativesExpression = new SequentialExpression() {
                  AtomicExpression = new IdentifierExpression() {
                    Name = null,
                    Identifier = "expression"
                  },
                  NonAlternativesExpression = new StringExpression() {
                    Name = null,
                    String = "]"
                  }
                }
              }
            },
            Name = "optional-expression",
            Properties = new List<Property>() {
              new Property() {
                Name = "expression",
                Source = new ConsumeEntity() {
                  Reference = "expression"
                }
              }
            } .AsReadOnly(),
            ParseAction = new ConsumeAll() {
              Actions = new List<ParseAction>() {
                new ConsumeString() {
                  String = "["
                },
                new ConsumeEntity() {
                  Reference = "expression"
                },
                new ConsumeString() {
                  String = "]"
                }
              }
            },
            Supers = new HashSet<string>() {
              "nested-expression"
            },
            Subs = new HashSet<string>()
          },
          new Entity() {
            Rule = new Rule() {
              Identifier = "repetition-expression",
              Expression = new SequentialExpression() {
                AtomicExpression = new StringExpression() {
                  Name = null,
                  String = "{"
                },
                NonAlternativesExpression = new SequentialExpression() {
                  AtomicExpression = new IdentifierExpression() {
                    Name = null,
                    Identifier = "expression"
                  },
                  NonAlternativesExpression = new StringExpression() {
                    Name = null,
                    String = "}"
                  }
                }
              }
            },
            Name = "repetition-expression",
            Properties = new List<Property>() {
              new Property() {
                Name = "expression",
                Source = new ConsumeEntity() {
                  Reference = "expression"
                }
              }
            } .AsReadOnly(),
            ParseAction = new ConsumeAll() {
              Actions = new List<ParseAction>() {
                new ConsumeString() {
                  String = "{"
                },
                new ConsumeEntity() {
                  Reference = "expression"
                },
                new ConsumeString() {
                  String = "}"
                }
              }
            },
            Supers = new HashSet<string>() {
              "nested-expression"
            },
            Subs = new HashSet<string>()
          },
          new Entity() {
            Rule = new Rule() {
              Identifier = "group-expression",
              Expression = new SequentialExpression() {
                AtomicExpression = new StringExpression() {
                  Name = null,
                  String = "("
                },
                NonAlternativesExpression = new SequentialExpression() {
                  AtomicExpression = new IdentifierExpression() {
                    Name = null,
                    Identifier = "expression"
                  },
                  NonAlternativesExpression = new StringExpression() {
                    Name = null,
                    String = ")"
                  }
                }
              }
            },
            Name = "group-expression",
            Properties = new List<Property>() {
              new Property() {
                Name = "expression",
                Source = new ConsumeEntity() {
                  Reference = "expression"
                }
              }
            } .AsReadOnly(),
            ParseAction = new ConsumeAll() {
              Actions = new List<ParseAction>() {
                new ConsumeString() {
                  String = "("
                },
                new ConsumeEntity() {
                  Reference = "expression"
                },
                new ConsumeString() {
                  String = ")"
                }
              }
            },
            Supers = new HashSet<string>() {
              "nested-expression"
            },
            Subs = new HashSet<string>()
          },
          // virtual
          new Entity() {
            Rule = new Rule() {
              Identifier = "terminal-expression",
              Expression = new AlternativesExpression() {
                NonAlternativesExpression = new IdentifierExpression() {
                  Name = null,
                  Identifier = "identifier-expression"
                },
                Expression = new AlternativesExpression() {
                  NonAlternativesExpression = new IdentifierExpression() {
                    Name = null,
                    Identifier = "string-expression"
                  },
                  Expression = new IdentifierExpression() {
                    Name = null,
                    Identifier = "extractor-expression"
                  }
                }
              }
            },
            Name = "terminal-expression",
            Properties = new List<Property>() {
              new Property() {
                Name = "alternative",
                Source = new ConsumeAny() {
                  Actions = new List<ParseAction>() {
                    new ConsumeEntity() {
                      Reference = "identifier-expression"
                    },
                    new ConsumeEntity() {
                      Reference = "string-expression"
                    },
                    new ConsumeEntity() {
                      Reference = "extractor-expression"
                    }
                  }
                }
              }
            } .AsReadOnly(),
            ParseAction = new ConsumeAny() {
              Actions = new List<ParseAction>() {
                new ConsumeEntity() {
                  Reference = "identifier-expression"
                },
                new ConsumeEntity() {
                  Reference = "string-expression"
                },
                new ConsumeEntity() {
                  Reference = "extractor-expression"
                }
              }
            },
            Supers = new HashSet<string>() {
              "atomic-expression"
            },
            Subs = new HashSet<string>() {
              "identifier-expression", "string-expression", "extractor-expression"
            }
          },
          new Entity() {
            Rule = new Rule() {
              Identifier = "identifier-expression",
              Expression = new SequentialExpression() {
                AtomicExpression = new OptionalExpression() {
                  Expression = new IdentifierExpression() {
                    Name = null,
                    Identifier = "name"
                  }
                },
                NonAlternativesExpression = new SequentialExpression() {
                  AtomicExpression = new OptionalExpression() {
                    Expression = new StringExpression() {
                      Name = "_",
                      String = "<"
                    }
                  },
                  NonAlternativesExpression = new SequentialExpression() {
                    AtomicExpression = new IdentifierExpression() {
                      Name = null,
                      Identifier = "identifier"
                    },
                    NonAlternativesExpression = new OptionalExpression() {
                      Expression = new StringExpression() {
                        Name = "_",
                        String = ">"
                      }
                    }
                  }
                }
              }
            },
            Name = "identifier-expression",
            Properties = new List<Property>() {
              new Property() {
                Name = "name",
                Source = new ConsumeEntity() {
                  IsOptional = true,
                  Reference = "name"
                }
              },
              new Property() {
                Name = "identifier",
                Source = new ConsumeEntity() {
                  Reference = "identifier"
                }
              }
            } .AsReadOnly(),
            ParseAction = new ConsumeAll() {
              Actions = new List<ParseAction>() {
                new ConsumeEntity() {
                  IsOptional = true,
                  Reference = "name"
                },
                new ConsumeString() {
                  IsOptional = true,
                  String = "<"
                },
                new ConsumeEntity() {
                  Reference = "identifier"
                },
                new ConsumeString() {
                  IsOptional = true,
                  String = ">"
                }
              }
            },
            Supers = new HashSet<string>() {
              "terminal-expression"
            },
            Subs = new HashSet<string>()
          },
          new Entity() {
            Rule = new Rule() {
              Identifier = "string-expression",
              Expression = new SequentialExpression() {
                AtomicExpression = new OptionalExpression() {
                  Expression = new IdentifierExpression() {
                    Name = null,
                    Identifier = "name"
                  }
                },
                NonAlternativesExpression = new IdentifierExpression() {
                  Name = null,
                  Identifier = "string"
                }
              }
            },
            Name = "string-expression",
            Properties = new List<Property>() {
              new Property() {
                Name = "name",
                Source = new ConsumeEntity() {
                  IsOptional = true,
                  Reference = "name"
                }
              },
              new Property() {
                Name = "string",
                Source = new ConsumeEntity() {
                  Reference = "string"
                }
              }
            } .AsReadOnly(),
            ParseAction = new ConsumeAll() {
              Actions = new List<ParseAction>() {
                new ConsumeEntity() {
                  IsOptional = true,
                  Reference = "name"
                },
                new ConsumeEntity() {
                  Reference = "string"
                }
              }
            },
            Supers = new HashSet<string>() {
              "terminal-expression"
            },
            Subs = new HashSet<string>()
          },
          new Entity() {
            Rule = new Rule() {
              Identifier = "extractor-expression",
              Expression = new SequentialExpression() {
                AtomicExpression = new OptionalExpression() {
                  Expression = new IdentifierExpression() {
                    Name = null,
                    Identifier = "name"
                  }
                },
                NonAlternativesExpression = new SequentialExpression() {
                  AtomicExpression = new StringExpression() {
                    Name = null,
                    String = "?"
                  },
                  NonAlternativesExpression = new SequentialExpression() {
                    AtomicExpression = new StringExpression() {
                      Name = null,
                      String = "/"
                    },
                    NonAlternativesExpression = new SequentialExpression() {
                      AtomicExpression = new IdentifierExpression() {
                        Name = null,
                        Identifier = "pattern"
                      },
                      NonAlternativesExpression = new SequentialExpression() {
                        AtomicExpression = new StringExpression() {
                          Name = null,
                          String = "/"
                        },
                        NonAlternativesExpression = new StringExpression() {
                          Name = null,
                          String = "?"
                        }
                      }
                    }
                  }
                }
              }
            },
            Name = "extractor-expression",
            Properties = new List<Property>() {
              new Property() {
                Name = "name",
                Source = new ConsumeEntity() {
                  IsOptional = true,
                  Reference = "name"
                }
              },
              new Property() {
                Name = "pattern",
                Source = new ConsumeEntity() {
                  Reference = "pattern"
                }
              }
            } .AsReadOnly(),
            ParseAction = new ConsumeAll() {
              Actions = new List<ParseAction>() {
                new ConsumeEntity() {
                  IsOptional = true,
                  Reference = "name"
                },
                new ConsumeString() {
                  String = "?"
                },
                new ConsumeString() {
                  String = "/"
                },
                new ConsumeEntity() {
                  Reference = "pattern"
                },
                new ConsumeString() {
                  String = "/"
                },
                new ConsumeString() {
                  String = "?"
                }
              }
            },
            Supers = new HashSet<string>() {
              "terminal-expression"
            },
            Subs = new HashSet<string>()
          },
          // virtual
          new Entity() {
            Rule = new Rule() {
              Identifier = "name",
              Expression = new SequentialExpression() {
                AtomicExpression = new IdentifierExpression() {
                  Name = null,
                  Identifier = "identifier"
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
                Name = "identifier",
                Source = new ConsumeEntity() {
                  Reference = "identifier"
                }
              }
            } .AsReadOnly(),
            ParseAction = new ConsumeAll() {
              Actions = new List<ParseAction>() {
                new ConsumeEntity() {
                  Reference = "identifier"
                },
                new ConsumeString() {
                  String = "@"
                }
              }
            },
            Supers = new HashSet<string>(),
            Subs = new HashSet<string>() { "identifier" }
          },
          // virtual
          new Entity() {
            Rule = new Rule() {
              Identifier = "identifier",
              Expression = new ExtractorExpression() {
                Name = null,
                Pattern = "([A-Za-z_][A-Za-z0-9-_]*)"
              }
            },
            Name = "identifier",
            Properties = new List<Property>() {
              new Property() {
                Name = "identifier",
                Source = new ConsumePattern() {
                  Pattern = "([A-Za-z_][A-Za-z0-9-_]*)"
                }
              }
            } .AsReadOnly(),
            ParseAction = new ConsumePattern() {
              Pattern = "([A-Za-z_][A-Za-z0-9-_]*)"
            },
            Supers = new HashSet<string>() {
              "name"
            },
            Subs = new HashSet<string>()
          },
          // virtual
          new Entity() {
            Rule = new Rule() {
              Identifier = "string",
              Expression = new ExtractorExpression() {
                Name = null,
                Pattern = "\"([^\"]*)\"|^'([^']*)'"
              }
            },
            Name = "string",
            Properties = new List<Property>() {
              new Property() {
                Name = "string",
                Source = new ConsumePattern() {
                  Pattern = "\"([^\"]*)\"|^'([^']*)'"
                }
              }
            } .AsReadOnly(),
            ParseAction = new ConsumePattern() {
              Pattern = "\"([^\"]*)\"|^'([^']*)'"
            },
            Supers = new HashSet<string>(),
            Subs = new HashSet<string>()
          },
          // virtual
          new Entity() {
            Rule = new Rule() {
              Identifier = "pattern",
              Expression = new ExtractorExpression() {
                Name = null,
                Pattern = "(.*?)(?<keep>/\\s*\\?)"
              }
            },
            Name = "pattern",
            Properties = new List<Property>() {
              new Property() {
                Name = "pattern",
                Source = new ConsumePattern() {
                  Pattern = "(.*?)(?<keep>/\\s*\\?)"
                }
              }
            } .AsReadOnly(),
            ParseAction = new ConsumePattern() {
              Pattern = "(.*?)(?<keep>/\\s*\\?)"
            },
            Supers = new HashSet<string>(),
            Subs = new HashSet<string>()
          },
          // virtual
          new Entity() {
            Rule = new Rule() {
              Identifier = "_",
              Expression = new ExtractorExpression() {
                Name = null,
                Pattern = "\\(\\*.*?\\*\\)"
              }
            },
            Name = "_",
            Properties = new List<Property>() {
              new Property() {
                Name = "_",
                Source = new ConsumePattern() {
                  Pattern = "\\(\\*.*?\\*\\)"
                }
              }
            } .AsReadOnly(),
            ParseAction = new ConsumePattern() {
              Pattern = "\\(\\*.*?\\*\\)"
            },
            Supers = new HashSet<string>(),
            Subs = new HashSet<string>()
          }
        },
        RootName = "grammar"
      }
    );
  }
}
