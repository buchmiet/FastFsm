using System.Collections.Generic;
using Generator.Model;

namespace FastFsm.HsmPrototype.TestModels;

public static class SimpleHierarchy
{
    public static StateMachineModel Create()
    {
        var model = new StateMachineModel
        {
            Namespace = "TestNamespace",
            ClassName = "SimpleHsmMachine",
            StateType = "ProcessState",
            TriggerType = "ProcessTrigger",
            HierarchyEnabled = true
        };
        
        // Define states
        model.States["Pending"] = new StateModel { Name = "Pending" };
        model.States["Work"] = new StateModel { 
            Name = "Work",
            OnEntryMethod = "OnWorkEntry",
            OnExitMethod = "OnWorkExit"
        };
        model.States["Work_Idle"] = new StateModel { 
            Name = "Work_Idle",
            ParentState = "Work",
            IsInitial = true,
            OnEntryMethod = "OnIdleEntry",
            OnExitMethod = "OnIdleExit"
        };
        model.States["Work_Active"] = new StateModel { 
            Name = "Work_Active",
            ParentState = "Work",
            OnEntryMethod = "OnActiveEntry",
            OnExitMethod = "OnActiveExit"
        };
        model.States["Done"] = new StateModel { Name = "Done" };
        
        // Build hierarchy relationships
        model.ParentOf["Pending"] = null;
        model.ParentOf["Work"] = null;
        model.ParentOf["Work_Idle"] = "Work";
        model.ParentOf["Work_Active"] = "Work";
        model.ParentOf["Done"] = null;
        
        model.ChildrenOf["Work"] = new List<string> { "Work_Idle", "Work_Active" };
        model.InitialChildOf["Work"] = "Work_Idle";
        
        // Work is automatically composite (has children)
        
        // Define transitions
        model.Transitions.Add(new TransitionModel
        {
            FromState = "Pending",
            Trigger = "Start",
            ToState = "Work"  // Should go to Work_Idle automatically
        });
        
        model.Transitions.Add(new TransitionModel
        {
            FromState = "Work_Idle",
            Trigger = "Activate",
            ToState = "Work_Active"
        });
        
        model.Transitions.Add(new TransitionModel
        {
            FromState = "Work_Active",
            Trigger = "Finish",
            ToState = "Done",
            Priority = 100
        });
        
        // Add transition between children of the same parent
        model.Transitions.Add(new TransitionModel
        {
            FromState = "Work_Active",
            Trigger = "Pause",
            ToState = "Work_Idle"  // Transition between Work's children
        });
        
        // Internal transition - tick w Work_Active
        model.Transitions.Add(new TransitionModel
        {
            FromState = "Work_Active",
            Trigger = "Tick",
            ToState = "Work_Active",  // Same state = internal
            IsInternal = true,
            ActionMethod = "UpdateProgress",
            Priority = 150  // High priority
        });
        
        // Transition z Work_Active z wysokim priorytetem
        model.Transitions.Add(new TransitionModel
        {
            FromState = "Work_Active",
            Trigger = "Emergency",
            ToState = "Done",
            Priority = 200,  // Highest priority
            GuardMethod = "IsEmergency"
        });
        
        // Transition z Work (parent) z niskim priorytetem - fallback
        model.Transitions.Add(new TransitionModel
        {
            FromState = "Work",
            Trigger = "Abort",
            ToState = "Done",
            Priority = 50,  // Low priority
            ActionMethod = "CleanupWork"
        });
        
        // Konflikt priorytetów - dwa transitions z tego samego stanu
        model.Transitions.Add(new TransitionModel
        {
            FromState = "Work_Active",
            Trigger = "Finish",
            ToState = "Work_Idle",
            Priority = 80  // Lower than existing Finish->Done (100)
        });
        
        return model;
    }
}