using System.Collections.Generic;
using Generator.Model;

namespace FastFsm.HsmPrototype.TestModels;

public static class HistoryHierarchy
{
    public static StateMachineModel Create()
    {
        var model = new StateMachineModel
        {
            Namespace = "TestNamespace",
            ClassName = "HistoryHsmMachine",
            StateType = "MachineState",
            TriggerType = "MachineTrigger",
            HierarchyEnabled = true
        };
        
        // States hierarchy:
        // Off
        // On (has shallow history)
        //   ├── On_Idle
        //   └── On_Working (has deep history)
        //         ├── On_Working_Fast
        //         └── On_Working_Slow
        
        // Define states
        model.States["Off"] = new StateModel { Name = "Off" };
        
        model.States["On"] = new StateModel { 
            Name = "On",
            History = HistoryMode.Shallow,  // Remember last direct child
            OnEntryMethod = "OnOnEntry",
            OnExitMethod = "OnOnExit"
        };
        
        model.States["On_Idle"] = new StateModel { 
            Name = "On_Idle",
            ParentState = "On",
            IsInitial = true,
            OnEntryMethod = "OnIdleEntry"
        };
        
        model.States["On_Working"] = new StateModel { 
            Name = "On_Working",
            ParentState = "On",
            History = HistoryMode.Deep,  // Remember full substate path
            OnEntryMethod = "OnWorkingEntry",
            OnExitMethod = "OnWorkingExit"
        };
        
        model.States["On_Working_Fast"] = new StateModel { 
            Name = "On_Working_Fast",
            ParentState = "On_Working",
            IsInitial = true,
            OnEntryMethod = "OnFastEntry"
        };
        
        model.States["On_Working_Slow"] = new StateModel { 
            Name = "On_Working_Slow",
            ParentState = "On_Working",
            OnEntryMethod = "OnSlowEntry"
        };
        
        // Build hierarchy
        model.ParentOf["Off"] = null;
        model.ParentOf["On"] = null;
        model.ParentOf["On_Idle"] = "On";
        model.ParentOf["On_Working"] = "On";
        model.ParentOf["On_Working_Fast"] = "On_Working";
        model.ParentOf["On_Working_Slow"] = "On_Working";
        
        model.ChildrenOf["On"] = new List<string> { "On_Idle", "On_Working" };
        model.ChildrenOf["On_Working"] = new List<string> { "On_Working_Fast", "On_Working_Slow" };
        
        model.InitialChildOf["On"] = "On_Idle";
        model.InitialChildOf["On_Working"] = "On_Working_Fast";
        
        model.HistoryOf["On"] = HistoryMode.Shallow;
        model.HistoryOf["On_Working"] = HistoryMode.Deep;
        
        // Transitions
        model.Transitions.Add(new TransitionModel
        {
            FromState = "Off",
            Trigger = "PowerOn",
            ToState = "On"  // Should use history if available, else On_Idle
        });
        
        model.Transitions.Add(new TransitionModel
        {
            FromState = "On",
            Trigger = "PowerOff",
            ToState = "Off"
        });
        
        model.Transitions.Add(new TransitionModel
        {
            FromState = "On_Idle",
            Trigger = "StartWork",
            ToState = "On_Working"  // Should use deep history if available
        });
        
        model.Transitions.Add(new TransitionModel
        {
            FromState = "On_Working",
            Trigger = "Stop",
            ToState = "On_Idle"
        });
        
        model.Transitions.Add(new TransitionModel
        {
            FromState = "On_Working_Fast",
            Trigger = "Slow",
            ToState = "On_Working_Slow"
        });
        
        model.Transitions.Add(new TransitionModel
        {
            FromState = "On_Working_Slow",
            Trigger = "Fast",
            ToState = "On_Working_Fast"
        });
        
        return model;
    }
}