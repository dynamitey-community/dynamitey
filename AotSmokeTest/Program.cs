using System;
using System.Collections.Generic;
using System.Linq;
using Dynamitey;

namespace Dynamitey.AotSmokeTest
{
    public class Simple
    {
        public string Name { get; set; } = "hello";
    }

    public class ParamsPoco
    {
        public string Args;

        public ParamsPoco(params string[] args)
        {
            Args = string.Join(",", args);
        }
    }

    /// <summary>
    /// Asserts how Dynamitey behaves when NativeAOT-compiled. It is a console
    /// application rather than an NUnit fixture because the thing under test is the
    /// published native binary: a test host would not be AOT-compiled, and running
    /// under one would prove nothing.
    ///
    /// Exit code 0 means every case behaved as documented. Non-zero means the
    /// documented behaviour changed, which makes the README and the annotation
    /// messages wrong.
    ///
    /// This library can never be trim-safe or AOT-safe; it is built on the DLR.
    /// These assertions do not claim it works. They pin *how it fails*, because the
    /// annotations added for issue #4 and the README both describe specific failures,
    /// and documentation that quietly stops being true is worse than none.
    /// </summary>
    internal static class Program
    {
        private static readonly List<string> Failures = new List<string>();

        private static int Main()
        {
            Console.WriteLine("Dynamitey NativeAOT smoke test");
            Console.WriteLine("------------------------------");

            // The DLR cannot resolve members in an AOT image. The failure is a
            // RuntimeBinderException claiming the member is absent - misleading, but
            // documented in the README precisely because it is misleading.
            ExpectRuntimeBinder(
                "InvokeGet on a public property",
                () => Dynamic.InvokeGet(new Simple(), "Name"));

            // Same failure for a constructor within the generated 0-14 argument range.
            // This is the case that proves the problem is not confined to the >14
            // argument emit path.
            ExpectRuntimeBinder(
                "InvokeConstructor, 5 args (generated 0-14 path)",
                () => Dynamic.InvokeConstructor(
                    typeof(ParamsPoco),
                    Enumerable.Range(0, 5).Select(it => it.ToString() as object).ToArray()));

            // Above 14 arguments Dynamitey emits the call site delegate itself
            // (issue #27), which needs Reflection.Emit. AOT has none, and the message
            // must stay actionable - it is the only one of the three that tells the
            // caller what to do.
            ExpectPlatformNotSupported(
                "InvokeConstructor, 20 args (Reflection.Emit path)",
                () => Dynamic.InvokeConstructor(
                    typeof(ParamsPoco),
                    Enumerable.Range(0, 20).Select(it => it.ToString() as object).ToArray()),
                expectedFragments: new[] { "14", "Reflection.Emit" });

            Console.WriteLine();

            if (Failures.Count == 0)
            {
                Console.WriteLine("All cases behaved as documented.");
                return 0;
            }

            Console.WriteLine($"{Failures.Count} case(s) did NOT behave as documented:");
            foreach (var tFailure in Failures)
            {
                Console.WriteLine($"  - {tFailure}");
            }

            return 1;
        }

        private static void ExpectRuntimeBinder(string label, Func<object> action)
        {
            try
            {
                var tResult = action();
                Report(label, false, $"expected RuntimeBinderException, but it succeeded: {tResult}");
            }
            catch (Exception ex) when (ex.GetType().Name == "RuntimeBinderException")
            {
                Report(label, true, $"RuntimeBinderException: {FirstLine(ex.Message)}");
            }
            catch (Exception ex)
            {
                Report(label, false, $"expected RuntimeBinderException, got {ex.GetType().Name}: {FirstLine(ex.Message)}");
            }
        }

        private static void ExpectPlatformNotSupported(
            string label, Func<object> action, string[] expectedFragments)
        {
            try
            {
                var tResult = action();
                Report(label, false, $"expected PlatformNotSupportedException, but it succeeded: {tResult}");
            }
            catch (PlatformNotSupportedException ex)
            {
                var tMissing = expectedFragments.Where(it => ex.Message.IndexOf(it, StringComparison.Ordinal) < 0).ToList();

                if (tMissing.Count > 0)
                {
                    Report(label, false,
                        $"PlatformNotSupportedException message no longer mentions {string.Join(", ", tMissing)} - "
                        + "it must stay actionable. Message was: " + FirstLine(ex.Message));
                }
                else
                {
                    Report(label, true, $"PlatformNotSupportedException, actionable: {FirstLine(ex.Message)}");
                }
            }
            catch (Exception ex)
            {
                Report(label, false, $"expected PlatformNotSupportedException, got {ex.GetType().Name}: {FirstLine(ex.Message)}");
            }
        }

        private static void Report(string label, bool asDocumented, string detail)
        {
            Console.WriteLine($"[{(asDocumented ? "as documented" : "CHANGED")}] {label}");
            Console.WriteLine($"    {detail}");

            if (!asDocumented)
            {
                Failures.Add($"{label}: {detail}");
            }
        }

        private static string FirstLine(string value)
        {
            var tIndex = value.IndexOf('\n');
            return tIndex < 0 ? value : value.Substring(0, tIndex).TrimEnd('\r');
        }
    }
}
