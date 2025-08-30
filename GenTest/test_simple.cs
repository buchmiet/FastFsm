using Abstractions.Attributes;
using System;

namespace TestFsm
{
    public enum LightState { Off, On, Blinking }
    public enum LightTrigger { TurnOn, TurnOff, StartBlink, StopBlink }

    [StateMachine(typeof(LightState), typeof(LightTrigger))]
    public partial class LightController
    {
        [Transition(LightState.Off, LightTrigger.TurnOn, LightState.On)]
        [Transition(LightState.On, LightTrigger.TurnOff, LightState.Off)]
        [Transition(LightState.On, LightTrigger.StartBlink, LightState.Blinking)]
        [Transition(LightState.Blinking, LightTrigger.StopBlink, LightState.On)]
        [Transition(LightState.Blinking, LightTrigger.TurnOff, LightState.Off)]
        private void ConfigureTransitions() { }

        [State(LightState.On, OnEntry = nameof(OnLightOn))]
        private void ConfigureOnState() { }
        
        private void OnLightOn() => Console.WriteLine("Light is ON!");
    }
}