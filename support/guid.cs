using System;
using System.IO;

public class Runner {
  public static void Main(string[] args) {
    Console.WriteLine(System.Guid.NewGuid().ToString("B").ToUpper());
  }
}
