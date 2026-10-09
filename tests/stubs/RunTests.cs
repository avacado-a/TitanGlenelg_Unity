using System;
using System.Reflection;
using System.Collections.Generic;

namespace TitanGlenelg.TestsRunner
{
    class Program
    {
        static int Main(string[] args)
        {
            Console.WriteLine("==================================================================");
            Console.WriteLine(" TITAN-GLENELG NATIVE C# TEST SUITE EXECUTION ");
            Console.WriteLine("==================================================================");

            Assembly testAssembly = Assembly.LoadFrom("tests/stubs/TitanGlenelg.Tests.dll");
            int total = 0;
            int passed = 0;
            int failed = 0;

            foreach (Type type in testAssembly.GetTypes())
            {
                if (type.GetCustomAttribute(typeof(NUnit.Framework.TestFixtureAttribute)) != null || type.Name.EndsWith("Tests"))
                {
                    Console.WriteLine($"\n--> Running Fixture: {type.Name}");
                    object instance = Activator.CreateInstance(type);
                    MethodInfo setUp = null;
                    MethodInfo tearDown = null;

                    foreach (var m in type.GetMethods())
                    {
                        if (m.GetCustomAttribute(typeof(NUnit.Framework.SetUpAttribute)) != null) setUp = m;
                        if (m.GetCustomAttribute(typeof(NUnit.Framework.TearDownAttribute)) != null) tearDown = m;
                    }

                    foreach (var m in type.GetMethods())
                    {
                        if (m.GetCustomAttribute(typeof(NUnit.Framework.TestAttribute)) != null)
                        {
                            total++;
                            try
                            {
                                setUp?.Invoke(instance, null);
                                m.Invoke(instance, null);
                                tearDown?.Invoke(instance, null);
                                Console.WriteLine($"  [PASS] {m.Name}");
                                passed++;
                            }
                            catch (Exception ex)
                            {
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine($"  [FAIL] {m.Name}: {ex.InnerException?.Message ?? ex.Message}");
                                Console.ResetColor();
                                failed++;
                            }
                        }
                    }
                }
            }

            Console.WriteLine("==================================================================");
            Console.WriteLine($" RESULTS: Total: {total} | Passed: {passed} | Failed: {failed}");
            Console.WriteLine("==================================================================");

            return failed == 0 ? 0 : 1;
        }
    }
}
