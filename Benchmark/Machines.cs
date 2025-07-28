//using Stateless;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using Abstractions.Attributes;

//namespace Benchmark
//{
//        // ===== Basic State Machine =====

//        // FastFSM
//        [StateMachine(typeof(State), typeof(Trigger))]
//        public partial class FastFsmBasic
//        {
//            [Transition(State.A, Trigger.Next, State.B)]
//            [Transition(State.B, Trigger.Next, State.C)]
//            [Transition(State.C, Trigger.Next, State.A)]
//            private void Configure() { }
//        }

       

//        // ===== With Guards and Actions =====

//        [StateMachine(typeof(State), typeof(Trigger))]
//        public partial class FastFsmWithGuardsActions
//        {
//            private int _counter;

//            [Transition(State.A, Trigger.Next, State.B,
//                Guard = nameof(CanTransition), Action = nameof(IncrementCounter))]
//            [Transition(State.B, Trigger.Next, State.C,
//                Guard = nameof(CanTransition), Action = nameof(IncrementCounter))]
//            [Transition(State.C, Trigger.Next, State.A,
//                Guard = nameof(CanTransition), Action = nameof(IncrementCounter))]
//            private void Configure() { }

//            private bool CanTransition() => _counter < 1000000;
//            private void IncrementCounter() => _counter++;
//        }

     
      

//        // ===== With Payload =====

//        public class PayloadData
//        {
//            public int Value { get; set; }
//            public string Message { get; set; } = "";
//        }

//        [StateMachine(typeof(State), typeof(Trigger))]
//        [PayloadType(typeof(PayloadData))]
//        public partial class FastFsmWithPayload
//        {
//            private int _sum;

//            [Transition(State.A, Trigger.Next, State.B, Action = nameof(ProcessPayload))]
//            [Transition(State.B, Trigger.Next, State.C, Action = nameof(ProcessPayload))]
//            [Transition(State.C, Trigger.Next, State.A, Action = nameof(ProcessPayload))]
//            private void Configure() { }

//            private void ProcessPayload(PayloadData data) => _sum += data.Value;
//        }




//        [StateMachine(typeof(State), typeof(Trigger))]
//        public partial class FastFsmAsyncActions
//        {
//            private int _asyncCounter;

//            [Transition(State.A, Trigger.Next, State.B, Action = nameof(ProcessAsync))]
//            [Transition(State.B, Trigger.Next, State.C, Action = nameof(ProcessAsync))]
//            [Transition(State.C, Trigger.Next, State.A, Action = nameof(ProcessAsync))]
//            private void Configure() { }

//            private async ValueTask ProcessAsync()
//            {
//                await Task.Yield();      // minimalny realny await (jak w Stateless)
//                _asyncCounter++;
//            }
//        }




//    public enum State { A, B, C }
//        public enum Trigger { Next }
//    }

