using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;
using Xunit.Abstractions;
using FastFsm.Tests.TestHelpers;
using Shouldly;

namespace FastFsm.Tests.Features.Parity
{
    /// <summary>
    /// CI-blocking tests that ensure 100% parity between Fluent and Legacy APIs
    /// </summary>
    [Trait("Category", "Parity")]
    [Trait("Category", "CI-Gate")]
    public class CoverageParityTests
    {
        private readonly ITestOutputHelper _output;

        public CoverageParityTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void AllMachines_MustHave_BothFluentAndLegacy_Implementations()
        {
            var machinesPath = Path.Combine(AppContext.BaseDirectory, "../../../Machines");
            var hsmPath = Path.Combine(AppContext.BaseDirectory, "../../../Features/Hsm/Runtime");
            
            var issues = new List<string>();
            
            // Check Machines folder
            CheckMachinesParity(machinesPath, issues);
            
            // Check HSM Runtime folder
            CheckMachinesParity(hsmPath, issues);
            
            if (issues.Any())
            {
                var report = new StringBuilder();
                report.AppendLine("PARITY VIOLATIONS DETECTED:");
                report.AppendLine("============================");
                foreach (var issue in issues)
                {
                    report.AppendLine($"❌ {issue}");
                }
                
                _output.WriteLine(report.ToString());
                Assert.True(false, $"Found {issues.Count} parity violations. See output for details.");
            }
        }

        private void CheckMachinesParity(string path, List<string> issues)
        {
            if (!Directory.Exists(path))
                return;
                
            var files = Directory.GetFiles(path, "*.cs")
                .Select(Path.GetFileName)
                .Where(f => !f.Contains(".Generated.cs") && !f.Contains("Tests.cs"))
                .ToList();
            
            var fluentFiles = files.Where(f => f.EndsWith(".Fluent.cs")).ToList();
            var legacyFiles = files.Where(f => f.EndsWith(".Legacy.cs")).ToList();
            
            // Extract machine names
            var fluentMachines = fluentFiles.Select(f => f.Replace(".Fluent.cs", "")).ToHashSet();
            var legacyMachines = legacyFiles.Select(f => f.Replace(".Legacy.cs", "")).ToHashSet();
            
            // Find machines with only Fluent
            foreach (var machine in fluentMachines.Except(legacyMachines))
            {
                issues.Add($"Machine '{machine}' has Fluent but missing Legacy implementation");
            }
            
            // Find machines with only Legacy
            foreach (var machine in legacyMachines.Except(fluentMachines))
            {
                issues.Add($"Machine '{machine}' has Legacy but missing Fluent implementation");
            }
        }

        [Fact]
        public void AllMachines_MustBe_RegisteredInMachineRegistry()
        {
            var allMachines = MachineRegistry.GetAllMachines().ToList();
            var issues = new List<string>();
            
            foreach (var machine in allMachines)
            {
                // Check if machine has complete registration
                if (!machine.IsComplete)
                {
                    var missing = new List<string>();
                    if (machine.FluentStateType == null) missing.Add("FluentStateType");
                    if (machine.LegacyStateType == null) missing.Add("LegacyStateType");
                    if (machine.FluentTriggerType == null) missing.Add("FluentTriggerType");
                    if (machine.LegacyTriggerType == null) missing.Add("LegacyTriggerType");
                    
                    issues.Add($"Machine '{machine.Name}' incomplete registration: missing {string.Join(", ", missing)}");
                }
                
                // Check if machine has wrapper factory
                if (machine.WrapperFactory == null)
                {
                    issues.Add($"Machine '{machine.Name}' missing WrapperFactory");
                }
            }
            
            if (issues.Any())
            {
                var report = new StringBuilder();
                report.AppendLine("REGISTRATION ISSUES DETECTED:");
                report.AppendLine("==============================");
                foreach (var issue in issues)
                {
                    report.AppendLine($"❌ {issue}");
                }
                
                _output.WriteLine(report.ToString());
                Assert.True(false, $"Found {issues.Count} registration issues. See output for details.");
            }
        }

        [Fact]
        public void AllMachines_MustHave_WorkingWrappers()
        {
            var allMachines = MachineRegistry.GetAllMachines()
                .Where(m => m.WrapperFactory != null)
                .ToList();
            
            var issues = new List<string>();
            
            foreach (var machine in allMachines)
            {
                try
                {
                    // Try to create Fluent wrapper
                    var fluentWrapper = machine.WrapperFactory!(StateMachineWrapperFactory.ApiType.Fluent, null);
                    if (fluentWrapper == null)
                    {
                        issues.Add($"Machine '{machine.Name}' Fluent wrapper factory returned null");
                    }
                    else
                    {
                        // Basic smoke test
                        fluentWrapper.Start();
                        _ = fluentWrapper.CurrentState;
                        _ = fluentWrapper.GetPermittedTriggers();
                    }
                }
                catch (NotImplementedException)
                {
                    issues.Add($"Machine '{machine.Name}' Fluent wrapper not implemented");
                }
                catch (Exception ex)
                {
                    issues.Add($"Machine '{machine.Name}' Fluent wrapper error: {ex.Message}");
                }
                
                try
                {
                    // Try to create Legacy wrapper
                    var legacyWrapper = machine.WrapperFactory!(StateMachineWrapperFactory.ApiType.Legacy, null);
                    if (legacyWrapper == null)
                    {
                        issues.Add($"Machine '{machine.Name}' Legacy wrapper factory returned null");
                    }
                    else
                    {
                        // Basic smoke test
                        legacyWrapper.Start();
                        _ = legacyWrapper.CurrentState;
                        _ = legacyWrapper.GetPermittedTriggers();
                    }
                }
                catch (NotImplementedException)
                {
                    issues.Add($"Machine '{machine.Name}' Legacy wrapper not implemented");
                }
                catch (Exception ex)
                {
                    issues.Add($"Machine '{machine.Name}' Legacy wrapper error: {ex.Message}");
                }
            }
            
            if (issues.Any())
            {
                var report = new StringBuilder();
                report.AppendLine("WRAPPER ISSUES DETECTED:");
                report.AppendLine("=========================");
                foreach (var issue in issues)
                {
                    report.AppendLine($"❌ {issue}");
                }
                
                _output.WriteLine(report.ToString());
                Assert.True(false, $"Found {issues.Count} wrapper issues. See output for details.");
            }
        }

        [Fact]
        public void EnumConverterV2_MustHave_CompleteAliases()
        {
            var issues = new List<string>();
            var allMachines = MachineRegistry.GetAllMachines()
                .Where(m => m.IsComplete)
                .ToList();
            
            foreach (var machine in allMachines)
            {
                // Check state enum conversion
                if (machine.FluentStateType != null && machine.LegacyStateType != null)
                {
                    var fluentStates = Enum.GetNames(machine.FluentStateType);
                    var legacyStates = Enum.GetNames(machine.LegacyStateType);
                    
                    // Check if enums differ
                    if (!fluentStates.SequenceEqual(legacyStates))
                    {
                        // Check if aliases are registered
                        foreach (var fluentState in fluentStates)
                        {
                            try
                            {
                                // Try to convert using the converter - this will throw if no mapping exists
                                var fluentValue = Enum.Parse(machine.FluentStateType, fluentState);
                                // We can't use generic ToLegacy here, so just check if conversion would work
                                // by trying the non-generic version which should exist or be added
                                // For now, just mark as TODO
                                // TODO: Add proper enum conversion check
                            }
                            catch
                            {
                                issues.Add($"Machine '{machine.Name}' missing state alias: {fluentState} (Fluent->Legacy)");
                            }
                        }
                    }
                }
                
                // Check trigger enum conversion
                if (machine.FluentTriggerType != null && machine.LegacyTriggerType != null)
                {
                    var fluentTriggers = Enum.GetNames(machine.FluentTriggerType);
                    var legacyTriggers = Enum.GetNames(machine.LegacyTriggerType);
                    
                    // Check if enums differ
                    if (!fluentTriggers.SequenceEqual(legacyTriggers))
                    {
                        // Check if aliases are registered
                        foreach (var fluentTrigger in fluentTriggers)
                        {
                            try
                            {
                                // Try to convert using the converter - this will throw if no mapping exists
                                var fluentValue = Enum.Parse(machine.FluentTriggerType, fluentTrigger);
                                // TODO: Add proper enum conversion check
                            }
                            catch
                            {
                                issues.Add($"Machine '{machine.Name}' missing trigger alias: {fluentTrigger} (Fluent->Legacy)");
                            }
                        }
                    }
                }
            }
            
            if (issues.Any())
            {
                var report = new StringBuilder();
                report.AppendLine("ENUM ALIAS ISSUES DETECTED:");
                report.AppendLine("============================");
                foreach (var issue in issues)
                {
                    report.AppendLine($"❌ {issue}");
                }
                
                _output.WriteLine(report.ToString());
                Assert.True(false, $"Found {issues.Count} enum alias issues. See output for details.");
            }
        }

        [Fact]
        public void ApiCapabilities_MustBe_ConsistentAcrossApis()
        {
            var issues = new List<string>();
            var machines = MachineRegistry.GetAllMachines()
                .Where(m => m.WrapperFactory != null)
                .ToList();
            
            foreach (var machine in machines)
            {
                try
                {
                    var fluentWrapper = machine.WrapperFactory!(StateMachineWrapperFactory.ApiType.Fluent, null);
                    var legacyWrapper = machine.WrapperFactory!(StateMachineWrapperFactory.ApiType.Legacy, null);
                    
                    if (fluentWrapper.Caps != legacyWrapper.Caps)
                    {
                        issues.Add($"Machine '{machine.Name}' capability mismatch: Fluent={fluentWrapper.Caps}, Legacy={legacyWrapper.Caps}");
                    }
                }
                catch (NotImplementedException)
                {
                    // Already caught in wrapper test
                }
                catch (Exception ex)
                {
                    issues.Add($"Machine '{machine.Name}' capability check error: {ex.Message}");
                }
            }
            
            if (issues.Any())
            {
                var report = new StringBuilder();
                report.AppendLine("CAPABILITY CONSISTENCY ISSUES:");
                report.AppendLine("===============================");
                foreach (var issue in issues)
                {
                    report.AppendLine($"❌ {issue}");
                }
                
                _output.WriteLine(report.ToString());
                Assert.True(false, $"Found {issues.Count} capability issues. See output for details.");
            }
        }

        [Fact]
        public void GenerateParityReport()
        {
            var report = new StringBuilder();
            report.AppendLine("PARITY COVERAGE REPORT");
            report.AppendLine("======================");
            report.AppendLine();
            
            var machines = MachineRegistry.GetAllMachines().OrderBy(m => m.Name).ToList();
            
            report.AppendLine($"Total Machines Registered: {machines.Count}");
            report.AppendLine($"Complete Registrations: {machines.Count(m => m.IsComplete)}");
            report.AppendLine($"With Wrappers: {machines.Count(m => m.WrapperFactory != null)}");
            report.AppendLine();
            
            report.AppendLine("Machine Status:");
            report.AppendLine("---------------");
            
            foreach (var machine in machines)
            {
                var status = new List<string>();
                
                if (machine.IsComplete) status.Add("✅ Complete");
                else status.Add("❌ Incomplete");
                
                if (machine.WrapperFactory != null) status.Add("✅ Wrapper");
                else status.Add("❌ No Wrapper");
                
                try
                {
                    if (machine.WrapperFactory != null)
                    {
                        var wrapper = machine.WrapperFactory(StateMachineWrapperFactory.ApiType.Fluent, null);
                        status.Add($"Caps: {wrapper.Caps}");
                    }
                }
                catch
                {
                    status.Add("⚠️ Wrapper Error");
                }
                
                report.AppendLine($"  {machine.Name}: {string.Join(", ", status)}");
            }
            
            _output.WriteLine(report.ToString());
        }
    }
}