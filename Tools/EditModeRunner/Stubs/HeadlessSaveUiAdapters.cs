// Opt-in adapters for pure save/menu controller tests; no device or Unity lifecycle coverage.
#if COO_HEADLESS_SAVE_UI
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
[assembly: HeadlessSaveUiLogCheck]
public sealed class HeadlessSaveUiLogCheckAttribute : Attribute, ITestAction
{
 public ActionTargets Targets => ActionTargets.Test;
 public void BeforeTest(ITest test) => UnityEngine.TestTools.LogAssert.Reset();
 public void AfterTest(ITest test) => UnityEngine.TestTools.LogAssert.Verify();
}
namespace UnityEngine { public enum LogType { Error, Assert, Warning, Log, Exception } }
namespace UnityEngine.TestTools
{
 public static class LogAssert
 {
  private static readonly Queue<(UnityEngine.LogType type, Regex regex)> expected = new Queue<(UnityEngine.LogType, Regex)>();
  private static readonly List<string> unexpected = new List<string>();
  public static void Expect(UnityEngine.LogType type, Regex regex) => expected.Enqueue((type, regex));
  public static void Observe(UnityEngine.LogType type, string message)
  {
   if (expected.Count > 0 && expected.Peek().type == type && expected.Peek().regex.IsMatch(message ?? "")) expected.Dequeue();
   else unexpected.Add(message);
  }
  public static void Reset() { expected.Clear(); unexpected.Clear(); }
  public static void Verify() { Assert.That(expected.Count, Is.Zero, "Expected log was not emitted"); Assert.That(unexpected, Is.Empty, "Unexpected error logs"); }
 }
}

namespace CavesOfOoo.Rendering { public static class InputHelper { public static bool GetKeyDown(UnityEngine.KeyCode key) => false; } }

#endif
