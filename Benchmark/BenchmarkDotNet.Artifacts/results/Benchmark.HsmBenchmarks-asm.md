## .NET 9.0.5 (9.0.525.21509), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
```assembly
; Benchmark.HsmBenchmarks.FastFSM_Hsm_AsyncYield()
       push      rbx
       sub       rsp,50
       xor       eax,eax
       mov       [rsp+28],rax
       vxorps    xmm4,xmm4,xmm4
       vmovdqu   ymmword ptr [rsp+30],ymm4
       mov       rbx,rdx
       mov       [rsp+28],rcx
       mov       dword ptr [rsp+30],0FFFFFFFF
       lea       rcx,[rsp+28]
       call      qword ptr [7FFA3EBF5D28]; System.Runtime.CompilerServices.AsyncMethodBuilderCore.Start[[Benchmark.HsmBenchmarks+<FastFSM_Hsm_AsyncYield>d__13, Benchmark]](<FastFSM_Hsm_AsyncYield>d__13 ByRef)
       lea       rcx,[rsp+38]
       mov       rdx,rbx
       call      qword ptr [7FFA3EB244C8]; System.Runtime.CompilerServices.AsyncValueTaskMethodBuilder.get_Task()
       mov       rax,rbx
       add       rsp,50
       pop       rbx
       ret
; Total bytes of code 72
```
```assembly
; BenchmarkDotNet.Helpers.AwaitHelper.GetResult(System.Threading.Tasks.ValueTask)
       push      r15
       push      r14
       push      r13
       push      r12
       push      rdi
       push      rsi
       push      rbp
       push      rbx
       sub       rsp,58
       vxorps    xmm4,xmm4,xmm4
       vmovdqu   ymmword ptr [rsp+30],ymm4
       xor       eax,eax
       mov       [rsp+50],rax
       mov       rbx,[rcx]
       movsx     rsi,word ptr [rcx+8]
       mov       edi,esi
       test      rbx,rbx
       jne       short M01_L01
M01_L00:
       add       rsp,58
       pop       rbx
       pop       rbp
       pop       rsi
       pop       rdi
       pop       r12
       pop       r13
       pop       r14
       pop       r15
       ret
M01_L01:
       mov       rdx,rbx
       mov       rcx,offset MT_System.Threading.Tasks.Task
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rbp,rax
       mov       rax,rbp
       test      rax,rax
       jne       near ptr M01_L07
       mov       rcx,offset MT_System.Runtime.CompilerServices.PoolingAsyncValueTaskMethodBuilder<System.Threading.Tasks.VoidTaskResult>+StateMachineBox<System.Threading.AsyncOverSyncWithIoCancellation+<InvokeAsync>d__7<System.ValueTuple<Microsoft.Win32.SafeHandles.SafeFileHandle, System.ReadOnlyMemory<System.Byte>, System.Int64, System.IO.Strategies.OSFileStreamStrategy>>>
       cmp       [rbx],rcx
       je        short M01_L06
       mov       rcx,rbx
       mov       edx,esi
       mov       r11,7FFA3E6C0728
       call      qword ptr [r11]
M01_L02:
       test      eax,eax
       jne       near ptr M01_L09
M01_L03:
       call      qword ptr [7FFA3EBF7900]; BenchmarkDotNet.Helpers.AwaitHelper+ValueTaskWaiter.get_Current()
       mov       r14,rax
       mov       r15,[r14+10]
       test      dword ptr [r15+18],40000000
       jne       near ptr M01_L27
       mov       rcx,[r15+10]
       test      rcx,rcx
       jne       near ptr M01_L17
M01_L04:
       xor       eax,eax
       mov       [rsp+48],eax
M01_L05:
       mov       eax,[r15+18]
       mov       [rsp+44],eax
       lea       rcx,[r15+18]
       mov       edx,eax
       and       edx,7FFFFFFF
       lock cmpxchg [rcx],edx
       cmp       eax,[rsp+44]
       je        short M01_L10
       lea       rcx,[rsp+48]
       mov       edx,0FFFFFFFF
       call      qword ptr [7FFA3EBF7AC8]; System.Threading.SpinWait.SpinOnceCore(Int32)
       jmp       short M01_L05
M01_L06:
       lea       rcx,[rbx+18]
       mov       edx,esi
       call      qword ptr [7FFA3EBFE9D0]
       jmp       short M01_L02
M01_L07:
       test      dword ptr [rax+34],1600000
       jne       short M01_L09
       jmp       short M01_L03
M01_L08:
       mov       rcx,[r14+10]
       cmp       [rcx],cl
       xor       r8d,r8d
       mov       edx,0FFFFFFFF
       call      qword ptr [7FFA3EBF7AF8]; System.Threading.ManualResetEventSlim.Wait(Int32, System.Threading.CancellationToken)
M01_L09:
       mov       rdx,rbx
       mov       rcx,offset MT_System.Threading.Tasks.Task
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rbp,rax
       mov       rcx,rbp
       test      rcx,rcx
       jne       near ptr M01_L16
       mov       rcx,rbx
       mov       edx,edi
       mov       r11,7FFA3E6C0738
       call      qword ptr [r11]
       jmp       near ptr M01_L00
M01_L10:
       mov       r13,[r14+8]
       mov       rcx,rbp
       test      rcx,rcx
       je        near ptr M01_L26
       mov       [rsp+30],rcx
       xor       ecx,ecx
       mov       [rsp+38],ecx
       mov       r12,[rsp+30]
       mov       rsi,r13
       mov       ebp,[rsp+38]
       and       ebp,1
       test      rsi,rsi
       je        near ptr M01_L18
       mov       rcx,1A2A3801480
       mov       rcx,[rcx]
       cmp       byte ptr [rcx+9D],0
       jne       short M01_L11
       cmp       byte ptr [7FFA3E6BB054],0
       je        short M01_L12
M01_L11:
       mov       rcx,r12
       mov       rdx,r13
       call      qword ptr [7FFA3EBF6F28]
       mov       rsi,rax
M01_L12:
       cmp       [r12],r12b
       test      ebp,ebp
       jne       near ptr M01_L19
M01_L13:
       test      dword ptr [r12+34],1600000
       jne       near ptr M01_L25
       cmp       qword ptr [r12+20],0
       jne       near ptr M01_L24
       lea       rcx,[r12+20]
       test      rcx,rcx
       je        near ptr M01_L23
       mov       rdx,rsi
       xor       r8d,r8d
       call      System.Threading.Interlocked.CompareExchangeObject(System.Object ByRef, System.Object, System.Object)
       test      rax,rax
       jne       near ptr M01_L24
M01_L14:
       xor       ecx,ecx
       mov       [rsp+50],ecx
       mov       rcx,[r14+10]
       test      dword ptr [rcx+18],80000000
       jne       near ptr M01_L09
M01_L15:
       cmp       dword ptr [rsp+50],0A
       jge       near ptr M01_L08
       lea       rcx,[rsp+50]
       mov       edx,14
       call      qword ptr [7FFA3EBF7AC8]; System.Threading.SpinWait.SpinOnceCore(Int32)
       mov       rcx,[r14+10]
       test      dword ptr [rcx+18],80000000
       je        short M01_L15
       jmp       near ptr M01_L09
M01_L16:
       mov       edx,[rcx+34]
       and       edx,11000000
       cmp       edx,1000000
       je        near ptr M01_L00
       jmp       near ptr M01_L28
M01_L17:
       call      qword ptr [7FFA3EBFE838]
       jmp       near ptr M01_L04
M01_L18:
       mov       ecx,14630
       mov       rdx,7FFA3E6B4000
       call      CORINFO_HELP_STRCNS
       mov       rcx,rax
       call      qword ptr [7FFA3EBFEC28]
       int       3
M01_L19:
       mov       ecx,4
       call      CORINFO_HELP_GETDYNAMIC_GCTHREADSTATIC_BASE_NOCTOR_OPTIMIZED
       mov       rax,[rax+10]
       test      rax,rax
       jne       short M01_L20
       call      qword ptr [7FFA3EA84C18]; System.Threading.Thread.InitializeCurrentThread()
M01_L20:
       mov       r13,[rax+10]
       test      r13,r13
       je        short M01_L21
       mov       rcx,offset MT_System.Threading.SynchronizationContext
       cmp       [r13],rcx
       je        short M01_L21
       mov       rcx,offset MT_System.Threading.Tasks.SynchronizationContextAwaitTaskContinuation
       call      CORINFO_HELP_NEWSFAST
       mov       r15,rax
       mov       rcx,r15
       mov       rdx,rsi
       xor       r8d,r8d
       call      qword ptr [7FFA3EBFE190]
       lea       rcx,[r15+20]
       mov       rdx,r13
       call      CORINFO_HELP_ASSIGN_REF
       jmp       short M01_L22
M01_L21:
       call      qword ptr [7FFA3EBF62B0]; System.Threading.Tasks.TaskScheduler.get_InternalCurrent()
       mov       r15,rax
       test      r15,r15
       je        near ptr M01_L13
       mov       rcx,1A2A38014D8
       cmp       r15,[rcx]
       je        near ptr M01_L13
       mov       rcx,offset MT_System.Threading.Tasks.TaskSchedulerAwaitTaskContinuation
       call      CORINFO_HELP_NEWSFAST
       mov       rbp,rax
       mov       rcx,rbp
       mov       rdx,rsi
       xor       r8d,r8d
       call      qword ptr [7FFA3EBFE190]
       lea       rcx,[rbp+20]
       mov       rdx,r15
       call      CORINFO_HELP_ASSIGN_REF
       mov       r15,rbp
M01_L22:
       mov       rcx,r12
       mov       rdx,r15
       xor       r8d,r8d
       call      qword ptr [7FFA3EBF7030]; System.Threading.Tasks.Task.AddTaskContinuation(System.Object, Boolean)
       test      eax,eax
       jne       near ptr M01_L14
       mov       rcx,r15
       mov       rdx,r12
       xor       r8d,r8d
       mov       rax,[r15]
       mov       rax,[rax+40]
       call      qword ptr [rax+20]
       jmp       near ptr M01_L14
M01_L23:
       call      qword ptr [7FFA3EBFE1A8]
       int       3
M01_L24:
       mov       rcx,r12
       mov       rdx,rsi
       xor       r8d,r8d
       call      qword ptr [7FFA3EBF7C00]; System.Threading.Tasks.Task.AddTaskContinuationComplex(System.Object, Boolean)
       test      eax,eax
       jne       near ptr M01_L14
M01_L25:
       mov       rcx,rsi
       mov       rdx,r12
       call      qword ptr [7FFA3EBFE388]
       jmp       near ptr M01_L14
M01_L26:
       mov       rcx,offset MT_System.Runtime.CompilerServices.ValueTaskAwaiter
       call      CORINFO_HELP_GET_GCSTATIC_BASE
       mov       rcx,1A2A3801618
       mov       rdx,[rcx]
       mov       r9d,esi
       xor       ecx,ecx
       mov       [rsp+20],ecx
       mov       rcx,rbx
       mov       r8,r13
       mov       r11,7FFA3E6C0730
       call      qword ptr [r11]
       jmp       near ptr M01_L14
M01_L27:
       mov       rcx,r15
       call      qword ptr [7FFA3EBFE898]
       int       3
M01_L28:
       xor       edx,edx
       call      qword ptr [7FFA3EBFE778]
       jmp       near ptr M01_L00
; Total bytes of code 999
```
```assembly
; System.Runtime.CompilerServices.AsyncMethodBuilderCore.Start[[Benchmark.HsmBenchmarks+<FastFSM_Hsm_AsyncYield>d__13, Benchmark]](<FastFSM_Hsm_AsyncYield>d__13 ByRef)
       push      rbp
       push      rsi
       push      rbx
       sub       rsp,40
       lea       rbp,[rsp+50]
       mov       [rbp-30],rsp
       mov       rbx,rcx
       cmp       [rbx],bl
       mov       rax,gs:[58]
       mov       rax,[rax+48]
       cmp       dword ptr [rax+208],4
       jle       short M02_L04
       mov       rax,[rax+210]
       mov       rax,[rax+20]
       test      rax,rax
       je        short M02_L04
M02_L00:
       mov       rsi,[rax+10]
       test      rsi,rsi
       jne       short M02_L01
       call      qword ptr [7FFA3EA84C18]; System.Threading.Thread.InitializeCurrentThread()
       mov       rsi,rax
M02_L01:
       mov       [rbp-18],rsi
       mov       rdx,[rsi+8]
       mov       [rbp-20],rdx
       mov       rcx,[rsi+10]
       mov       [rbp-28],rcx
       mov       rcx,rbx
       call      qword ptr [7FFA3EBF5D58]; Benchmark.HsmBenchmarks+<FastFSM_Hsm_AsyncYield>d__13.MoveNext()
       nop
       mov       rcx,[rbp-28]
       cmp       rcx,[rsi+10]
       jne       short M02_L05
M02_L02:
       mov       r8,[rsi+8]
       mov       rdx,[rbp-20]
       cmp       rdx,r8
       jne       short M02_L06
M02_L03:
       add       rsp,40
       pop       rbx
       pop       rsi
       pop       rbp
       ret
M02_L04:
       mov       ecx,4
       call      CORINFO_HELP_GETDYNAMIC_GCTHREADSTATIC_BASE_NOCTOR_OPTIMIZED
       jmp       short M02_L00
M02_L05:
       lea       rcx,[rsi+10]
       mov       rdx,[rbp-28]
       call      CORINFO_HELP_ASSIGN_REF
       jmp       short M02_L02
M02_L06:
       mov       rcx,rsi
       call      qword ptr [7FFA3EA8FC90]
       jmp       short M02_L03
       push      rbp
       push      rsi
       push      rbx
       sub       rsp,30
       mov       rbp,[rcx+20]
       mov       [rsp+20],rbp
       lea       rbp,[rbp+50]
       mov       rdx,[rbp-28]
       mov       rcx,[rbp-18]
       cmp       rdx,[rcx+10]
       je        short M02_L07
       lea       rcx,[rcx+10]
       call      CORINFO_HELP_ASSIGN_REF
       mov       rcx,[rbp-18]
M02_L07:
       mov       r8,[rcx+8]
       mov       rdx,[rbp-20]
       cmp       rdx,r8
       je        short M02_L08
       call      qword ptr [7FFA3EA8FC90]
M02_L08:
       nop
       add       rsp,30
       pop       rbx
       pop       rsi
       pop       rbp
       ret
; Total bytes of code 251
```
```assembly
; System.Runtime.CompilerServices.AsyncValueTaskMethodBuilder.get_Task()
       push      rdi
       push      rsi
       push      rbx
       sub       rsp,20
       mov       rsi,rcx
       mov       rbx,rdx
       mov       rdx,[rsi]
       mov       rcx,1A2A3800948
       cmp       rdx,[rcx]
       je        short M03_L03
       test      rdx,rdx
       je        short M03_L01
M03_L00:
       test      rdx,rdx
       je        short M03_L02
       mov       rcx,rbx
       call      CORINFO_HELP_CHECKED_ASSIGN_REF
       mov       word ptr [rbx+8],0
       mov       byte ptr [rbx+0A],1
       mov       rax,rbx
       add       rsp,20
       pop       rbx
       pop       rsi
       pop       rdi
       ret
M03_L01:
       mov       rcx,offset MT_System.Threading.Tasks.Task<System.Threading.Tasks.VoidTaskResult>
       call      CORINFO_HELP_NEWSFAST
       mov       rdi,rax
       mov       rcx,rdi
       call      qword ptr [7FFA3EBFE430]
       mov       rcx,rsi
       mov       rdx,rdi
       call      CORINFO_HELP_CHECKED_ASSIGN_REF
       mov       rdx,rdi
       jmp       short M03_L00
M03_L02:
       mov       ecx,9
       call      qword ptr [7FFA3E76FB28]
       int       3
M03_L03:
       xor       eax,eax
       mov       [rbx],rax
       mov       [rbx+8],rax
       mov       rax,rbx
       add       rsp,20
       pop       rbx
       pop       rsi
       pop       rdi
       ret
; Total bytes of code 145
```
```assembly
; System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       test      rdx,rdx
       je        short M04_L01
       cmp       [rdx],rcx
       je        short M04_L01
       mov       rax,[rdx]
       mov       r8,[rax+10]
M04_L00:
       cmp       r8,rcx
       jne       short M04_L02
M04_L01:
       mov       rax,rdx
       ret
M04_L02:
       test      r8,r8
       je        short M04_L03
       mov       r8,[r8+10]
       cmp       r8,rcx
       je        short M04_L01
       test      r8,r8
       je        short M04_L03
       mov       r8,[r8+10]
       cmp       r8,rcx
       je        short M04_L01
       test      r8,r8
       je        short M04_L03
       mov       r8,[r8+10]
       cmp       r8,rcx
       je        short M04_L01
       test      r8,r8
       je        short M04_L03
       mov       r8,[r8+10]
       jmp       short M04_L00
M04_L03:
       xor       edx,edx
       jmp       short M04_L01
; Total bytes of code 83
```
```assembly
; BenchmarkDotNet.Helpers.AwaitHelper+ValueTaskWaiter.get_Current()
       push      rbx
       sub       rsp,20
       mov       rax,gs:[58]
       mov       rax,[rax+48]
       cmp       dword ptr [rax+208],9
       jle       short M05_L02
       mov       rax,[rax+210]
       mov       rdx,[rax+48]
       test      rdx,rdx
       je        short M05_L02
M05_L00:
       mov       rax,[rdx+10]
       test      rax,rax
       je        short M05_L03
M05_L01:
       add       rsp,20
       pop       rbx
       ret
M05_L02:
       mov       ecx,9
       call      CORINFO_HELP_GETDYNAMIC_GCTHREADSTATIC_BASE_NOCTOR_OPTIMIZED
       mov       rdx,rax
       jmp       short M05_L00
M05_L03:
       mov       rcx,offset MT_BenchmarkDotNet.Helpers.AwaitHelper+ValueTaskWaiter
       call      CORINFO_HELP_NEWSFAST
       mov       rbx,rax
       mov       rcx,rbx
       call      qword ptr [7FFA3EBF7960]; BenchmarkDotNet.Helpers.AwaitHelper+ValueTaskWaiter..ctor()
       mov       ecx,9
       call      CORINFO_HELP_GETDYNAMIC_GCTHREADSTATIC_BASE_NOCTOR_OPTIMIZED
       lea       rcx,[rax+10]
       mov       rdx,rbx
       call      CORINFO_HELP_ASSIGN_REF
       mov       rax,rbx
       jmp       short M05_L01
; Total bytes of code 127
```
```assembly
; System.Threading.SpinWait.SpinOnceCore(Int32)
       push      rbp
       push      r15
       push      r14
       push      r13
       push      r12
       push      rdi
       push      rsi
       push      rbx
       sub       rsp,68
       vzeroupper
       lea       rbp,[rsp+0A0]
       mov       rbx,rcx
       mov       esi,edx
       lea       rcx,[rbp-70]
       call      CORINFO_HELP_INIT_PINVOKE_FRAME
       mov       rdi,rax
       mov       rax,rsp
       mov       [rbp-58],rax
       mov       rax,rbp
       mov       [rbp-48],rax
       mov       r14d,[rbx]
       cmp       r14d,0A
       jge       short M06_L04
M06_L00:
       call      System.Threading.Thread.get_OptimalMaxSpinWaitsPerSpinIteration()
       mov       ecx,eax
       mov       [rbp+10],rbx
       mov       eax,[rbx]
       cmp       eax,1E
       jg        short M06_L01
       mov       edx,1
       shlx      eax,edx,eax
       cmp       eax,ecx
       cmovl     ecx,eax
M06_L01:
       cmp       ecx,400
       jge       near ptr M06_L13
       mov       rax,7FFA9E34A9F0
       call      rax
       cmp       dword ptr [7FFA9E6CD624],0
       jne       near ptr M06_L12
M06_L02:
       mov       rbx,[rbp+10]
       mov       ecx,[rbx]
       cmp       ecx,7FFFFFFF
       je        near ptr M06_L14
       inc       ecx
M06_L03:
       mov       [rbx],ecx
       add       rsp,68
       pop       rbx
       pop       rsi
       pop       rdi
       pop       r12
       pop       r13
       pop       r14
       pop       r15
       pop       rbp
       ret
M06_L04:
       cmp       r14d,esi
       jl        short M06_L05
       test      esi,esi
       jge       short M06_L06
M06_L05:
       lea       eax,[r14-0A]
       test      al,1
       jne       short M06_L00
       cmp       r14d,esi
       jl        short M06_L07
M06_L06:
       test      esi,esi
       jge       near ptr M06_L11
M06_L07:
       add       r14d,0FFFFFFF6
       mov       ecx,r14d
       shr       ecx,1F
       add       ecx,r14d
       sar       ecx,1
       mov       edx,66666667
       mov       eax,edx
       imul      ecx
       mov       eax,edx
       shr       eax,1F
       sar       edx,1
       add       eax,edx
       lea       eax,[rax+rax*4]
       sub       ecx,eax
       cmp       ecx,4
       je        short M06_L10
       mov       [rbp+10],rbx
       mov       rax,7FFA3E7CECF8
       mov       [rbp-60],rax
       lea       rax,[M06_L08]
       mov       [rbp-50],rax
       lea       rax,[rbp-70]
       mov       [rdi+8],rax
       mov       byte ptr [rdi+4],0
       mov       rax,7FFA9E37ED90
       call      rax
M06_L08:
       mov       byte ptr [rdi+4],1
       cmp       dword ptr [7FFA9E6CD624],0
       je        short M06_L09
       call      qword ptr [7FFA9E6BB408]; CORINFO_HELP_STOP_FOR_GC
M06_L09:
       mov       rcx,[rbp-68]
       mov       [rdi+8],rcx
       jmp       near ptr M06_L02
M06_L10:
       xor       ecx,ecx
       call      qword ptr [7FFA3E76EFE8]; System.Threading.Thread.Sleep(Int32)
       mov       [rbp+10],rbx
       jmp       near ptr M06_L02
M06_L11:
       mov       ecx,1
       call      qword ptr [7FFA3E76EFE8]; System.Threading.Thread.Sleep(Int32)
       mov       [rbp+10],rbx
       jmp       near ptr M06_L02
M06_L12:
       call      CORINFO_HELP_POLL_GC
       jmp       near ptr M06_L02
M06_L13:
       call      qword ptr [7FFA3EBFDFE0]
       jmp       near ptr M06_L02
M06_L14:
       mov       ecx,0A
       jmp       near ptr M06_L03
; Total bytes of code 402
```
```assembly
; System.Threading.ManualResetEventSlim.Wait(Int32, System.Threading.CancellationToken)
       push      rbp
       push      r15
       push      r14
       push      r13
       push      r12
       push      rdi
       push      rsi
       push      rbx
       sub       rsp,0F8
       vzeroupper
       lea       rbp,[rsp+130]
       xor       eax,eax
       mov       [rbp-0F8],rax
       vxorps    xmm4,xmm4,xmm4
       vmovdqu32 [rbp-0F0],zmm4
       vmovdqu32 [rbp-0B0],zmm4
       vmovdqa   xmmword ptr [rbp-70],xmm4
       mov       [rbp-100],rsp
       mov       [rbp+10],rcx
       mov       [rbp+18],edx
       mov       [rbp+20],r8
       mov       rbx,rcx
       mov       esi,edx
       lea       rcx,[rbp-0D8]
       call      CORINFO_HELP_INIT_PINVOKE_FRAME
       mov       [rbp-0A0],rax
       mov       rcx,rsp
       mov       [rbp-0C0],rcx
       mov       rcx,rbp
       mov       [rbp-0B0],rcx
       test      dword ptr [rbx+18],40000000
       jne       near ptr M07_L41
       cmp       qword ptr [rbp+20],0
       jne       short M07_L01
M07_L00:
       cmp       esi,0FFFFFFFF
       jge       short M07_L02
       jmp       near ptr M07_L32
M07_L01:
       mov       rcx,[rbp+20]
       cmp       dword ptr [rcx+20],0
       je        short M07_L00
       jmp       near ptr M07_L40
M07_L02:
       test      dword ptr [rbx+18],80000000
       jne       near ptr M07_L38
       test      esi,esi
       je        near ptr M07_L37
       xor       ecx,ecx
       mov       [rbp-3C],ecx
       mov       [rbp-40],ecx
       mov       [rbp-44],esi
       cmp       esi,0FFFFFFFF
       jne       near ptr M07_L33
M07_L03:
       mov       edi,[rbx+18]
       and       edi,3FF80000
       shr       edi,13
       xor       ecx,ecx
       mov       [rbp-50],ecx
       cmp       [rbp-50],edi
       jge       short M07_L06
M07_L04:
       lea       rcx,[rbp-50]
       mov       edx,0FFFFFFFF
       call      qword ptr [7FFA3EBF7AC8]; System.Threading.SpinWait.SpinOnceCore(Int32)
       test      dword ptr [rbx+18],80000000
       jne       near ptr M07_L38
       cmp       dword ptr [rbp-50],64
       jge       near ptr M07_L34
M07_L05:
       cmp       [rbp-50],edi
       jl        short M07_L04
M07_L06:
       cmp       qword ptr [rbx+8],0
       jne       short M07_L07
       mov       rcx,offset MT_System.Object
       call      CORINFO_HELP_NEWSFAST
       mov       rdx,rax
       lea       rcx,[rbx+8]
       test      rcx,rcx
       je        near ptr M07_L35
       xor       r8d,r8d
       call      System.Threading.Interlocked.CompareExchangeObject(System.Object ByRef, System.Object, System.Object)
M07_L07:
       mov       rdx,1A2A3801570
       mov       r8,[rdx]
       mov       rcx,[rbp+20]
       test      rcx,rcx
       je        short M07_L08
       xor       edx,edx
       mov       [rsp+20],rdx
       mov       [rsp+28],rdx
       lea       rdx,[rbp-70]
       mov       r9,rbx
       call      qword ptr [7FFA3EBFEC58]
       mov       rax,[rbp-70]
       mov       rdx,[rbp-68]
       jmp       short M07_L09
M07_L08:
       xor       eax,eax
       xor       edx,edx
M07_L09:
       mov       [rbp-0F8],rax
       mov       [rbp-0E8],rdx
       mov       rdi,[rbx+8]
       mov       [rbp-0F0],rdi
       xor       edx,edx
       mov       [rbp-58],edx
       cmp       byte ptr [rbp-58],0
       jne       near ptr M07_L23
       lea       rdx,[rbp-58]
       mov       rcx,rdi
       call      System.Threading.Monitor.ReliableEnter(System.Object, Boolean ByRef)
       nop
M07_L10:
       mov       rbx,[rbp+10]
       test      dword ptr [rbx+18],80000000
       jne       near ptr M07_L29
       cmp       qword ptr [rbp+20],0
       jne       short M07_L14
M07_L11:
       cmp       dword ptr [rbp-40],0
       jne       near ptr M07_L22
M07_L12:
       mov       esi,[rbx+18]
       and       esi,7FFFF
       inc       esi
       cmp       esi,7FFFF
       jge       near ptr M07_L26
       xor       eax,eax
       mov       [rbp-78],eax
M07_L13:
       mov       eax,[rbx+18]
       mov       [rbp-7C],eax
       lea       rcx,[rbx+18]
       mov       edx,eax
       and       edx,0FFF80000
       or        edx,esi
       lock cmpxchg [rcx],edx
       cmp       eax,[rbp-7C]
       je        short M07_L15
       lea       rcx,[rbp-78]
       mov       edx,0FFFFFFFF
       call      qword ptr [7FFA3EBF7AC8]; System.Threading.SpinWait.SpinOnceCore(Int32)
       jmp       short M07_L13
M07_L14:
       mov       rax,[rbp+20]
       cmp       dword ptr [rax+20],0
       je        short M07_L11
       jmp       near ptr M07_L24
M07_L15:
       test      dword ptr [rbx+18],80000000
       jne       near ptr M07_L25
       mov       rcx,[rbx+8]
       mov       [rbp-88],rcx
       mov       rcx,[rbp-88]
       test      rcx,rcx
       je        near ptr M07_L18
       cmp       dword ptr [rbp-44],0FFFFFFFF
       jl        near ptr M07_L19
       lea       rcx,[rbp-88]
       mov       edx,[rbp-44]
       mov       rax,7FFA3E7D1258
       mov       [rbp-0C8],rax
       lea       rax,[M07_L16]
       mov       [rbp-0B8],rax
       mov       rax,[rbp-0A0]
       lea       r8,[rbp-0D8]
       mov       [rax+8],r8
       mov       rax,[rbp-0A0]
       mov       byte ptr [rax+4],0
       mov       rax,7FFA9E377AB0
       call      rax
M07_L16:
       mov       rcx,[rbp-0A0]
       mov       byte ptr [rcx+4],1
       cmp       dword ptr [7FFA9E6CD624],0
       je        short M07_L17
       call      qword ptr [7FFA9E6BB408]; CORINFO_HELP_STOP_FOR_GC
M07_L17:
       mov       rcx,[rbp-0A0]
       mov       rdx,[rbp-0D0]
       mov       [rcx+8],rdx
       test      eax,eax
       jne       short M07_L21
       jmp       short M07_L20
M07_L18:
       mov       ecx,137B
       mov       rdx,7FFA3E6B4000
       call      CORINFO_HELP_STRCNS
       mov       rcx,rax
       call      qword ptr [7FFA3EBFEC28]
       int       3
M07_L19:
       mov       ecx,13E3
       mov       rdx,7FFA3E6B4000
       call      CORINFO_HELP_STRCNS
       mov       r8,rax
       mov       ecx,[rbp-44]
       mov       edx,0FFFFFFFF
       call      qword ptr [7FFA3EBFEB38]
       int       3
M07_L20:
       xor       ecx,ecx
       mov       [rbp-5C],ecx
       jmp       near ptr M07_L28
M07_L21:
       mov       rcx,rsp
       call      M07_L42
       jmp       near ptr M07_L10
M07_L22:
       mov       ecx,[rbp-3C]
       mov       edx,[rbp+18]
       call      qword ptr [7FFA3EBFF018]
       mov       [rbp-44],eax
       cmp       dword ptr [rbp-44],0
       jg        near ptr M07_L12
       jmp       near ptr M07_L27
M07_L23:
       call      qword ptr [7FFA3EBFE868]
       int       3
M07_L24:
       lea       rcx,[rbp+20]
       call      qword ptr [7FFA3EBFE8B0]
       int       3
M07_L25:
       mov       edx,[rbx+18]
       and       edx,7FFFF
       dec       edx
       mov       rcx,rbx
       call      qword ptr [7FFA3EBF7BA0]; System.Threading.ManualResetEventSlim.set_Waiters(Int32)
       mov       dword ptr [rbp-5C],1
       jmp       short M07_L30
M07_L26:
       mov       rcx,offset MT_System.Int32
       call      CORINFO_HELP_NEWSFAST
       mov       rbx,rax
       call      qword ptr [7FFA3EBFEB80]
       mov       rsi,rax
       mov       dword ptr [rbx+8],7FFFF
       mov       rcx,offset MT_System.InvalidOperationException
       call      CORINFO_HELP_NEWSFAST
       mov       r14,rax
       mov       rdx,rbx
       mov       rcx,rsi
       call      qword ptr [7FFA3EBFEB98]
       mov       rdx,rax
       mov       rcx,r14
       call      qword ptr [7FFA3EA86CA0]
       mov       rcx,r14
       call      CORINFO_HELP_THROW
       int       3
M07_L27:
       xor       ecx,ecx
       mov       [rbp-5C],ecx
       jmp       short M07_L30
M07_L28:
       mov       rcx,rsp
       call      M07_L42
       jmp       short M07_L30
M07_L29:
       cmp       byte ptr [rbp-58],0
       je        short M07_L31
       mov       rcx,[rbp-0F0]
       call      System.Threading.Monitor.Exit(System.Object)
       jmp       short M07_L31
M07_L30:
       mov       rcx,rsp
       call      M07_L46
       jmp       near ptr M07_L36
M07_L31:
       mov       rax,[rbp-0F8]
       test      rax,rax
       je        near ptr M07_L38
       mov       rcx,[rax+8]
       mov       rdx,[rbp-0E8]
       mov       r8,rax
       cmp       [rcx],ecx
       call      qword ptr [7FFA3EBFEC70]
       test      eax,eax
       jne       near ptr M07_L38
       mov       rcx,[rbp-0E8]
       mov       rdx,[rbp-0F8]
       call      qword ptr [7FFA3EBFF030]
       jmp       near ptr M07_L38
M07_L32:
       mov       ecx,13E3
       mov       rdx,7FFA3E6B4000
       call      CORINFO_HELP_STRCNS
       mov       r8,rax
       mov       ecx,esi
       mov       edx,0FFFFFFFF
       call      qword ptr [7FFA3EBFEB38]
       int       3
M07_L33:
       call      System.Environment.get_TickCount()
       mov       [rbp-3C],eax
       mov       dword ptr [rbp-40],1
       jmp       near ptr M07_L03
M07_L34:
       mov       ecx,[rbp-50]
       mov       edx,66666667
       mov       eax,edx
       imul      dword ptr [rbp-50]
       mov       eax,edx
       shr       eax,1F
       sar       edx,2
       add       eax,edx
       lea       eax,[rax+rax*4]
       add       eax,eax
       sub       ecx,eax
       jne       near ptr M07_L05
       cmp       qword ptr [rbp+20],0
       je        near ptr M07_L05
       mov       rax,[rbp+20]
       cmp       dword ptr [rax+20],0
       setne     al
       movzx     eax,al
       test      eax,eax
       je        near ptr M07_L05
       jmp       short M07_L40
M07_L35:
       call      qword ptr [7FFA3EBFE1A8]
       int       3
M07_L36:
       mov       rcx,rsp
       call      M07_L48
       nop
       mov       eax,[rbp-5C]
       jmp       short M07_L39
M07_L37:
       xor       eax,eax
       jmp       short M07_L39
M07_L38:
       mov       eax,1
M07_L39:
       movzx     eax,al
       add       rsp,0F8
       pop       rbx
       pop       rsi
       pop       rdi
       pop       r12
       pop       r13
       pop       r14
       pop       r15
       pop       rbp
       ret
M07_L40:
       lea       rcx,[rbp+20]
       call      qword ptr [7FFA3EBFE8B0]
       int       3
M07_L41:
       mov       rcx,rbx
       call      qword ptr [7FFA3EBFE898]
       int       3
M07_L42:
       push      rbp
       push      r15
       push      r14
       push      r13
       push      r12
       push      rdi
       push      rsi
       push      rbx
       sub       rsp,38
       vzeroupper
       mov       rbp,[rcx+30]
       mov       [rsp+30],rbp
       lea       rbp,[rbp+130]
       mov       rbx,[rbp+10]
       mov       esi,[rbx+18]
       and       esi,7FFFF
       dec       esi
       cmp       esi,7FFFF
       jge       short M07_L45
       xor       eax,eax
       mov       [rbp-90],eax
M07_L43:
       mov       eax,[rbx+18]
       mov       [rbp-94],eax
       lea       rcx,[rbx+18]
       mov       edx,eax
       and       edx,0FFF80000
       or        edx,esi
       lock cmpxchg [rcx],edx
       cmp       eax,[rbp-94]
       jne       short M07_L44
       add       rsp,38
       pop       rbx
       pop       rsi
       pop       rdi
       pop       r12
       pop       r13
       pop       r14
       pop       r15
       pop       rbp
       ret
M07_L44:
       lea       rcx,[rbp-90]
       mov       edx,0FFFFFFFF
       call      qword ptr [7FFA3EBF7AC8]; System.Threading.SpinWait.SpinOnceCore(Int32)
       jmp       short M07_L43
M07_L45:
       mov       rcx,offset MT_System.Int32
       call      CORINFO_HELP_NEWSFAST
       mov       rbx,rax
       call      qword ptr [7FFA3EBFEB80]
       mov       rsi,rax
       mov       dword ptr [rbx+8],7FFFF
       mov       rcx,offset MT_System.InvalidOperationException
       call      CORINFO_HELP_NEWSFAST
       mov       rdi,rax
       mov       rdx,rbx
       mov       rcx,rsi
       call      qword ptr [7FFA3EBFEB98]
       mov       rdx,rax
       mov       rcx,rdi
       call      qword ptr [7FFA3EA86CA0]
       mov       rcx,rdi
       call      CORINFO_HELP_THROW
       int       3
M07_L46:
       push      rbp
       push      r15
       push      r14
       push      r13
       push      r12
       push      rdi
       push      rsi
       push      rbx
       sub       rsp,38
       vzeroupper
       mov       rbp,[rcx+30]
       mov       [rsp+30],rbp
       lea       rbp,[rbp+130]
       cmp       byte ptr [rbp-58],0
       je        short M07_L47
       mov       rcx,[rbp-0F0]
       call      System.Threading.Monitor.Exit(System.Object)
M07_L47:
       nop
       add       rsp,38
       pop       rbx
       pop       rsi
       pop       rdi
       pop       r12
       pop       r13
       pop       r14
       pop       r15
       pop       rbp
       ret
M07_L48:
       push      rbp
       push      r15
       push      r14
       push      r13
       push      r12
       push      rdi
       push      rsi
       push      rbx
       sub       rsp,38
       vzeroupper
       mov       rbp,[rcx+30]
       mov       [rsp+30],rbp
       lea       rbp,[rbp+130]
       cmp       qword ptr [rbp-0F8],0
       je        short M07_L49
       mov       r8,[rbp-0F8]
       mov       rcx,[r8+8]
       mov       rdx,[rbp-0E8]
       cmp       [rcx],ecx
       call      qword ptr [7FFA3EBFEC70]
       test      eax,eax
       jne       short M07_L49
       mov       rcx,[rbp-0E8]
       mov       rdx,[rbp-0F8]
       call      qword ptr [7FFA3EBFF030]
M07_L49:
       nop
       add       rsp,38
       pop       rbx
       pop       rsi
       pop       rdi
       pop       r12
       pop       r13
       pop       r14
       pop       r15
       pop       rbp
       ret
; Total bytes of code 1729
```
```assembly
; System.Threading.Thread.InitializeCurrentThread()
       push      rbx
       sub       rsp,20
       call      qword ptr [7FFA960C8F58]; System.Threading.Thread.GetCurrentThreadNative()
       mov       rbx,rax
       call      qword ptr [7FFA960B2B68]
       lea       rcx,[rax+10]
       mov       rdx,rbx
       call      qword ptr [7FFA960B10F0]; CORINFO_HELP_ASSIGN_REF
       mov       rax,rbx
       add       rsp,20
       pop       rbx
       ret
; Total bytes of code 42
```
```assembly
; System.Threading.Tasks.TaskScheduler.get_InternalCurrent()
       push      rbx
       sub       rsp,20
       mov       rax,gs:[58]
       mov       rax,[rax+48]
       cmp       dword ptr [rax+208],5
       jle       short M09_L02
       mov       rax,[rax+210]
       mov       rdx,[rax+28]
       test      rdx,rdx
       je        short M09_L02
M09_L00:
       mov       rbx,[rdx+10]
       test      rbx,rbx
       jne       short M09_L03
M09_L01:
       xor       eax,eax
       add       rsp,20
       pop       rbx
       ret
M09_L02:
       mov       ecx,5
       call      CORINFO_HELP_GETDYNAMIC_GCTHREADSTATIC_BASE_NOCTOR_OPTIMIZED
       mov       rdx,rax
       jmp       short M09_L00
M09_L03:
       test      byte ptr [rbx+34],10
       jne       short M09_L01
       mov       rax,[rbx+18]
       add       rsp,20
       pop       rbx
       ret
; Total bytes of code 91
```
```assembly
; System.Threading.Tasks.Task.AddTaskContinuation(System.Object, Boolean)
       push      rdi
       push      rsi
       push      rbx
       sub       rsp,20
       mov       rbx,rcx
       mov       rsi,rdx
       mov       edi,r8d
       test      dword ptr [rbx+34],1600000
       jne       short M10_L02
       cmp       qword ptr [rbx+20],0
       jne       short M10_L01
       lea       rcx,[rbx+20]
       test      rcx,rcx
       je        short M10_L00
       mov       rdx,rsi
       xor       r8d,r8d
       call      System.Threading.Interlocked.CompareExchangeObject(System.Object ByRef, System.Object, System.Object)
       test      rax,rax
       jne       short M10_L01
       mov       eax,1
       add       rsp,20
       pop       rbx
       pop       rsi
       pop       rdi
       ret
M10_L00:
       call      qword ptr [7FFA3EBFE1A8]
       int       3
M10_L01:
       movzx     r8d,dil
       mov       rcx,rbx
       mov       rdx,rsi
       add       rsp,20
       pop       rbx
       pop       rsi
       pop       rdi
       jmp       qword ptr [7FFA3EBF7C00]; System.Threading.Tasks.Task.AddTaskContinuationComplex(System.Object, Boolean)
M10_L02:
       xor       eax,eax
       add       rsp,20
       pop       rbx
       pop       rsi
       pop       rdi
       ret
; Total bytes of code 110
```
```assembly
; System.Threading.Tasks.Task.AddTaskContinuationComplex(System.Object, Boolean)
       push      rbp
       push      r15
       push      r14
       push      rdi
       push      rsi
       push      rbx
       sub       rsp,48
       lea       rbp,[rsp+70]
       mov       [rbp-50],rsp
       mov       rbx,rcx
       mov       rsi,rdx
       mov       edi,r8d
       mov       r14,[rbx+20]
       mov       rax,1A2A3800218
       cmp       r14,[rax]
       jne       near ptr M11_L07
M11_L00:
       xor       eax,eax
       add       rsp,48
       pop       rbx
       pop       rsi
       pop       rdi
       pop       r14
       pop       r15
       pop       rbp
       ret
M11_L01:
       lea       rdx,[rbp-30]
       mov       rcx,r15
       call      qword ptr [7FFA3EA8DD10]; System.Threading.Monitor.Enter(System.Object, Boolean ByRef)
       mov       rcx,[rbx+20]
       mov       rax,1A2A3800218
       cmp       rcx,[rax]
       jne       short M11_L02
       xor       ecx,ecx
       mov       [rbp-34],ecx
       jmp       near ptr M11_L18
M11_L02:
       mov       ecx,[r15+10]
       mov       rax,[r15+8]
       cmp       ecx,[rax+8]
       jne       short M11_L04
       mov       rcx,1A2A3800270
       mov       rdx,[rcx]
       test      rdx,rdx
       jne       short M11_L03
       mov       rcx,offset MT_System.Predicate<System.Object>
       call      CORINFO_HELP_NEWSFAST
       mov       rbx,rax
       mov       rdx,1A2A3800240
       mov       rdx,[rdx]
       mov       rcx,rbx
       mov       r8,7FFA3EBFAB08
       call      qword ptr [7FFA3E7669D0]; System.MulticastDelegate.CtorClosed(System.Object, IntPtr)
       mov       rcx,1A2A3800270
       mov       rdx,rbx
       call      CORINFO_HELP_ASSIGN_REF
       mov       rdx,rbx
M11_L03:
       mov       rcx,r15
       call      qword ptr [7FFA3EBFEB20]
M11_L04:
       test      dil,dil
       je        short M11_L05
       mov       rcx,r15
       mov       r8,rsi
       xor       edx,edx
       call      qword ptr [7FFA3E87DBD8]
       jmp       near ptr M11_L19
M11_L05:
       inc       dword ptr [r15+14]
       mov       rcx,[r15+8]
       mov       edx,[r15+10]
       cmp       [rcx+8],edx
       jbe       short M11_L06
       lea       r8d,[rdx+1]
       mov       [r15+10],r8d
       movsxd    rdx,edx
       mov       r8,rsi
       call      qword ptr [7FFA3E765758]; System.Runtime.CompilerServices.CastHelpers.StelemRef(System.Object[], IntPtr, System.Object)
       jmp       near ptr M11_L19
M11_L06:
       mov       rcx,r15
       mov       rdx,rsi
       call      qword ptr [7FFA3E766F40]; System.Collections.Generic.List`1[[System.__Canon, System.Private.CoreLib]].AddWithResize(System.__Canon)
       jmp       near ptr M11_L19
M11_L07:
       mov       rdx,r14
       mov       rcx,offset MT_System.Collections.Generic.List<System.Object>
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       r15,rax
       test      r15,r15
       jne       near ptr M11_L17
       mov       rcx,offset MT_System.Collections.Generic.List<System.Object>
       call      CORINFO_HELP_NEWSFAST
       mov       r15,rax
       mov       rcx,r15
       call      qword ptr [7FFA3EA87420]; System.Collections.Generic.List`1[[System.__Canon, System.Private.CoreLib]]..ctor()
       test      dil,dil
       je        short M11_L11
       inc       dword ptr [r15+14]
       mov       rcx,[r15+8]
       mov       edx,[r15+10]
       cmp       [rcx+8],edx
       jbe       short M11_L08
       lea       r8d,[rdx+1]
       mov       [r15+10],r8d
       movsxd    rdx,edx
       mov       r8,rsi
       call      qword ptr [7FFA3E765758]; System.Runtime.CompilerServices.CastHelpers.StelemRef(System.Object[], IntPtr, System.Object)
       jmp       short M11_L09
M11_L08:
       mov       rcx,r15
       mov       rdx,rsi
       call      qword ptr [7FFA3E766F40]; System.Collections.Generic.List`1[[System.__Canon, System.Private.CoreLib]].AddWithResize(System.__Canon)
M11_L09:
       inc       dword ptr [r15+14]
       mov       rcx,[r15+8]
       mov       edx,[r15+10]
       cmp       [rcx+8],edx
       jbe       short M11_L10
       lea       r8d,[rdx+1]
       mov       [r15+10],r8d
       movsxd    rdx,edx
       mov       r8,r14
       call      qword ptr [7FFA3E765758]; System.Runtime.CompilerServices.CastHelpers.StelemRef(System.Object[], IntPtr, System.Object)
       jmp       short M11_L15
M11_L10:
       mov       rcx,r15
       mov       rdx,r14
       call      qword ptr [7FFA3E766F40]; System.Collections.Generic.List`1[[System.__Canon, System.Private.CoreLib]].AddWithResize(System.__Canon)
       jmp       short M11_L15
M11_L11:
       inc       dword ptr [r15+14]
       mov       rcx,[r15+8]
       mov       edx,[r15+10]
       cmp       [rcx+8],edx
       jbe       short M11_L12
       lea       r8d,[rdx+1]
       mov       [r15+10],r8d
       movsxd    rdx,edx
       mov       r8,r14
       call      qword ptr [7FFA3E765758]; System.Runtime.CompilerServices.CastHelpers.StelemRef(System.Object[], IntPtr, System.Object)
       jmp       short M11_L13
M11_L12:
       mov       rcx,r15
       mov       rdx,r14
       call      qword ptr [7FFA3E766F40]; System.Collections.Generic.List`1[[System.__Canon, System.Private.CoreLib]].AddWithResize(System.__Canon)
M11_L13:
       inc       dword ptr [r15+14]
       mov       rcx,[r15+8]
       mov       edx,[r15+10]
       cmp       [rcx+8],edx
       jbe       short M11_L14
       lea       r8d,[rdx+1]
       mov       [r15+10],r8d
       movsxd    rdx,edx
       mov       r8,rsi
       call      qword ptr [7FFA3E765758]; System.Runtime.CompilerServices.CastHelpers.StelemRef(System.Object[], IntPtr, System.Object)
       jmp       short M11_L15
M11_L14:
       mov       rcx,r15
       mov       rdx,rsi
       call      qword ptr [7FFA3E766F40]; System.Collections.Generic.List`1[[System.__Canon, System.Private.CoreLib]].AddWithResize(System.__Canon)
M11_L15:
       lea       rcx,[rbx+20]
       test      rcx,rcx
       jne       short M11_L16
       call      qword ptr [7FFA3EBFE1A8]
       int       3
M11_L16:
       mov       rdx,r15
       mov       r8,r14
       call      System.Threading.Interlocked.CompareExchangeObject(System.Object ByRef, System.Object, System.Object)
       mov       r15,rax
       cmp       r15,r14
       je        short M11_L20
       mov       rdx,r15
       mov       rcx,offset MT_System.Collections.Generic.List<System.Object>
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       r15,rax
       test      r15,r15
       je        near ptr M11_L00
M11_L17:
       mov       [rbp-40],r15
       xor       edx,edx
       mov       [rbp-30],edx
       jmp       near ptr M11_L01
M11_L18:
       mov       rcx,rsp
       call      M11_L21
       nop
       mov       eax,[rbp-34]
       movzx     eax,al
       add       rsp,48
       pop       rbx
       pop       rsi
       pop       rdi
       pop       r14
       pop       r15
       pop       rbp
       ret
M11_L19:
       cmp       byte ptr [rbp-30],0
       je        short M11_L20
       mov       rcx,r15
       call      System.Threading.Monitor.Exit(System.Object)
M11_L20:
       mov       eax,1
       add       rsp,48
       pop       rbx
       pop       rsi
       pop       rdi
       pop       r14
       pop       r15
       pop       rbp
       ret
M11_L21:
       push      rbp
       push      r15
       push      r14
       push      rdi
       push      rsi
       push      rbx
       sub       rsp,28
       mov       rbp,[rcx+20]
       mov       [rsp+20],rbp
       lea       rbp,[rbp+70]
       cmp       byte ptr [rbp-30],0
       je        short M11_L22
       mov       rcx,[rbp-40]
       call      System.Threading.Monitor.Exit(System.Object)
M11_L22:
       nop
       add       rsp,28
       pop       rbx
       pop       rsi
       pop       rdi
       pop       r14
       pop       r15
       pop       rbp
       ret
; Total bytes of code 765
```
```assembly
; Benchmark.HsmBenchmarks+<FastFSM_Hsm_AsyncYield>d__13.MoveNext()
       push      rbp
       push      r14
       push      rdi
       push      rsi
       push      rbx
       sub       rsp,90
       lea       rbp,[rsp+0B0]
       vxorps    xmm4,xmm4,xmm4
       vmovdqu32 [rbp-80],zmm4
       vmovdqa   xmmword ptr [rbp-40],xmm4
       vmovdqa   xmmword ptr [rbp-30],xmm4
       mov       [rbp-90],rsp
       mov       [rbp+10],rcx
       mov       rsi,rcx
       mov       ecx,[rsi+8]
       mov       rbx,[rsi]
       test      ecx,ecx
       jne       near ptr M12_L04
       lea       rdi,[rsi+18]
       vmovdqu   xmm0,xmmword ptr [rdi]
       vmovdqu   xmmword ptr [rbp-30],xmm0
       xor       ecx,ecx
       mov       [rdi],rcx
       mov       [rdi+8],rcx
       mov       dword ptr [rsi+8],0FFFFFFFF
M12_L00:
       mov       r14,[rbp-30]
       test      r14,r14
       jne       near ptr M12_L14
M12_L01:
       inc       dword ptr [rsi+0C]
       cmp       dword ptr [rsi+0C],400
       jge       short M12_L05
M12_L02:
       mov       rcx,[rbx+10]
       cmp       [rcx],cl
       vxorps    ymm0,ymm0,ymm0
       vmovdqu32 [rbp-80],zmm0
       vmovdqu   xmmword ptr [rbp-40],xmm0
       vxorps    xmm0,xmm0,xmm0
       vmovdqu   xmmword ptr [rbp-68],xmm0
       mov       [rbp-80],rcx
       mov       dword ptr [rbp-6C],2
       xor       ecx,ecx
       mov       [rbp-78],rcx
       mov       [rbp-58],rcx
       mov       dword ptr [rbp-70],0FFFFFFFF
       lea       rcx,[rbp-80]
       call      qword ptr [7FFA3EB24AB0]; System.Runtime.CompilerServices.AsyncMethodBuilderCore.Start[[StateMachine.Runtime.AsyncStateMachineBase`2+<TryFireAsync>d__22[[Benchmark.HsmState, Benchmark],[Benchmark.HsmTrigger, Benchmark]], StateMachine]](<TryFireAsync>d__22<Benchmark.HsmState,Benchmark.HsmTrigger> ByRef)
       mov       rax,[rbp-68]
       mov       rcx,1A2A3800970
       cmp       rax,[rcx]
       je        short M12_L08
       test      rax,rax
       je        short M12_L09
M12_L03:
       test      rax,rax
       jne       short M12_L06
       jmp       short M12_L10
M12_L04:
       xor       ecx,ecx
       mov       [rsi+0C],ecx
       cmp       dword ptr [rsi+0C],400
       jl        short M12_L02
M12_L05:
       mov       rcx,[rbx+10]
       mov       ecx,[rcx+18]
       call      qword ptr [7FFA3EBF5D88]; BenchmarkDotNet.Engines.DeadCodeEliminationHelper.KeepAliveWithoutBoxing[[Benchmark.HsmState, Benchmark]](Benchmark.HsmState)
       jmp       near ptr M12_L16
M12_L06:
       xor       ecx,ecx
M12_L07:
       mov       [rbp-30],rax
       mov       word ptr [rbp-28],0
       mov       [rbp-26],cl
       mov       byte ptr [rbp-25],1
       mov       rdi,[rbp-30]
       test      rdi,rdi
       je        near ptr M12_L00
       jmp       short M12_L11
M12_L08:
       movzx     ecx,byte ptr [rbp-60]
       xor       eax,eax
       jmp       short M12_L07
M12_L09:
       mov       rcx,offset MT_System.Threading.Tasks.Task<System.Boolean>
       call      CORINFO_HELP_NEWSFAST
       mov       dword ptr [rax+34],2000400
       mov       [rbp-68],rax
       jmp       short M12_L03
M12_L10:
       mov       ecx,9
       call      qword ptr [7FFA3E76FB28]
       int       3
M12_L11:
       mov       rdx,rdi
       mov       rcx,offset MT_System.Threading.Tasks.Task<System.Boolean>
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       test      rax,rax
       je        short M12_L12
       test      dword ptr [rax+34],1600000
       setne     r14b
       movzx     r14d,r14b
       jmp       short M12_L13
M12_L12:
       mov       rcx,rdi
       movsx     rdx,word ptr [rbp-28]
       mov       r11,7FFA3E6C0708
       call      qword ptr [r11]
       test      eax,eax
       setne     r14b
       movzx     r14d,r14b
M12_L13:
       test      r14d,r14d
       jne       near ptr M12_L00
       xor       eax,eax
       mov       [rsi+8],eax
       lea       rdi,[rsi+18]
       lea       rsi,[rbp-30]
       call      CORINFO_HELP_ASSIGN_BYREF
       movsq
       mov       rsi,[rbp+10]
       lea       rdx,[rsi+10]
       mov       rcx,rsi
       call      qword ptr [7FFA3EBF7780]; System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1[[System.Threading.Tasks.VoidTaskResult, System.Private.CoreLib]].GetStateMachineBox[[Benchmark.HsmBenchmarks+<FastFSM_Hsm_AsyncYield>d__13, Benchmark]](<FastFSM_Hsm_AsyncYield>d__13 ByRef, System.Threading.Tasks.Task`1<System.Threading.Tasks.VoidTaskResult> ByRef)
       mov       rdx,rax
       lea       rcx,[rbp-30]
       call      qword ptr [7FFA3EBF7798]; System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1[[System.Threading.Tasks.VoidTaskResult, System.Private.CoreLib]].AwaitUnsafeOnCompleted[[System.Runtime.CompilerServices.ValueTaskAwaiter`1[[System.Boolean, System.Private.CoreLib]], System.Private.CoreLib]](System.Runtime.CompilerServices.ValueTaskAwaiter`1<Boolean> ByRef, System.Runtime.CompilerServices.IAsyncStateMachineBox)
       jmp       short M12_L17
M12_L14:
       mov       rdx,r14
       mov       rcx,offset MT_System.Threading.Tasks.Task<System.Boolean>
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       test      rax,rax
       je        short M12_L15
       mov       ecx,[rax+34]
       and       ecx,11000000
       cmp       ecx,1000000
       je        near ptr M12_L01
       mov       rcx,rax
       xor       edx,edx
       call      qword ptr [7FFA3EBFE778]
       jmp       near ptr M12_L01
M12_L15:
       mov       rcx,r14
       movsx     rdx,word ptr [rbp-28]
       mov       r11,7FFA3E6C0710
       call      qword ptr [r11]
       jmp       near ptr M12_L01
M12_L16:
       mov       dword ptr [rsi+8],0FFFFFFFE
       lea       rcx,[rsi+10]
       call      qword ptr [7FFA3EB24600]; System.Runtime.CompilerServices.AsyncValueTaskMethodBuilder.SetResult()
M12_L17:
       nop
       add       rsp,90
       pop       rbx
       pop       rsi
       pop       rdi
       pop       r14
       pop       rbp
       ret
       push      rbp
       push      r14
       push      rdi
       push      rsi
       push      rbx
       sub       rsp,30
       mov       rbp,[rcx+20]
       mov       [rsp+20],rbp
       lea       rbp,[rbp+0B0]
       mov       rsi,[rbp+10]
       mov       dword ptr [rsi+8],0FFFFFFFE
       lea       rcx,[rsi+10]
       call      qword ptr [7FFA3EB24618]
       lea       rax,[M12_L17]
       add       rsp,30
       pop       rbx
       pop       rsi
       pop       rdi
       pop       r14
       pop       rbp
       ret
; Total bytes of code 653
```
```assembly
; BenchmarkDotNet.Helpers.AwaitHelper+ValueTaskWaiter..ctor()
       push      rbp
       sub       rsp,30
       lea       rbp,[rsp+30]
       xor       eax,eax
       mov       [rbp-8],rax
       mov       [rbp-10],rax
       mov       [rbp+10],rcx
       mov       rcx,[rbp+10]
       call      qword ptr [7FFA3E76C8A0]; System.Object..ctor()
       mov       rcx,offset MT_System.Threading.ManualResetEventSlim
       call      CORINFO_HELP_NEWSFAST
       mov       [rbp-8],rax
       mov       rcx,[rbp-8]
       call      qword ptr [7FFA3EBF7990]; System.Threading.ManualResetEventSlim..ctor()
       mov       rax,[rbp+10]
       lea       rcx,[rax+10]
       mov       rdx,[rbp-8]
       call      CORINFO_HELP_ASSIGN_REF
       mov       rcx,offset MT_System.Action
       call      CORINFO_HELP_NEWSFAST
       mov       [rbp-10],rax
       mov       rax,[rbp+10]
       mov       rdx,[rax+10]
       mov       rcx,[rbp-10]
       mov       r8,offset System.Threading.ManualResetEventSlim.Set()
       call      qword ptr [7FFA3E7669D0]; System.MulticastDelegate.CtorClosed(System.Object, IntPtr)
       mov       rax,[rbp+10]
       lea       rcx,[rax+8]
       mov       rdx,[rbp-10]
       call      CORINFO_HELP_ASSIGN_REF
       nop
       add       rsp,30
       pop       rbp
       ret
; Total bytes of code 151
```
```assembly
; System.Threading.Thread.Sleep(Int32)
       push      rbp
       push      r15
       push      r14
       push      r13
       push      r12
       push      rdi
       push      rsi
       push      rbx
       sub       rsp,68
       vzeroupper
       lea       rbp,[rsp+0A0]
       mov       ebx,ecx
       lea       rcx,[rbp-70]
       call      CORINFO_HELP_INIT_PINVOKE_FRAME
       mov       rsi,rax
       mov       rcx,rsp
       mov       [rbp-58],rcx
       mov       rcx,rbp
       mov       [rbp-48],rcx
       cmp       ebx,0FFFFFFFF
       jl        short M14_L02
       mov       ecx,ebx
       mov       rax,7FFA3E7CEBF8
       mov       [rbp-60],rax
       lea       rax,[M14_L00]
       mov       [rbp-50],rax
       lea       rax,[rbp-70]
       mov       [rsi+8],rax
       mov       byte ptr [rsi+4],0
       mov       rax,7FFA9E26CE30
       call      rax
M14_L00:
       mov       byte ptr [rsi+4],1
       cmp       dword ptr [7FFA9E6CD624],0
       je        short M14_L01
       call      qword ptr [7FFA9E6BB408]; CORINFO_HELP_STOP_FOR_GC
M14_L01:
       mov       rcx,[rbp-68]
       mov       [rsi+8],rcx
       add       rsp,68
       pop       rbx
       pop       rsi
       pop       rdi
       pop       r12
       pop       r13
       pop       r14
       pop       r15
       pop       rbp
       ret
M14_L02:
       mov       ecx,13E3
       mov       rdx,7FFA3E6B4000
       call      CORINFO_HELP_STRCNS
       mov       r8,rax
       mov       ecx,ebx
       mov       edx,0FFFFFFFF
       call      qword ptr [7FFA3EBFEB38]
       int       3
; Total bytes of code 192
```
```assembly
; System.Threading.ManualResetEventSlim.set_Waiters(Int32)
       push      rdi
       push      rsi
       push      rbx
       sub       rsp,30
       mov       rbx,rcx
       mov       esi,edx
       cmp       esi,7FFFF
       jge       short M15_L02
       xor       eax,eax
       mov       [rsp+28],eax
M15_L00:
       mov       eax,[rbx+18]
       mov       [rsp+24],eax
       lea       rcx,[rbx+18]
       mov       edx,eax
       and       edx,0FFF80000
       or        edx,esi
       lock cmpxchg [rcx],edx
       cmp       eax,[rsp+24]
       jne       short M15_L01
       add       rsp,30
       pop       rbx
       pop       rsi
       pop       rdi
       ret
M15_L01:
       lea       rcx,[rsp+28]
       mov       edx,0FFFFFFFF
       call      qword ptr [7FFA3EBF7AC8]; System.Threading.SpinWait.SpinOnceCore(Int32)
       jmp       short M15_L00
M15_L02:
       mov       rcx,offset MT_System.Int32
       call      CORINFO_HELP_NEWSFAST
       mov       rbx,rax
       call      qword ptr [7FFA3EBFEB80]
       mov       rsi,rax
       mov       dword ptr [rbx+8],7FFFF
       mov       rcx,offset MT_System.InvalidOperationException
       call      CORINFO_HELP_NEWSFAST
       mov       rdi,rax
       mov       rdx,rbx
       mov       rcx,rsi
       call      qword ptr [7FFA3EBFEB98]
       mov       rdx,rax
       mov       rcx,rdi
       call      qword ptr [7FFA3EA86CA0]
       mov       rcx,rdi
       call      CORINFO_HELP_THROW
       int       3
; Total bytes of code 168
```
```assembly
; System.Threading.Monitor.Enter(System.Object, Boolean ByRef)
       sub       rsp,28
       cmp       byte ptr [rdx],0
       jne       short M16_L00
       lea       rax,[System.Reflection.CustomAttributeExtensions.GetCustomAttribute[[System.__Canon, System.Private.CoreLib]](System.Reflection.Assembly)]
       add       rsp,28
       jmp       qword ptr [rax]
M16_L00:
       call      qword ptr [7FFA960C8EB8]
       int       3
; Total bytes of code 30
```
```assembly
; System.MulticastDelegate.CtorClosed(System.Object, IntPtr)
       push      rdi
       push      rsi
       push      rbx
       sub       rsp,20
       mov       rbx,rcx
       mov       rsi,rdx
       mov       rdi,r8
       test      rsi,rsi
       je        short M17_L00
       mov       rcx,7FFA3ED35BAC
       call      CORINFO_HELP_COUNTPROFILE32
       lea       rcx,[rbx+8]
       mov       rdx,rsi
       call      CORINFO_HELP_ASSIGN_REF
       mov       [rbx+18],rdi
       add       rsp,20
       pop       rbx
       pop       rsi
       pop       rdi
       ret
M17_L00:
       mov       rcx,7FFA3ED35BA8
       call      CORINFO_HELP_COUNTPROFILE32
       call      qword ptr [7FFA3ECD5F20]
       int       3
; Total bytes of code 82
```
```assembly
; System.Runtime.CompilerServices.CastHelpers.StelemRef(System.Object[], IntPtr, System.Object)
       sub       rsp,28
       mov       eax,[rcx+8]
       cmp       rax,rdx
       jbe       short M18_L03
       lea       r10,[rcx+rdx*8+10]
       mov       rdx,[rcx]
       mov       rdx,[rdx+30]
       test      r8,r8
       je        short M18_L02
       cmp       rdx,[r8]
       jne       short M18_L01
M18_L00:
       mov       rdx,r8
       mov       rcx,r10
       lea       rax,[System.Reflection.CustomAttributeExtensions.GetCustomAttribute[[System.__Canon, System.Private.CoreLib]](System.Reflection.Assembly)]
       add       rsp,28
       jmp       qword ptr [rax]
M18_L01:
       mov       rcx,[rcx]
       cmp       rcx,[System.Reflection.CustomAttributeExtensions.GetCustomAttribute[[System.__Canon, System.Private.CoreLib]](System.Reflection.Assembly)]
       je        short M18_L00
       mov       rcx,r10
       lea       rax,[System.Reflection.CustomAttributeExtensions.GetCustomAttribute[[System.__Canon, System.Private.CoreLib]](System.Reflection.Assembly)]
       add       rsp,28
       jmp       qword ptr [rax]
M18_L02:
       xor       eax,eax
       mov       [r10],rax
       add       rsp,28
       ret
M18_L03:
       call      qword ptr [7FFA960CBBF8]
       int       3
; Total bytes of code 100
```
```assembly
; System.Collections.Generic.List`1[[System.__Canon, System.Private.CoreLib]].AddWithResize(System.__Canon)
       push      rdi
       push      rsi
       push      rbp
       push      rbx
       sub       rsp,28
       mov       rbx,rcx
       mov       rsi,rdx
       mov       edi,[rbx+10]
       lea       ebp,[rdi+1]
       mov       ecx,ebp
       mov       rdx,[rbx+8]
       cmp       dword ptr [rdx+8],0
       jne       short M19_L01
       mov       edx,4
M19_L00:
       mov       eax,7FFFFFC7
       cmp       edx,7FFFFFC7
       cmova     edx,eax
       cmp       edx,ecx
       cmovl     edx,ecx
       mov       rcx,rbx
       call      qword ptr [7FFA960D5E98]; Precode of System.Collections.Generic.List`1[[System.__Canon, System.Private.CoreLib]].set_Capacity(Int32)
       mov       [rbx+10],ebp
       mov       rcx,[rbx+8]
       movsxd    rdx,edi
       mov       r8,rsi
       call      qword ptr [7FFA960B1110]; Precode of System.Runtime.CompilerServices.CastHelpers.StelemRef(System.Object[], IntPtr, System.Object)
       nop
       add       rsp,28
       pop       rbx
       pop       rbp
       pop       rsi
       pop       rdi
       ret
M19_L01:
       mov       rdx,[rbx+8]
       mov       edx,[rdx+8]
       add       edx,edx
       jmp       short M19_L00
; Total bytes of code 105
```
```assembly
; System.Collections.Generic.List`1[[System.__Canon, System.Private.CoreLib]]..ctor()
       push      rbx
       sub       rsp,30
       mov       [rsp+28],rcx
       mov       rbx,rcx
       mov       rcx,[rbx]
       call      qword ptr [7FFA960B4188]
       mov       rcx,rax
       call      qword ptr [7FFA960B11B0]; CORINFO_HELP_GET_GCSTATIC_BASE
       mov       rdx,[rax]
       lea       rcx,[rbx+8]
       call      qword ptr [7FFA960B10F0]; CORINFO_HELP_ASSIGN_REF
       nop
       add       rsp,30
       pop       rbx
       ret
; Total bytes of code 51
```
**Extern method**
System.Threading.Interlocked.CompareExchangeObject(System.Object ByRef, System.Object, System.Object)
System.Threading.Thread.get_OptimalMaxSpinWaitsPerSpinIteration()
System.Threading.Monitor.ReliableEnter(System.Object, Boolean ByRef)
System.Threading.Monitor.Exit(System.Object)
System.Environment.get_TickCount()
System.Threading.Thread.GetCurrentThreadNative()

## .NET 9.0.5 (9.0.525.21509), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
```assembly
; Benchmark.HsmBenchmarks.Stateless_Hsm_AsyncYield()
       sub       rsp,48
       xor       eax,eax
       mov       [rsp+28],rax
       vxorps    xmm4,xmm4,xmm4
       vmovdqa   xmmword ptr [rsp+30],xmm4
       mov       [rsp+40],rax
       mov       [rsp+28],rcx
       mov       dword ptr [rsp+30],0FFFFFFFF
       lea       rcx,[rsp+28]
       call      qword ptr [7FFA3EBF5D28]; System.Runtime.CompilerServices.AsyncMethodBuilderCore.Start[[Benchmark.HsmBenchmarks+<Stateless_Hsm_AsyncYield>d__14, Benchmark]](<Stateless_Hsm_AsyncYield>d__14 ByRef)
       mov       rax,[rsp+38]
       test      rax,rax
       je        short M00_L01
M00_L00:
       add       rsp,48
       ret
M00_L01:
       lea       rcx,[rsp+38]
       call      qword ptr [7FFA3EBFF978]
       jmp       short M00_L00
; Total bytes of code 78
```
```assembly
; BenchmarkDotNet.Helpers.AwaitHelper.GetResult(System.Threading.Tasks.Task)
       sub       rsp,28
       mov       edx,[rcx+34]
       and       edx,11000000
       cmp       edx,1000000
       jne       short M01_L01
M01_L00:
       add       rsp,28
       ret
M01_L01:
       xor       edx,edx
       call      qword ptr [7FFA3EBFC588]; System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(System.Threading.Tasks.Task, System.Threading.Tasks.ConfigureAwaitOptions)
       jmp       short M01_L00
; Total bytes of code 36
```
```assembly
; System.Runtime.CompilerServices.AsyncMethodBuilderCore.Start[[Benchmark.HsmBenchmarks+<Stateless_Hsm_AsyncYield>d__14, Benchmark]](<Stateless_Hsm_AsyncYield>d__14 ByRef)
       push      rbp
       push      rsi
       push      rbx
       sub       rsp,40
       lea       rbp,[rsp+50]
       mov       [rbp-30],rsp
       mov       rbx,rcx
       cmp       [rbx],bl
       mov       rax,gs:[58]
       mov       rax,[rax+48]
       cmp       dword ptr [rax+208],4
       jle       short M02_L04
       mov       rax,[rax+210]
       mov       rax,[rax+20]
       test      rax,rax
       je        short M02_L04
M02_L00:
       mov       rsi,[rax+10]
       test      rsi,rsi
       jne       short M02_L01
       call      qword ptr [7FFA3EA84C18]; System.Threading.Thread.InitializeCurrentThread()
       mov       rsi,rax
M02_L01:
       mov       [rbp-18],rsi
       mov       rdx,[rsi+8]
       mov       [rbp-20],rdx
       mov       rcx,[rsi+10]
       mov       [rbp-28],rcx
       mov       rcx,rbx
       call      qword ptr [7FFA3EBF5D58]; Benchmark.HsmBenchmarks+<Stateless_Hsm_AsyncYield>d__14.MoveNext()
       nop
       mov       rcx,[rbp-28]
       cmp       rcx,[rsi+10]
       jne       short M02_L05
M02_L02:
       mov       r8,[rsi+8]
       mov       rdx,[rbp-20]
       cmp       rdx,r8
       jne       short M02_L06
M02_L03:
       add       rsp,40
       pop       rbx
       pop       rsi
       pop       rbp
       ret
M02_L04:
       mov       ecx,4
       call      CORINFO_HELP_GETDYNAMIC_GCTHREADSTATIC_BASE_NOCTOR_OPTIMIZED
       jmp       short M02_L00
M02_L05:
       lea       rcx,[rsi+10]
       mov       rdx,[rbp-28]
       call      CORINFO_HELP_ASSIGN_REF
       jmp       short M02_L02
M02_L06:
       mov       rcx,rsi
       call      qword ptr [7FFA3EA8FC90]
       jmp       short M02_L03
       push      rbp
       push      rsi
       push      rbx
       sub       rsp,30
       mov       rbp,[rcx+20]
       mov       [rsp+20],rbp
       lea       rbp,[rbp+50]
       mov       rdx,[rbp-28]
       mov       rcx,[rbp-18]
       cmp       rdx,[rcx+10]
       je        short M02_L07
       lea       rcx,[rcx+10]
       call      CORINFO_HELP_ASSIGN_REF
       mov       rcx,[rbp-18]
M02_L07:
       mov       r8,[rcx+8]
       mov       rdx,[rbp-20]
       cmp       rdx,r8
       je        short M02_L08
       call      qword ptr [7FFA3EA8FC90]
M02_L08:
       nop
       add       rsp,30
       pop       rbx
       pop       rsi
       pop       rbp
       ret
; Total bytes of code 251
```
```assembly
; System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(System.Threading.Tasks.Task, System.Threading.Tasks.ConfigureAwaitOptions)
       push      rsi
       push      rbx
       sub       rsp,28
       mov       rbx,rcx
       mov       esi,edx
       test      dword ptr [rbx+34],1600000
       jne       short M03_L00
       mov       rcx,rbx
       xor       r8d,r8d
       mov       edx,0FFFFFFFF
       call      qword ptr [7FFA3EBFC5A0]; System.Threading.Tasks.Task.InternalWaitCore(Int32, System.Threading.CancellationToken)
M03_L00:
       test      dword ptr [rbx+34],10000000
       jne       short M03_L03
M03_L01:
       mov       ecx,[rbx+34]
       and       ecx,1600000
       cmp       ecx,1000000
       jne       short M03_L04
M03_L02:
       add       rsp,28
       pop       rbx
       pop       rsi
       ret
M03_L03:
       mov       rcx,rbx
       mov       rax,[rbx]
       mov       rax,[rax+40]
       call      qword ptr [rax+20]
       test      eax,eax
       je        short M03_L01
       mov       rcx,rbx
       call      qword ptr [7FFA3ECC6820]
       jmp       short M03_L01
M03_L04:
       test      sil,2
       jne       short M03_L05
       mov       rcx,rbx
       call      qword ptr [7FFA3ECC6868]
M03_L05:
       mov       rcx,rbx
       call      qword ptr [7FFA3ECC6FA0]
       jmp       short M03_L02
; Total bytes of code 124
```
```assembly
; System.Threading.Thread.InitializeCurrentThread()
       push      rbx
       sub       rsp,20
       call      qword ptr [7FFA960C8F58]; System.Threading.Thread.GetCurrentThreadNative()
       mov       rbx,rax
       call      qword ptr [7FFA960B2B68]
       lea       rcx,[rax+10]
       mov       rdx,rbx
       call      qword ptr [7FFA960B10F0]; CORINFO_HELP_ASSIGN_REF
       mov       rax,rbx
       add       rsp,20
       pop       rbx
       ret
; Total bytes of code 42
```
```assembly
; Benchmark.HsmBenchmarks+<Stateless_Hsm_AsyncYield>d__14.MoveNext()
       push      rbp
       push      rsi
       push      rbx
       sub       rsp,60
       lea       rbp,[rsp+70]
       vxorps    xmm4,xmm4,xmm4
       vmovdqu   ymmword ptr [rbp-40],ymm4
       vmovdqa   xmmword ptr [rbp-20],xmm4
       mov       [rbp-50],rsp
       mov       [rbp+10],rcx
       mov       edx,[rcx+8]
       mov       rbx,[rcx]
       test      edx,edx
       jne       near ptr M05_L04
       mov       rdx,[rcx+18]
       mov       [rbp-18],rdx
       xor       edx,edx
       mov       [rcx+18],rdx
       mov       dword ptr [rcx+8],0FFFFFFFF
M05_L00:
       mov       rax,[rbp-18]
       mov       edx,[rax+34]
       and       edx,11000000
       cmp       edx,1000000
       jne       near ptr M05_L06
M05_L01:
       inc       dword ptr [rcx+0C]
       cmp       dword ptr [rcx+0C],400
       jge       near ptr M05_L05
M05_L02:
       mov       rdx,[rbx+28]
       mov       rsi,[rdx+8]
       cmp       [rsi],sil
       mov       rcx,offset MT_System.Object[]
       xor       edx,edx
       call      CORINFO_HELP_NEWARR_1_OBJ
       vxorps    ymm0,ymm0,ymm0
       vmovdqu   ymmword ptr [rbp-40],ymm0
       vmovdqu   xmmword ptr [rbp-28],xmm0
       xor       ecx,ecx
       mov       [rbp-28],rcx
       mov       [rbp-40],rsi
       mov       dword ptr [rbp-2C],2
       mov       [rbp-38],rax
       mov       dword ptr [rbp-30],0FFFFFFFF
       lea       rcx,[rbp-40]
       call      qword ptr [7FFA3EBF5E30]; System.Runtime.CompilerServices.AsyncMethodBuilderCore.Start[[Stateless.StateMachine`2+<InternalFireAsync>d__21[[Benchmark.HsmState, Benchmark],[Benchmark.HsmTrigger, Benchmark]], Stateless]](<InternalFireAsync>d__21<Benchmark.HsmState,Benchmark.HsmTrigger> ByRef)
       mov       rcx,[rbp-28]
       test      rcx,rcx
       je        near ptr M05_L07
M05_L03:
       cmp       [rcx],cl
       mov       [rbp-18],rcx
       mov       rcx,[rbp-18]
       test      dword ptr [rcx+34],1600000
       mov       rcx,[rbp+10]
       jne       near ptr M05_L00
       xor       edx,edx
       mov       [rcx+8],edx
       lea       rcx,[rcx+18]
       mov       rdx,[rbp-18]
       call      CORINFO_HELP_CHECKED_ASSIGN_REF
       mov       rcx,[rbp+10]
       lea       rdx,[rcx+10]
       call      qword ptr [7FFA3EBFC4F8]; System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1[[System.Threading.Tasks.VoidTaskResult, System.Private.CoreLib]].GetStateMachineBox[[Benchmark.HsmBenchmarks+<Stateless_Hsm_AsyncYield>d__14, Benchmark]](<Stateless_Hsm_AsyncYield>d__14 ByRef, System.Threading.Tasks.Task`1<System.Threading.Tasks.VoidTaskResult> ByRef)
       mov       rdx,rax
       lea       rcx,[rbp-18]
       call      qword ptr [7FFA3EBFC060]; System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1[[System.Threading.Tasks.VoidTaskResult, System.Private.CoreLib]].AwaitUnsafeOnCompleted[[System.Runtime.CompilerServices.TaskAwaiter, System.Private.CoreLib]](System.Runtime.CompilerServices.TaskAwaiter ByRef, System.Runtime.CompilerServices.IAsyncStateMachineBox)
       jmp       short M05_L09
M05_L04:
       xor       edx,edx
       mov       [rcx+0C],edx
       cmp       dword ptr [rcx+0C],400
       jl        near ptr M05_L02
M05_L05:
       mov       rdx,[rbx+28]
       mov       rdx,[rdx+8]
       mov       rcx,7FFA3EC39188
       call      qword ptr [7FFA3EBF5D88]; BenchmarkDotNet.Engines.DeadCodeEliminationHelper.KeepAliveWithoutBoxing[[System.__Canon, System.Private.CoreLib]](System.__Canon)
       jmp       short M05_L08
M05_L06:
       mov       rcx,rax
       xor       edx,edx
       call      qword ptr [7FFA3EBFC588]; System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(System.Threading.Tasks.Task, System.Threading.Tasks.ConfigureAwaitOptions)
       mov       rcx,[rbp+10]
       jmp       near ptr M05_L01
M05_L07:
       lea       rcx,[rbp-28]
       call      qword ptr [7FFA3EBFF978]
       mov       rcx,rax
       jmp       near ptr M05_L03
M05_L08:
       mov       rcx,[rbp+10]
       mov       dword ptr [rcx+8],0FFFFFFFE
       add       rcx,10
       call      qword ptr [7FFA3EA8FF78]; System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()
M05_L09:
       nop
       add       rsp,60
       pop       rbx
       pop       rsi
       pop       rbp
       ret
       push      rbp
       push      rsi
       push      rbx
       sub       rsp,30
       mov       rbp,[rcx+20]
       mov       [rsp+20],rbp
       lea       rbp,[rbp+70]
       mov       rcx,[rbp+10]
       mov       dword ptr [rcx+8],0FFFFFFFE
       add       rcx,10
       call      qword ptr [7FFA3EA8FF90]
       lea       rax,[M05_L09]
       add       rsp,30
       pop       rbx
       pop       rsi
       pop       rbp
       ret
; Total bytes of code 445
```
```assembly
; System.Threading.Tasks.Task.InternalWaitCore(Int32, System.Threading.CancellationToken)
       push      r15
       push      r14
       push      r13
       push      r12
       push      rdi
       push      rsi
       push      rbp
       push      rbx
       sub       rsp,38
       mov       rbx,rcx
       mov       esi,edx
       mov       rdi,r8
       test      dword ptr [rbx+34],1600000
       jne       near ptr M06_L13
       mov       rax,20692801480
       mov       rbp,[rax]
       movzx     r14d,byte ptr [rbp+9D]
       test      r14d,r14d
       jne       short M06_L05
M06_L00:
       call      System.Diagnostics.Debugger.get_IsAttached()
       test      eax,eax
       jne       near ptr M06_L08
M06_L01:
       cmp       esi,0FFFFFFFF
       jne       short M06_L02
       test      rdi,rdi
       jne       short M06_L02
       mov       rcx,rbx
       call      qword ptr [7FFA3EBFC5B8]; System.Threading.Tasks.Task.WrappedTryRunInline()
       test      eax,eax
       jne       near ptr M06_L09
M06_L02:
       mov       rcx,rbx
       mov       edx,esi
       mov       r8,rdi
       call      qword ptr [7FFA3EBFC5D0]; System.Threading.Tasks.Task.SpinThenBlockingWait(Int32, System.Threading.CancellationToken)
       mov       r15d,eax
M06_L03:
       test      r14d,r14d
       jne       near ptr M06_L10
M06_L04:
       mov       eax,r15d
       add       rsp,38
       pop       rbx
       pop       rbp
       pop       rsi
       pop       rdi
       pop       r12
       pop       r13
       pop       r14
       pop       r15
       ret
M06_L05:
       mov       ecx,5
       call      CORINFO_HELP_GETDYNAMIC_GCTHREADSTATIC_BASE_NOCTOR_OPTIMIZED
       mov       r15,[rax+10]
       test      r15,r15
       jne       short M06_L06
       mov       rcx,206928014D8
       mov       rcx,[rcx]
       call      qword ptr [7FFA3EBF6DD8]; System.Threading.Tasks.TaskScheduler.get_Id()
       mov       r13d,eax
       xor       r12d,r12d
       jmp       short M06_L07
M06_L06:
       mov       rcx,[r15+18]
       cmp       [rcx],ecx
       call      qword ptr [7FFA3EBF6DD8]; System.Threading.Tasks.TaskScheduler.get_Id()
       mov       r13d,eax
       mov       rcx,r15
       call      qword ptr [7FFA3EBF7D50]
       mov       r12d,eax
M06_L07:
       mov       rcx,rbx
       call      qword ptr [7FFA3EBF7D50]
       mov       r9d,eax
       mov       dword ptr [rsp+20],1
       xor       edx,edx
       mov       [rsp+28],edx
       mov       edx,r13d
       mov       r8d,r12d
       mov       rcx,rbp
       call      qword ptr [7FFA3ECC6898]
       jmp       near ptr M06_L00
M06_L08:
       call      qword ptr [7FFA3EBFF9A8]
       jmp       near ptr M06_L01
M06_L09:
       test      dword ptr [rbx+34],1600000
       je        near ptr M06_L02
       mov       r15d,1
       jmp       near ptr M06_L03
M06_L10:
       mov       ecx,5
       call      CORINFO_HELP_GETDYNAMIC_GCTHREADSTATIC_BASE_NOCTOR_OPTIMIZED
       mov       rsi,[rax+10]
       test      rsi,rsi
       je        short M06_L11
       mov       rcx,[rsi+18]
       cmp       [rcx],ecx
       call      qword ptr [7FFA3EBF6DD8]; System.Threading.Tasks.TaskScheduler.get_Id()
       mov       edi,eax
       mov       rcx,rsi
       call      qword ptr [7FFA3EBF7D50]
       mov       esi,eax
       mov       rcx,rbx
       call      qword ptr [7FFA3EBF7D50]
       mov       r9d,eax
       mov       edx,edi
       mov       r8d,esi
       mov       rcx,rbp
       call      qword ptr [7FFA3ECC68B0]
       jmp       short M06_L12
M06_L11:
       mov       rcx,206928014D8
       mov       rcx,[rcx]
       call      qword ptr [7FFA3EBF6DD8]; System.Threading.Tasks.TaskScheduler.get_Id()
       mov       esi,eax
       mov       rcx,rbx
       call      qword ptr [7FFA3EBF7D50]
       mov       r9d,eax
       mov       edx,esi
       mov       rcx,rbp
       xor       r8d,r8d
       call      qword ptr [7FFA3ECC68B0]
M06_L12:
       mov       rcx,rbx
       call      qword ptr [7FFA3EBF7D50]
       mov       edx,eax
       mov       rcx,rbp
       call      qword ptr [7FFA3ECC68C8]
       jmp       near ptr M06_L04
M06_L13:
       mov       eax,1
       add       rsp,38
       pop       rbx
       pop       rbp
       pop       rsi
       pop       rdi
       pop       r12
       pop       r13
       pop       r14
       pop       r15
       ret
; Total bytes of code 469
```

## .NET 9.0.5 (9.0.525.21509), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
```assembly
; Benchmark.HsmBenchmarks.FastFSM_Hsm_Basic_EnterLeave()
       push      rdi
       push      rsi
       push      rbx
       sub       rsp,20
       mov       rbx,rcx
       mov       rsi,offset MT_Benchmark.FastFsmHsmBasic
       mov       edi,400
M00_L00:
       mov       rcx,[rbx+8]
       cmp       [rcx],rsi
       jne       near ptr M00_L04
       cmp       byte ptr [rcx+14],0
       je        near ptr M00_L03
       xor       edx,edx
       xor       r8d,r8d
       call      qword ptr [7FFA3EB18E30]; Benchmark.FastFsmHsmBasic.TryFireInternal(Benchmark.HsmTrigger, System.Object)
       mov       rcx,[rbx+8]
       cmp       [rcx],rsi
       jne       near ptr M00_L05
       cmp       byte ptr [rcx+14],0
       je        short M00_L02
       mov       edx,1
       xor       r8d,r8d
       call      qword ptr [7FFA3EB18E30]; Benchmark.FastFsmHsmBasic.TryFireInternal(Benchmark.HsmTrigger, System.Object)
M00_L01:
       dec       edi
       jne       short M00_L00
       mov       rcx,[rbx+8]
       mov       ecx,[rcx+10]
       add       rsp,20
       pop       rbx
       pop       rsi
       pop       rdi
       jmp       qword ptr [7FFA3EC24F60]; BenchmarkDotNet.Engines.DeadCodeEliminationHelper.KeepAliveWithoutBoxing[[Benchmark.HsmState, Benchmark]](Benchmark.HsmState)
M00_L02:
       call      System.Object.GetType()
       mov       rbx,rax
       mov       rcx,offset MT_System.InvalidOperationException
       call      CORINFO_HELP_NEWSFAST
       mov       rsi,rax
       mov       rcx,rbx
       xor       edx,edx
       call      qword ptr [7FFA3EB277E0]; System.RuntimeType.GetCachedName(System.TypeNameKind)
       mov       rdi,rax
       mov       ecx,166
       mov       rdx,7FFA3EB17150
       call      CORINFO_HELP_STRCNS
       mov       rdx,rax
       mov       rcx,rdi
       call      qword ptr [7FFA3E77D788]; System.String.Concat(System.String, System.String)
       mov       rdx,rax
       mov       rcx,rsi
       call      qword ptr [7FFA3EA96CA0]
       mov       rcx,rsi
       call      CORINFO_HELP_THROW
       int       3
M00_L03:
       call      System.Object.GetType()
       mov       rbx,rax
       mov       rcx,offset MT_System.InvalidOperationException
       call      CORINFO_HELP_NEWSFAST
       mov       rsi,rax
       mov       rcx,rbx
       xor       edx,edx
       call      qword ptr [7FFA3EB277E0]; System.RuntimeType.GetCachedName(System.TypeNameKind)
       mov       rbx,rax
       mov       ecx,166
       mov       rdx,7FFA3EB17150
       call      CORINFO_HELP_STRCNS
       mov       rdx,rax
       mov       rcx,rbx
       call      qword ptr [7FFA3E77D788]; System.String.Concat(System.String, System.String)
       mov       rdx,rax
       mov       rcx,rsi
       call      qword ptr [7FFA3EA96CA0]
       mov       rcx,rsi
       call      CORINFO_HELP_THROW
       int       3
M00_L04:
       xor       edx,edx
       xor       r8d,r8d
       mov       rax,[rcx]
       mov       rax,[rax+48]
       call      qword ptr [rax]
       mov       rcx,[rbx+8]
M00_L05:
       mov       edx,1
       xor       r8d,r8d
       mov       rax,[rcx]
       mov       rax,[rax+48]
       call      qword ptr [rax]
       jmp       near ptr M00_L01
; Total bytes of code 342
```
```assembly
; Benchmark.FastFsmHsmBasic.TryFireInternal(Benchmark.HsmTrigger, System.Object)
       push      rbp
       push      r15
       push      r14
       push      r13
       push      r12
       push      rdi
       push      rsi
       push      rbx
       sub       rsp,38
       lea       rbp,[rsp+20]
       mov       [rbp],rsp
       mov       rax,10FBC432BCD0
       mov       [rbp+8],rax
       mov       rbx,rcx
M01_L00:
       xor       esi,esi
       mov       r8d,80000000
       mov       r10d,7FFFFFFF
       mov       r9d,7FFFFFFF
       xor       edi,edi
       mov       ecx,0FFFFFFFF
       xor       r14d,r14d
       mov       [rbp+14],r14d
       xor       r15d,r15d
       mov       r13d,[rbx+10]
       mov       [rbp+10],r13d
       mov       r11d,r13d
       test      r13d,r13d
       jl        near ptr M01_L26
M01_L01:
       cmp       r11d,r13d
       je        short M01_L03
       mov       rax,1E3D1800900
       mov       r12,[rax]
       mov       rax,r12
       cmp       r13d,5
       jae       near ptr M01_L63
       mov       r14d,r13d
       mov       eax,[rax+r14*4+10]
       cmp       r11d,5
       jae       near ptr M01_L63
       mov       r14d,r11d
       sub       eax,[r12+r14*4+10]
M01_L02:
       cmp       r11d,3
       ja        near ptr M01_L24
       mov       r12d,r11d
       lea       r13,[7FFA3E82E8B8]
       mov       r13d,[r13+r12*4]
       lea       r14,[M01_L00]
       add       r13,r14
       jmp       r13
M01_L03:
       xor       eax,eax
       jmp       short M01_L02
       cmp       edx,2
       mov       r13d,[rbp+10]
       jne       near ptr M01_L24
       test      esi,esi
       jne       short M01_L05
M01_L04:
       mov       esi,1
       xor       r8d,r8d
       mov       r10d,eax
       mov       r9d,r15d
       xor       edi,edi
       mov       ecx,2
       jmp       near ptr M01_L22
M01_L05:
       test      r8d,r8d
       jl        short M01_L04
       test      r8d,r8d
       jne       short M01_L07
       cmp       eax,r10d
       jl        short M01_L04
       cmp       eax,r10d
       jne       short M01_L06
       cmp       r15d,r9d
       mov       r14d,[rbp+14]
       jge       near ptr M01_L23
       jmp       short M01_L04
M01_L06:
       mov       r14d,[rbp+14]
       jmp       near ptr M01_L23
M01_L07:
       mov       r14d,[rbp+14]
       jmp       near ptr M01_L23
       cmp       edx,2
       mov       r13d,[rbp+10]
       jne       near ptr M01_L24
       test      esi,esi
       jne       short M01_L09
M01_L08:
       mov       esi,1
       xor       r8d,r8d
       mov       r10d,eax
       mov       r9d,r15d
       xor       edi,edi
       mov       ecx,3
       jmp       near ptr M01_L22
M01_L09:
       test      r8d,r8d
       jl        short M01_L08
       test      r8d,r8d
       jne       short M01_L11
       cmp       eax,r10d
       jl        short M01_L08
       cmp       eax,r10d
       jne       short M01_L10
       cmp       r15d,r9d
       mov       r14d,[rbp+14]
       jge       near ptr M01_L23
       jmp       short M01_L08
M01_L10:
       mov       r14d,[rbp+14]
       jmp       near ptr M01_L23
M01_L11:
       mov       r14d,[rbp+14]
       jmp       near ptr M01_L23
       cmp       edx,1
       je        short M01_L16
       cmp       edx,3
       mov       r13d,[rbp+10]
       jne       near ptr M01_L24
       test      esi,esi
       jne       short M01_L13
M01_L12:
       mov       esi,1
       xor       r8d,r8d
       mov       r10d,eax
       mov       r9d,r15d
       mov       edi,1
       mov       r14d,1
       jmp       near ptr M01_L23
M01_L13:
       test      r8d,r8d
       jl        short M01_L12
       test      r8d,r8d
       jne       short M01_L15
       cmp       eax,r10d
       jl        short M01_L12
       cmp       eax,r10d
       jne       short M01_L14
       cmp       r15d,r9d
       mov       r14d,[rbp+14]
       jge       near ptr M01_L23
       jmp       short M01_L12
M01_L14:
       mov       r14d,[rbp+14]
       jmp       near ptr M01_L23
M01_L15:
       mov       r14d,[rbp+14]
       jmp       short M01_L23
M01_L16:
       test      esi,esi
       jne       short M01_L18
M01_L17:
       mov       esi,1
       xor       r8d,r8d
       mov       r10d,eax
       mov       r9d,r15d
       xor       edi,edi
       xor       ecx,ecx
       mov       r13d,[rbp+10]
       jmp       short M01_L22
M01_L18:
       test      r8d,r8d
       jl        short M01_L17
       test      r8d,r8d
       jne       short M01_L20
       cmp       eax,r10d
       jl        short M01_L17
       cmp       eax,r10d
       jne       short M01_L19
       cmp       r15d,r9d
       mov       r14d,[rbp+14]
       jl        short M01_L17
       mov       r13d,[rbp+10]
       jmp       short M01_L23
M01_L19:
       mov       r13d,[rbp+10]
       mov       r14d,[rbp+14]
       jmp       short M01_L23
M01_L20:
       mov       r13d,[rbp+10]
       mov       r14d,[rbp+14]
       jmp       short M01_L23
       test      edx,edx
       mov       r13d,[rbp+10]
       jne       short M01_L24
       test      esi,esi
       jne       short M01_L27
M01_L21:
       mov       esi,1
       xor       r8d,r8d
       mov       r10d,eax
       mov       r9d,r15d
       xor       edi,edi
       mov       ecx,1
M01_L22:
       xor       r14d,r14d
M01_L23:
       inc       r15d
       mov       [rbp+14],r14d
M01_L24:
       mov       rax,1E3D18008F8
       mov       r12,[rax]
       mov       eax,5
       cmp       eax,r11d
       ja        short M01_L28
       mov       r11d,0FFFFFFFF
M01_L25:
       test      r11d,r11d
       jge       near ptr M01_L01
M01_L26:
       test      esi,esi
       je        short M01_L32
       test      edi,edi
       je        near ptr M01_L34
       mov       r14d,[rbp+14]
       test      r14d,r14d
       je        short M01_L30
       cmp       r14d,1
       jne       short M01_L30
       jmp       short M01_L29
M01_L27:
       test      r8d,r8d
       jl        short M01_L21
       test      r8d,r8d
       mov       r14d,[rbp+14]
       jne       short M01_L23
       cmp       eax,r10d
       mov       [rbp+14],r14d
       jl        short M01_L21
       cmp       eax,r10d
       mov       r14d,[rbp+14]
       jne       short M01_L23
       cmp       r15d,r9d
       jge       short M01_L23
       jmp       near ptr M01_L21
M01_L28:
       mov       eax,r11d
       mov       r11d,[r12+rax*4+10]
       jmp       short M01_L25
M01_L29:
       jmp       short M01_L30
M01_L30:
       mov       eax,1
       mov       rcx,10FBC432BCD0
       cmp       [rbp+8],rcx
       je        short M01_L31
       call      CORINFO_HELP_FAIL_FAST
M01_L31:
       nop
       lea       rsp,[rbp+18]
       pop       rbx
       pop       rsi
       pop       rdi
       pop       r12
       pop       r13
       pop       r14
       pop       r15
       pop       rbp
       ret
M01_L32:
       xor       eax,eax
       mov       rcx,10FBC432BCD0
       cmp       [rbp+8],rcx
       je        short M01_L33
       call      CORINFO_HELP_FAIL_FAST
M01_L33:
       nop
       lea       rsp,[rbp+18]
       pop       rbx
       pop       rsi
       pop       rdi
       pop       r12
       pop       r13
       pop       r14
       pop       r15
       pop       rbp
       ret
M01_L34:
       mov       edx,[rbx+10]
       cmp       edx,ecx
       jne       near ptr M01_L42
       mov       esi,edx
M01_L35:
       mov       eax,[rbx+10]
       mov       r8,1E3D18008F8
       mov       r12,[r8]
       mov       r8d,5
       cmp       r8d,eax
       ja        near ptr M01_L49
       mov       r8d,0FFFFFFFF
M01_L36:
       test      r8d,r8d
       jl        short M01_L39
       mov       r10,1E3D1800910
       mov       r10,[r10]
M01_L37:
       mov       r9,r10
       cmp       r8d,5
       jae       near ptr M01_L63
       mov       r11d,r8d
       cmp       dword ptr [r9+r11*4+10],0
       je        short M01_L38
       mov       r9,[rbx+18]
       cmp       r8d,[r9+8]
       jae       near ptr M01_L63
       mov       r11d,r8d
       mov       [r9+r11*4+10],eax
M01_L38:
       mov       r9,r12
       mov       r8d,r8d
       mov       r8d,[r9+r8*4+10]
       test      r8d,r8d
       jge       short M01_L37
M01_L39:
       jmp       short M01_L41
M01_L40:
       mov       rax,r12
       cmp       edx,5
       jae       near ptr M01_L63
       mov       edx,edx
       mov       edx,[rax+rdx*4+10]
M01_L41:
       cmp       edx,esi
       je        near ptr M01_L50
       test      edx,edx
       jge       short M01_L40
       jmp       near ptr M01_L50
M01_L42:
       mov       esi,edx
       mov       eax,ecx
       mov       r8,1E3D1800900
       mov       r12,[r8]
       jmp       short M01_L44
M01_L43:
       mov       r8,1E3D18008F8
       mov       r8,[r8]
       mov       r15,r8
       mov       r8,r15
       mov       r10d,esi
       mov       esi,[r8+r10*4+10]
M01_L44:
       mov       r8,r12
       cmp       esi,5
       jae       near ptr M01_L63
       mov       r10d,esi
       mov       r8d,[r8+r10*4+10]
       mov       r10,r12
       cmp       ecx,5
       jae       near ptr M01_L63
       mov       r9d,ecx
       cmp       r8d,[r10+r9*4+10]
       jg        short M01_L43
       jmp       short M01_L46
M01_L45:
       mov       r10,1E3D18008F8
       mov       r15,[r10]
       mov       r10,r15
       mov       eax,eax
       mov       eax,[r10+rax*4+10]
M01_L46:
       mov       r10,r12
       cmp       eax,5
       jae       near ptr M01_L63
       mov       r9d,eax
       cmp       [r10+r9*4+10],r8d
       jg        short M01_L45
       jmp       short M01_L48
M01_L47:
       mov       r8,1E3D18008F8
       mov       r15,[r8]
       mov       r8,r15
       cmp       esi,5
       jae       near ptr M01_L63
       mov       r10d,esi
       mov       esi,[r8+r10*4+10]
       mov       r8,r15
       cmp       eax,5
       jae       near ptr M01_L63
       mov       eax,eax
       mov       eax,[r8+rax*4+10]
M01_L48:
       cmp       esi,eax
       jne       short M01_L47
       jmp       near ptr M01_L35
M01_L49:
       mov       r8,r12
       mov       r10d,eax
       mov       r8d,[r8+r10*4+10]
       jmp       near ptr M01_L36
M01_L50:
       mov       [rbx+10],ecx
       mov       edx,[rbx+10]
       mov       rcx,rbx
       call      qword ptr [7FFA3EB24258]; Benchmark.FastFsmHsmBasic.GetCompositeEntryTarget(Int32)
       mov       [rbx+10],eax
       xor       ecx,ecx
       mov       rax,1E3D1800900
       mov       rax,[rax]
       mov       r8d,[rbx+10]
       mov       edx,r8d
       cmp       edx,5
       jae       near ptr M01_L63
       mov       r10d,[rax+rdx*4+10]
       inc       r10d
       mov       eax,r10d
       mov       r9d,4
       mul       r9
       jb        near ptr M01_L62
       test      rax,rax
       je        short M01_L52
       add       rax,0F
       shr       rax,4
       add       rsp,20
M01_L51:
       push      0
       push      0
       dec       rax
       jne       short M01_L51
       sub       rsp,20
       lea       rax,[rsp+20]
M01_L52:
       test      r10d,r10d
       jl        short M01_L59
       jmp       short M01_L55
M01_L53:
       mov       rcx,r12
       mov       r8d,r8d
       mov       r8d,[rcx+r8*4+10]
M01_L54:
       mov       ecx,edx
M01_L55:
       test      r8d,r8d
       jl        short M01_L56
       cmp       r8d,esi
       je        short M01_L56
       lea       edx,[rcx+1]
       cmp       ecx,r10d
       jae       short M01_L63
       mov       ecx,ecx
       mov       [rax+rcx*4],r8d
       cmp       r8d,5
       jl        short M01_L53
       mov       r8d,0FFFFFFFF
       jmp       short M01_L54
M01_L56:
       dec       ecx
       js        short M01_L58
M01_L57:
       cmp       ecx,r10d
       jae       short M01_L63
       mov       edx,[rax+rcx*4]
       dec       ecx
       jns       short M01_L57
M01_L58:
       mov       r14d,[rbp+14]
       test      r14d,r14d
       je        near ptr M01_L30
       cmp       r14d,1
       jne       near ptr M01_L30
       jmp       near ptr M01_L30
M01_L59:
       call      qword ptr [7FFA3E987798]
       int       3
M01_L60:
       xor       eax,eax
       mov       rcx,10FBC432BCD0
       cmp       [rbp+8],rcx
       je        short M01_L61
       call      CORINFO_HELP_FAIL_FAST
M01_L61:
       nop
       lea       rsp,[rbp+18]
       pop       rbx
       pop       rsi
       pop       rdi
       pop       r12
       pop       r13
       pop       r14
       pop       r15
       pop       rbp
       ret
M01_L62:
       call      CORINFO_HELP_OVERFLOW
       int       3
M01_L63:
       call      CORINFO_HELP_RNGCHKFAIL
       int       3
       push      rbp
       push      r15
       push      r14
       push      r13
       push      r12
       push      rdi
       push      rsi
       push      rbx
       sub       rsp,28
       mov       rbp,[rcx+20]
       mov       [rsp+20],rbp
       lea       rbp,[rbp+20]
       lea       rax,[M01_L60]
       add       rsp,28
       pop       rbx
       pop       rsi
       pop       rdi
       pop       r12
       pop       r13
       pop       r14
       pop       r15
       pop       rbp
       ret
       push      rbp
       push      r15
       push      r14
       push      r13
       push      r12
       push      rdi
       push      rsi
       push      rbx
       sub       rsp,28
       mov       rbp,[rcx+20]
       mov       [rsp+20],rbp
       lea       rbp,[rbp+20]
       lea       rax,[M01_L30]
       add       rsp,28
       pop       rbx
       pop       rsi
       pop       rdi
       pop       r12
       pop       r13
       pop       r14
       pop       r15
       pop       rbp
       ret
; Total bytes of code 1583
```
```assembly
; BenchmarkDotNet.Engines.DeadCodeEliminationHelper.KeepAliveWithoutBoxing[[Benchmark.HsmState, Benchmark]](Benchmark.HsmState)
       ret
; Total bytes of code 1
```
```assembly
; System.RuntimeType.GetCachedName(System.TypeNameKind)
       push      rbx
       sub       rsp,20
       mov       ebx,edx
       mov       rax,[rcx+10]
       test      rax,rax
       je        short M03_L01
       mov       rax,[rax]
       test      rax,rax
       je        short M03_L01
M03_L00:
       mov       rcx,rax
       mov       edx,ebx
       cmp       [rcx],ecx
       call      qword ptr [7FFA960C2D08]; Precode of System.RuntimeType+RuntimeTypeCache.GetName(System.TypeNameKind)
       nop
       add       rsp,20
       pop       rbx
       ret
M03_L01:
       call      qword ptr [7FFA960C29E8]; Precode of System.RuntimeType.InitializeCache()
       jmp       short M03_L00
; Total bytes of code 52
```
```assembly
; System.String.Concat(System.String, System.String)
       push      r15
       push      r14
       push      rdi
       push      rsi
       push      rbp
       push      rbx
       sub       rsp,28
       mov       rsi,rcx
       mov       rbx,rdx
       test      rsi,rsi
       je        near ptr M04_L02
       mov       edi,[rsi+8]
       test      edi,edi
       je        short M04_L02
       test      rbx,rbx
       je        short M04_L01
       mov       ebp,[rbx+8]
       test      ebp,ebp
       je        short M04_L01
       mov       r14d,edi
       lea       ecx,[r14+rbp]
       test      ecx,ecx
       jl        short M04_L00
       call      00007FFA3E7724D8
       mov       r15,rax
       cmp       [r15],r15b
       lea       rcx,[r15+0C]
       mov       r8d,edi
       add       r8,r8
       lea       rdx,[rsi+0C]
       call      qword ptr [7FFA3E7757B8]; System.SpanHelpers.Memmove(Byte ByRef, Byte ByRef, UIntPtr)
       movsxd    rcx,r14d
       lea       rcx,[r15+rcx*2+0C]
       mov       r8d,ebp
       add       r8,r8
       lea       rdx,[rbx+0C]
       call      qword ptr [7FFA3E7757B8]; System.SpanHelpers.Memmove(Byte ByRef, Byte ByRef, UIntPtr)
       mov       rax,r15
       add       rsp,28
       pop       rbx
       pop       rbp
       pop       rsi
       pop       rdi
       pop       r14
       pop       r15
       ret
M04_L00:
       call      qword ptr [7FFA3EC2C5B8]
       int       3
M04_L01:
       mov       rax,rsi
       add       rsp,28
       pop       rbx
       pop       rbp
       pop       rsi
       pop       rdi
       pop       r14
       pop       r15
       ret
M04_L02:
       test      rbx,rbx
       je        short M04_L03
       mov       ebp,[rbx+8]
       test      ebp,ebp
       sete      al
       movzx     eax,al
       test      eax,eax
       jne       short M04_L03
       mov       rax,rbx
       add       rsp,28
       pop       rbx
       pop       rbp
       pop       rsi
       pop       rdi
       pop       r14
       pop       r15
       ret
M04_L03:
       mov       rax,1E380600008
       add       rsp,28
       pop       rbx
       pop       rbp
       pop       rsi
       pop       rdi
       pop       r14
       pop       r15
       ret
; Total bytes of code 210
```
```assembly
; Benchmark.FastFsmHsmBasic.GetCompositeEntryTarget(Int32)
       sub       rsp,28
       mov       rax,1E3D1800908
       mov       r8,[rax]
M05_L00:
       cmp       edx,5
       jae       short M05_L01
       mov       rax,r8
       mov       r10d,edx
       mov       r10d,[rax+r10*4+10]
       test      r10d,r10d
       jge       short M05_L02
M05_L01:
       mov       eax,edx
       add       rsp,28
       ret
M05_L02:
       mov       rax,1E3D1800910
       mov       rax,[rax]
       mov       r9d,edx
       mov       eax,[rax+r9*4+10]
       test      eax,eax
       jne       short M05_L04
M05_L03:
       test      r10d,r10d
       jl        short M05_L01
       mov       edx,r10d
       jmp       short M05_L00
M05_L04:
       mov       r9,[rcx+18]
       cmp       edx,[r9+8]
       jae       short M05_L10
       mov       r11d,edx
       cmp       dword ptr [r9+r11*4+10],0
       jl        short M05_L03
       mov       r9,[rcx+18]
       cmp       edx,[r9+8]
       jae       short M05_L10
       mov       r11d,edx
       mov       r9d,[r9+r11*4+10]
       cmp       eax,1
       jne       short M05_L09
M05_L05:
       test      r9d,r9d
       jl        short M05_L07
       mov       rax,1E3D18008F8
       mov       rax,[rax]
       cmp       r9d,5
       jae       short M05_L10
       mov       r11d,r9d
       cmp       [rax+r11*4+10],edx
       je        short M05_L06
       mov       rax,1E3D18008F8
       mov       rax,[rax]
       mov       r9d,r9d
       mov       r9d,[rax+r9*4+10]
       jmp       short M05_L05
M05_L06:
       jmp       short M05_L08
M05_L07:
       mov       r9d,r10d
M05_L08:
       mov       r10d,r9d
       jmp       short M05_L03
M05_L09:
       mov       r10d,r9d
       jmp       short M05_L03
M05_L10:
       call      CORINFO_HELP_RNGCHKFAIL
       int       3
; Total bytes of code 202
```
```assembly
; System.SpanHelpers.Memmove(Byte ByRef, Byte ByRef, UIntPtr)
       mov       rax,rcx
       sub       rax,rdx
       cmp       rax,r8
       jb        near ptr M06_L12
       mov       rax,rdx
       sub       rax,rcx
       cmp       rax,r8
       jb        near ptr M06_L12
       lea       rax,[rdx+r8]
       lea       r10,[rcx+r8]
       cmp       r8,10
       ja        short M06_L05
       test      r8b,18
       jne       short M06_L00
       test      r8b,4
       je        short M06_L04
       mov       edx,[rdx]
       mov       [rcx],edx
       mov       ecx,[rax-4]
       mov       [r10-4],ecx
       jmp       short M06_L03
M06_L00:
       mov       r8,[rdx]
       mov       [rcx],r8
       mov       rax,[rax-8]
       mov       [r10-8],rax
       jmp       short M06_L03
M06_L01:
       vmovups   xmm0,[rdx+20]
       vmovups   [rcx+20],xmm0
M06_L02:
       vmovups   xmm0,[rax-10]
       vmovups   [r10-10],xmm0
M06_L03:
       vzeroupper
       ret
M06_L04:
       test      r8,r8
       je        short M06_L03
       movzx     edx,byte ptr [rdx]
       mov       [rcx],dl
       test      r8b,2
       je        short M06_L03
       movsx     r8,word ptr [rax-2]
       mov       [r10-2],r8w
       jmp       short M06_L03
M06_L05:
       cmp       r8,40
       ja        short M06_L07
M06_L06:
       vmovups   xmm0,[rdx]
       vmovups   [rcx],xmm0
       cmp       r8,20
       jbe       short M06_L02
       jmp       short M06_L10
M06_L07:
       cmp       r8,800
       ja        near ptr M06_L13
       cmp       r8,100
       jae       short M06_L11
M06_L08:
       mov       r9,r8
       shr       r9,6
M06_L09:
       vmovdqu32 zmm0,[rdx]
       vmovdqu32 [rcx],zmm0
       add       rcx,40
       add       rdx,40
       dec       r9
       jne       short M06_L09
       and       r8,3F
       cmp       r8,10
       ja        short M06_L06
       jmp       near ptr M06_L02
M06_L10:
       vmovups   xmm0,[rdx+10]
       vmovups   [rcx+10],xmm0
       cmp       r8,30
       jbe       near ptr M06_L02
       jmp       near ptr M06_L01
M06_L11:
       mov       r9,rcx
       and       r9,3F
       neg       r9
       add       r9,40
       vmovdqu32 zmm0,[rdx]
       vmovdqu32 [rcx],zmm0
       add       rdx,r9
       add       rcx,r9
       sub       r8,r9
       jmp       short M06_L08
M06_L12:
       cmp       rcx,rdx
       jne       short M06_L13
       cmp       [rdx],dl
       jmp       near ptr M06_L03
M06_L13:
       cmp       [rcx],cl
       cmp       [rdx],dl
       vzeroupper
       jmp       qword ptr [7FFA3E776538]; System.Buffer._Memmove(Byte ByRef, Byte ByRef, UIntPtr)
; Total bytes of code 316
```
**Extern method**
System.Object.GetType()

## .NET 9.0.5 (9.0.525.21509), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
```assembly
; Benchmark.HsmBenchmarks.Stateless_Hsm_Basic_EnterLeave()
       push      r14
       push      rdi
       push      rsi
       push      rbp
       push      rbx
       sub       rsp,20
       mov       rbx,rcx
       mov       esi,400
M00_L00:
       mov       rcx,[rbx+20]
       mov       rdi,[rcx+8]
       cmp       [rdi],dil
       mov       rbp,offset MT_System.Object[]
       mov       rcx,rbp
       xor       edx,edx
       call      CORINFO_HELP_NEWARR_1_OBJ
       mov       ecx,[rdi+4C]
       test      ecx,ecx
       je        near ptr M00_L04
       cmp       ecx,1
       jne       short M00_L03
       mov       rcx,rdi
       mov       r8,rax
       xor       edx,edx
       call      qword ptr [7FFA3EC04FA8]; Stateless.StateMachine`2[[Benchmark.HsmState, Benchmark],[Benchmark.HsmTrigger, Benchmark]].InternalFireQueued(Benchmark.HsmTrigger, System.Object[])
M00_L01:
       mov       rcx,[rbx+20]
       mov       r14,[rcx+8]
       cmp       [r14],r14b
       mov       rcx,rbp
       xor       edx,edx
       call      CORINFO_HELP_NEWARR_1_OBJ
       mov       ecx,[r14+4C]
       test      ecx,ecx
       je        near ptr M00_L06
       cmp       ecx,1
       jne       near ptr M00_L05
       mov       rcx,r14
       mov       r8,rax
       mov       edx,1
       call      qword ptr [7FFA3EC04FA8]; Stateless.StateMachine`2[[Benchmark.HsmState, Benchmark],[Benchmark.HsmTrigger, Benchmark]].InternalFireQueued(Benchmark.HsmTrigger, System.Object[])
M00_L02:
       dec       esi
       jne       short M00_L00
       mov       rdx,[rbx+20]
       mov       rdx,[rdx+8]
       mov       rcx,7FFA3EC1D198
       add       rsp,20
       pop       rbx
       pop       rbp
       pop       rsi
       pop       rdi
       pop       r14
       jmp       qword ptr [7FFA3EC04F30]; BenchmarkDotNet.Engines.DeadCodeEliminationHelper.KeepAliveWithoutBoxing[[System.__Canon, System.Private.CoreLib]](System.__Canon)
M00_L03:
       mov       rcx,offset MT_System.InvalidOperationException
       call      CORINFO_HELP_NEWSFAST
       mov       rdi,rax
       mov       ecx,9B
       mov       rdx,7FFA3EB2F550
       call      CORINFO_HELP_STRCNS
       mov       rdx,rax
       mov       rcx,rdi
       call      qword ptr [7FFA3EA76C70]
       mov       rcx,rdi
       call      CORINFO_HELP_THROW
       int       3
M00_L04:
       mov       rcx,rdi
       mov       r8,rax
       xor       edx,edx
       call      qword ptr [7FFA3EC04F90]; Stateless.StateMachine`2[[Benchmark.HsmState, Benchmark],[Benchmark.HsmTrigger, Benchmark]].InternalFireOne(Benchmark.HsmTrigger, System.Object[])
       jmp       near ptr M00_L01
M00_L05:
       mov       rcx,offset MT_System.InvalidOperationException
       call      CORINFO_HELP_NEWSFAST
       mov       r14,rax
       mov       ecx,9B
       mov       rdx,7FFA3EB2F550
       call      CORINFO_HELP_STRCNS
       mov       rdx,rax
       mov       rcx,r14
       call      qword ptr [7FFA3EA76C70]
       mov       rcx,r14
       call      CORINFO_HELP_THROW
       int       3
M00_L06:
       mov       rcx,r14
       mov       r8,rax
       mov       edx,1
       call      qword ptr [7FFA3EC04F90]; Stateless.StateMachine`2[[Benchmark.HsmState, Benchmark],[Benchmark.HsmTrigger, Benchmark]].InternalFireOne(Benchmark.HsmTrigger, System.Object[])
       jmp       near ptr M00_L02
; Total bytes of code 335
```
```assembly
; Stateless.StateMachine`2[[Benchmark.HsmState, Benchmark],[Benchmark.HsmTrigger, Benchmark]].InternalFireQueued(Benchmark.HsmTrigger, System.Object[])
       push      rbp
       push      r15
       push      r14
       push      rdi
       push      rsi
       push      rbx
       sub       rsp,28
       lea       rbp,[rsp+50]
       mov       [rbp-30],rsp
       mov       [rbp+10],rcx
       mov       rbx,rcx
       mov       edi,edx
       mov       rsi,r8
       mov       rcx,offset MT_Stateless.StateMachine<Benchmark.HsmState, Benchmark.HsmTrigger>+QueuedTrigger
       call      CORINFO_HELP_NEWSFAST
       mov       r14,rax
       mov       r15,[rbx+40]
       mov       [r14+10],edi
       lea       rcx,[r14+8]
       mov       rdx,rsi
       call      CORINFO_HELP_ASSIGN_REF
       mov       edx,[r15+18]
       mov       rcx,[r15+8]
       cmp       edx,[rcx+8]
       je        near ptr M01_L07
M01_L00:
       movsxd    rdx,dword ptr [r15+14]
       mov       rcx,[r15+8]
       mov       r8,r14
       call      System.Runtime.CompilerServices.CastHelpers.StelemRef(System.Object[], IntPtr, System.Object)
       lea       rdx,[r15+14]
       mov       ecx,[rdx]
       inc       ecx
       mov       rax,[r15+8]
       xor       r8d,r8d
       cmp       [rax+8],ecx
       cmove     ecx,r8d
       mov       [rdx],ecx
       inc       dword ptr [r15+18]
       inc       dword ptr [r15+1C]
       cmp       byte ptr [rbx+50],0
       jne       near ptr M01_L08
       mov       byte ptr [rbx+50],1
       mov       rdx,[rbx+40]
       mov       rcx,7FFA3EC1D328
       call      qword ptr [7FFA3EC04FC0]; System.Linq.Enumerable.Any[[System.__Canon, System.Private.CoreLib]](System.Collections.Generic.IEnumerable`1<System.__Canon>)
       test      eax,eax
       je        near ptr M01_L06
M01_L01:
       mov       rcx,[rbx+40]
       mov       r8d,[rcx+10]
       mov       rdx,[rcx+8]
       mov       rax,rdx
       cmp       dword ptr [rcx+18],0
       je        short M01_L04
       mov       r10d,[rax+8]
       cmp       r8d,r10d
       jae       short M01_L05
       mov       r9d,r8d
       mov       r9,[rax+r9*8+10]
       movsxd    r11,r8d
       cmp       r11,r10
       jae       short M01_L05
       movsxd    r8,r8d
       xor       r10d,r10d
       mov       [rax+r8*8+10],r10
       lea       r8,[rcx+10]
       mov       eax,[r8]
       inc       eax
       cmp       [rdx+8],eax
       je        short M01_L03
M01_L02:
       mov       [r8],eax
       dec       dword ptr [rcx+18]
       inc       dword ptr [rcx+1C]
       mov       r8,[r9+8]
       mov       edx,[r9+10]
       mov       rcx,rbx
       call      qword ptr [7FFA3EC04F90]; Stateless.StateMachine`2[[Benchmark.HsmState, Benchmark],[Benchmark.HsmTrigger, Benchmark]].InternalFireOne(Benchmark.HsmTrigger, System.Object[])
       mov       rdx,[rbx+40]
       mov       rcx,7FFA3EC1D328
       call      qword ptr [7FFA3EC04FC0]; System.Linq.Enumerable.Any[[System.__Canon, System.Private.CoreLib]](System.Collections.Generic.IEnumerable`1<System.__Canon>)
       test      eax,eax
       jne       short M01_L01
       jmp       short M01_L06
M01_L03:
       xor       eax,eax
       jmp       short M01_L02
M01_L04:
       call      qword ptr [7FFA3EC0D908]
       int       3
M01_L05:
       call      CORINFO_HELP_RNGCHKFAIL
       int       3
M01_L06:
       mov       byte ptr [rbx+50],0
       jmp       short M01_L08
M01_L07:
       mov       edx,[r15+18]
       inc       edx
       mov       rcx,r15
       call      qword ptr [7FFA3EC050B0]; System.Collections.Generic.Queue`1[[System.__Canon, System.Private.CoreLib]].Grow(Int32)
       jmp       near ptr M01_L00
M01_L08:
       add       rsp,28
       pop       rbx
       pop       rsi
       pop       rdi
       pop       r14
       pop       r15
       pop       rbp
       ret
       push      rbp
       push      r15
       push      r14
       push      rdi
       push      rsi
       push      rbx
       sub       rsp,28
       mov       rbp,[rcx+20]
       mov       [rsp+20],rbp
       lea       rbp,[rbp+50]
       mov       rbx,[rbp+10]
       mov       byte ptr [rbx+50],0
       add       rsp,28
       pop       rbx
       pop       rsi
       pop       rdi
       pop       r14
       pop       r15
       pop       rbp
       ret
; Total bytes of code 403
```
```assembly
; BenchmarkDotNet.Engines.DeadCodeEliminationHelper.KeepAliveWithoutBoxing[[System.__Canon, System.Private.CoreLib]](System.__Canon)
       ret
; Total bytes of code 1
```
```assembly
; Stateless.StateMachine`2[[Benchmark.HsmState, Benchmark],[Benchmark.HsmTrigger, Benchmark]].InternalFireOne(Benchmark.HsmTrigger, System.Object[])
       push      r15
       push      r14
       push      r13
       push      r12
       push      rdi
       push      rsi
       push      rbp
       push      rbx
       sub       rsp,98
       xor       eax,eax
       mov       [rsp+68],rax
       vxorps    xmm4,xmm4,xmm4
       vmovdqu   ymmword ptr [rsp+70],ymm4
       mov       [rsp+90],rax
       mov       rbx,rcx
       mov       edi,edx
       mov       rsi,r8
       mov       rcx,offset MT_Stateless.StateMachine<Benchmark.HsmState, Benchmark.HsmTrigger>+<>c__DisplayClass74_0
       call      CORINFO_HELP_NEWSFAST
       mov       rbp,rax
       mov       [rbp+24],edi
       lea       rcx,[rbp+8]
       mov       rdx,rsi
       call      CORINFO_HELP_ASSIGN_REF
       lea       rcx,[rbp+10]
       mov       rdx,rbx
       call      CORINFO_HELP_ASSIGN_REF
       mov       rsi,[rbx+10]
       mov       edi,[rbp+24]
       mov       rcx,offset MT_System.Collections.Generic.Dictionary<Benchmark.HsmTrigger, Stateless.StateMachine<Benchmark.HsmState, Benchmark.HsmTrigger>+TriggerWithParameters>
       cmp       [rsi],rcx
       jne       near ptr M03_L43
       cmp       qword ptr [rsi+8],0
       je        near ptr M03_L04
       mov       r14,[rsi+18]
       test      r14,r14
       jne       near ptr M03_L40
       mov       eax,edi
       mov       rcx,[rsi+8]
       mov       edx,edi
       imul      rdx,[rsi+30]
       shr       rdx,20
       inc       rdx
       mov       r8d,[rcx+8]
       mov       r10d,r8d
       imul      rdx,r10
       shr       rdx,20
       cmp       edx,r8d
       jae       near ptr M03_L80
       mov       edx,edx
       lea       rcx,[rcx+rdx*4+10]
       mov       r14d,[rcx]
       mov       rsi,[rsi+10]
       xor       r15d,r15d
       dec       r14d
       mov       r13d,[rsi+8]
M03_L00:
       cmp       r13d,r14d
       jbe       short M03_L04
       mov       ecx,r14d
       lea       rcx,[rcx+rcx*2]
       lea       r12,[rsi+rcx*8+10]
       cmp       [r12+8],edi
       jne       near ptr M03_L39
       cmp       [r12+10],eax
       jne       near ptr M03_L39
M03_L01:
       jmp       short M03_L05
M03_L02:
       mov       r12d,[rsi+8]
       cmp       r12d,edx
       jbe       short M03_L04
       mov       edx,edx
       lea       rdx,[rdx+rdx*2]
       lea       rdx,[rsi+rdx*8+10]
       mov       rax,rdx
       cmp       [rax+8],r15d
       je        near ptr M03_L41
M03_L03:
       mov       edx,[rax+0C]
       inc       r13d
       cmp       r12d,r13d
       jae       short M03_L02
       jmp       near ptr M03_L58
M03_L04:
       xor       r12d,r12d
M03_L05:
       test      r12,r12
       jne       near ptr M03_L13
       xor       ecx,ecx
       mov       [rsp+88],rcx
M03_L06:
       mov       r8,[rbx+18]
       mov       rcx,offset Stateless.StateMachine`2+<>c__DisplayClass46_0[[Benchmark.HsmState, Benchmark],[Benchmark.HsmTrigger, Benchmark]].<.ctor>b__0()
       cmp       [r8+18],rcx
       jne       near ptr M03_L45
       mov       rcx,[r8+8]
       mov       rcx,[rcx+8]
       mov       r10d,[rcx+8]
M03_L07:
       mov       [rbp+20],r10d
       mov       edi,[rbp+20]
       mov       rsi,[rbx+8]
       mov       rcx,offset MT_System.Collections.Generic.Dictionary<Benchmark.HsmState, Stateless.StateMachine<Benchmark.HsmState, Benchmark.HsmTrigger>+StateRepresentation>
       cmp       [rsi],rcx
       jne       near ptr M03_L51
       mov       edx,edi
       cmp       qword ptr [rsi+8],0
       je        near ptr M03_L16
       mov       r13,[rsi+18]
       test      r13,r13
       jne       near ptr M03_L47
       mov       rcx,[rsi+8]
       mov       edx,edx
       imul      rdx,[rsi+30]
       shr       rdx,20
       inc       rdx
       mov       eax,[rcx+8]
       imul      rdx,rax
       shr       rdx,20
       cmp       edx,[rcx+8]
       jae       near ptr M03_L80
       mov       edx,edx
       lea       rcx,[rcx+rdx*4+10]
       mov       r14d,[rcx]
       mov       r15,[rsi+10]
       xor       r13d,r13d
       dec       r14d
       mov       esi,[r15+8]
M03_L08:
       cmp       esi,r14d
       jbe       near ptr M03_L16
       mov       ecx,r14d
       lea       rcx,[rcx+rcx*2]
       lea       r14,[r15+rcx*8+10]
       cmp       [r14+8],edi
       jne       near ptr M03_L46
       cmp       [r14+10],edi
       jne       near ptr M03_L46
M03_L09:
       test      r14,r14
       je        near ptr M03_L50
       mov       rcx,[r14]
       mov       [rsp+78],rcx
M03_L10:
       mov       rdx,[rsp+78]
       xor       ecx,ecx
       mov       [rsp+78],rcx
       lea       rcx,[rbp+18]
       call      CORINFO_HELP_ASSIGN_REF
       mov       r15,[rbp+18]
       mov       esi,[rbp+24]
       mov       r13,[rbp+8]
       cmp       [r15],r15b
       xor       ecx,ecx
       mov       [rsp+70],rcx
       mov       rcx,offset MT_Stateless.StateMachine<Benchmark.HsmState, Benchmark.HsmTrigger>+StateRepresentation+<>c__DisplayClass41_0
       call      CORINFO_HELP_NEWSFAST
       mov       rdi,rax
       lea       rcx,[rdi+8]
       mov       rdx,r13
       call      CORINFO_HELP_ASSIGN_REF
       mov       r14,[r15+8]
       mov       rcx,offset MT_System.Collections.Generic.Dictionary<Benchmark.HsmTrigger, System.Collections.Generic.ICollection<Stateless.StateMachine<Benchmark.HsmState, Benchmark.HsmTrigger>+TriggerBehaviour>>
       cmp       [r14],rcx
       jne       near ptr M03_L57
       mov       edx,esi
       cmp       qword ptr [r14+8],0
       je        near ptr M03_L19
       mov       r12,[r14+18]
       test      r12,r12
       jne       near ptr M03_L54
       mov       eax,edx
       mov       rcx,[r14+8]
       mov       r8d,edx
       imul      r8,[r14+30]
       shr       r8,20
       inc       r8
       mov       r10d,[rcx+8]
       mov       r9d,r10d
       imul      r8,r9
       shr       r8,20
       cmp       r8d,r10d
       jae       near ptr M03_L80
       mov       r8d,r8d
       lea       rcx,[rcx+r8*4+10]
       mov       r12d,[rcx]
       mov       r14,[r14+10]
       xor       r8d,r8d
       dec       r12d
       mov       r10d,[r14+8]
M03_L11:
       cmp       r10d,r12d
       jbe       near ptr M03_L19
       mov       ecx,r12d
       lea       rcx,[rcx+rcx*2]
       lea       r12,[r14+rcx*8+10]
       cmp       [r12+8],edx
       jne       near ptr M03_L53
       cmp       [r12+10],eax
       jne       near ptr M03_L53
M03_L12:
       jmp       near ptr M03_L20
M03_L13:
       mov       rcx,[r12]
       mov       [rsp+88],rcx
       jmp       near ptr M03_L44
M03_L14:
       mov       r12d,[rsi+8]
       cmp       r12d,edx
       jbe       short M03_L16
       mov       edx,edx
       lea       rdx,[rdx+rdx*2]
       lea       rdx,[rsi+rdx*8+10]
       mov       rax,rdx
       cmp       [rax+8],r14d
       je        near ptr M03_L48
M03_L15:
       mov       edx,[rax+0C]
       inc       r15d
       cmp       r12d,r15d
       jae       short M03_L14
       jmp       near ptr M03_L58
M03_L16:
       xor       r14d,r14d
       jmp       near ptr M03_L09
M03_L17:
       mov       r10d,[r14+8]
       cmp       r10d,edx
       jbe       short M03_L19
       mov       edx,edx
       lea       rdx,[rdx+rdx*2]
       lea       rdx,[r14+rdx*8+10]
       mov       r9,rdx
       mov       [rsp+64],eax
       cmp       [r9+8],eax
       je        near ptr M03_L55
M03_L18:
       mov       edx,[r9+0C]
       inc       ecx
       mov       [rsp+60],ecx
       cmp       r10d,ecx
       mov       eax,[rsp+64]
       mov       ecx,[rsp+60]
       jae       short M03_L17
       jmp       near ptr M03_L58
M03_L19:
       xor       r12d,r12d
M03_L20:
       test      r12,r12
       jne       short M03_L22
       xor       ecx,ecx
       mov       [rsp+68],rcx
M03_L21:
       xor       r14d,r14d
       jmp       near ptr M03_L64
M03_L22:
       mov       rcx,[r12]
       mov       [rsp+68],rcx
M03_L23:
       mov       rcx,offset MT_System.Func<Stateless.StateMachine<Benchmark.HsmState, Benchmark.HsmTrigger>+TriggerBehaviour, Stateless.StateMachine<Benchmark.HsmState, Benchmark.HsmTrigger>+TriggerBehaviourResult>
       call      CORINFO_HELP_NEWSFAST
       mov       r14,rax
       mov       r12,[rsp+68]
       lea       rcx,[r14+8]
       mov       rdx,rdi
       call      CORINFO_HELP_ASSIGN_REF
       mov       r8,offset Stateless.StateMachine`2+StateRepresentation+<>c__DisplayClass41_0[[Benchmark.HsmState, Benchmark],[Benchmark.HsmTrigger, Benchmark]].<TryFindLocalHandler>b__0(TriggerBehaviour<Benchmark.HsmState,Benchmark.HsmTrigger>)
       mov       [r14+18],r8
       mov       r8,r14
       mov       rdx,r12
       mov       rcx,7FFA3EC32568
       call      qword ptr [7FFA3EA77540]; System.Linq.Enumerable.Select[[System.__Canon, System.Private.CoreLib],[System.__Canon, System.Private.CoreLib]](System.Collections.Generic.IEnumerable`1<System.__Canon>, System.Func`2<System.__Canon,System.__Canon>)
       mov       r14,rax
       mov       rdx,r14
       mov       rcx,offset MT_System.Linq.Enumerable+Iterator<Stateless.StateMachine<Benchmark.HsmState, Benchmark.HsmTrigger>+TriggerBehaviourResult>
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       r12,rax
       test      r12,r12
       je        near ptr M03_L61
       mov       rdx,offset MT_System.Linq.Enumerable+ListSelectIterator<Stateless.StateMachine<Benchmark.HsmState, Benchmark.HsmTrigger>+TriggerBehaviour, Stateless.StateMachine<Benchmark.HsmState, Benchmark.HsmTrigger>+TriggerBehaviourResult>
       cmp       [r12],rdx
       jne       near ptr M03_L60
       mov       rdx,[r12+18]
       xor       edi,edi
       xor       r14d,r14d
       test      rdx,rdx
       je        short M03_L24
       mov       r14d,[rdx+10]
       mov       rdi,[rdx+8]
       cmp       [rdi+8],r14d
       jb        near ptr M03_L58
       add       rdi,10
M03_L24:
       test      r14d,r14d
       je        near ptr M03_L59
       mov       edx,r14d
       mov       rcx,offset MT_Stateless.StateMachine<Benchmark.HsmState, Benchmark.HsmTrigger>+TriggerBehaviourResult[]
       call      CORINFO_HELP_NEWARR_1_OBJ
       mov       [rsp+30],rax
       lea       r8,[rax+10]
       mov       r10d,[rax+8]
       mov       [rsp+28],r8
       mov       [rsp+5C],r10d
       mov       r12,[r12+20]
       xor       r9d,r9d
       test      r10d,r10d
       jle       short M03_L26
M03_L25:
       lea       rcx,[r8+r9*8]
       mov       [rsp+90],rcx
       cmp       r9d,r14d
       jae       near ptr M03_L80
       mov       [rsp+50],r9
       mov       rdx,[rdi+r9*8]
       mov       rcx,[r12+8]
       call      qword ptr [r12+18]
       mov       rcx,[rsp+90]
       mov       rdx,rax
       call      CORINFO_HELP_CHECKED_ASSIGN_REF
       mov       rcx,[rsp+50]
       inc       ecx
       mov       edx,[rsp+5C]
       cmp       ecx,edx
       mov       r9,rcx
       mov       r8,[rsp+28]
       jl        short M03_L25
M03_L26:
       mov       rax,[rsp+30]
       mov       rdi,rax
M03_L27:
       mov       rcx,r15
       mov       edx,esi
       mov       r8,rdi
       call      qword ptr [7FFA3EC05410]; Stateless.StateMachine`2+StateRepresentation[[Benchmark.HsmState, Benchmark],[Benchmark.HsmTrigger, Benchmark]].TryFindLocalHandlerResult(Benchmark.HsmTrigger, System.Collections.Generic.IEnumerable`1<TriggerBehaviourResult<Benchmark.HsmState,Benchmark.HsmTrigger>>)
       mov       r14,rax
       test      r14,r14
       je        near ptr M03_L63
M03_L28:
       test      r14,r14
       je        near ptr M03_L64
       mov       rdx,[r14+10]
       mov       rcx,7FFA3EC327B8
       call      qword ptr [7FFA3EC04FC0]; System.Linq.Enumerable.Any[[System.__Canon, System.Private.CoreLib]](System.Collections.Generic.IEnumerable`1<System.__Canon>)
       test      eax,eax
       sete      r12b
       movzx     r12d,r12b
M03_L29:
       xor       ecx,ecx
       mov       [rsp+68],rcx
       test      r12d,r12d
       je        near ptr M03_L38
       mov       eax,1
M03_L30:
       mov       rcx,[rsp+70]
       cmp       qword ptr [rsp+70],0
       cmove     rcx,r14
       movzx     eax,al
       xor       edx,edx
       mov       [rsp+70],rdx
       test      eax,eax
       je        near ptr M03_L77
       mov       r12,[rcx+8]
       mov       rax,r12
       test      rax,rax
       je        short M03_L31
       mov       rcx,offset MT_Stateless.StateMachine<Benchmark.HsmState, Benchmark.HsmTrigger>+TransitioningTriggerBehaviour
       cmp       [rax],rcx
       jne       near ptr M03_L66
       xor       eax,eax
M03_L31:
       test      rax,rax
       jne       near ptr M03_L37
       mov       rdi,r12
       test      rdi,rdi
       je        short M03_L32
       mov       rcx,offset MT_Stateless.StateMachine<Benchmark.HsmState, Benchmark.HsmTrigger>+TransitioningTriggerBehaviour
       cmp       [rdi],rcx
       jne       near ptr M03_L67
       xor       edi,edi
M03_L32:
       test      rdi,rdi
       jne       near ptr M03_L76
       mov       rdi,r12
       test      rdi,rdi
       je        short M03_L33
       mov       rcx,offset MT_Stateless.StateMachine<Benchmark.HsmState, Benchmark.HsmTrigger>+TransitioningTriggerBehaviour
       cmp       [rdi],rcx
       jne       near ptr M03_L68
       xor       edi,edi
M03_L33:
       test      rdi,rdi
       jne       near ptr M03_L75
       mov       rcx,r12
       test      rcx,rcx
       je        short M03_L34
       mov       rax,offset MT_Stateless.StateMachine<Benchmark.HsmState, Benchmark.HsmTrigger>+TransitioningTriggerBehaviour
       cmp       [rcx],rax
       jne       near ptr M03_L69
       xor       ecx,ecx
M03_L34:
       test      rcx,rcx
       jne       near ptr M03_L74
       mov       r15,r12
       test      r15,r15
       je        short M03_L35
       mov       rcx,offset MT_Stateless.StateMachine<Benchmark.HsmState, Benchmark.HsmTrigger>+TransitioningTriggerBehaviour
       cmp       [r15],rcx
       jne       near ptr M03_L70
M03_L35:
       test      r15,r15
       je        near ptr M03_L72
       lea       rsi,[rbp+20]
       mov       rcx,offset MT_Benchmark.HsmState
       call      CORINFO_HELP_NEWSFAST
       mov       r13,rax
       mov       ecx,[r15+14]
       mov       [r13+8],ecx
       mov       rcx,offset MT_Benchmark.HsmState
       call      CORINFO_HELP_NEWSFAST
       mov       ecx,[rsi]
       mov       [rax+8],ecx
       mov       rcx,rax
       mov       rdx,r13
       call      qword ptr [7FFA3E6A6098]; System.Enum.Equals(System.Object)
       test      eax,eax
       jne       short M03_L37
       mov       r14d,[rbp+20]
       mov       rcx,offset MT_Stateless.StateMachine<Benchmark.HsmState, Benchmark.HsmTrigger>+Transition
       call      CORINFO_HELP_NEWSFAST
       mov       rdi,rax
       mov       ecx,[r15+14]
       mov       edx,[rbp+24]
       mov       rax,[rbp+8]
       mov       [rdi+10],r14d
       mov       [rdi+14],ecx
       mov       [rdi+18],edx
       test      rax,rax
       je        near ptr M03_L71
M03_L36:
       lea       rcx,[rdi+8]
       mov       rdx,rax
       call      CORINFO_HELP_ASSIGN_REF
       mov       r8,[rbp+18]
       mov       rdx,[rbp+8]
       mov       rcx,rbx
       mov       r9,rdi
       call      qword ptr [7FFA3EC052D8]; Stateless.StateMachine`2[[Benchmark.HsmState, Benchmark],[Benchmark.HsmTrigger, Benchmark]].HandleTransitioningTrigger(System.Object[], StateRepresentation<Benchmark.HsmState,Benchmark.HsmTrigger>, Transition<Benchmark.HsmState,Benchmark.HsmTrigger>)
M03_L37:
       nop
       add       rsp,98
       pop       rbx
       pop       rbp
       pop       rsi
       pop       rdi
       pop       r12
       pop       r13
       pop       r14
       pop       r15
       ret
M03_L38:
       mov       rcx,[r15+30]
       test      rcx,rcx
       je        near ptr M03_L65
       lea       r9,[rsp+70]
       mov       edx,esi
       mov       r8,r13
       cmp       [rcx],ecx
       call      qword ptr [7FFA3EC05218]; Stateless.StateMachine`2+StateRepresentation[[Benchmark.HsmState, Benchmark],[Benchmark.HsmTrigger, Benchmark]].TryFindHandler(Benchmark.HsmTrigger, System.Object[], TriggerBehaviourResult<Benchmark.HsmState,Benchmark.HsmTrigger> ByRef)
       jmp       near ptr M03_L30
M03_L39:
       mov       r14d,[r12+0C]
       inc       r15d
       cmp       r13d,r15d
       jae       near ptr M03_L00
       jmp       near ptr M03_L58
M03_L40:
       mov       rcx,r14
       mov       edx,edi
       mov       r11,7FFA3E6B0760
       call      qword ptr [r11]
       mov       r15d,eax
       mov       rdx,[rsi+8]
       mov       ecx,r15d
       imul      rcx,[rsi+30]
       shr       rcx,20
       inc       rcx
       mov       r8d,[rdx+8]
       imul      rcx,r8
       shr       rcx,20
       cmp       ecx,[rdx+8]
       jae       near ptr M03_L80
       mov       ecx,ecx
       lea       rdx,[rdx+rcx*4+10]
       mov       edx,[rdx]
       mov       rsi,[rsi+10]
       xor       r13d,r13d
       dec       edx
       jmp       near ptr M03_L02
M03_L41:
       mov       [rsp+48],rax
       mov       edx,[rax+10]
       mov       rcx,r14
       mov       r8d,edi
       mov       r11,7FFA3E6B0768
       call      qword ptr [r11]
       test      eax,eax
       jne       short M03_L42
       mov       rax,[rsp+48]
       jmp       near ptr M03_L03
M03_L42:
       mov       r12,[rsp+48]
       jmp       near ptr M03_L01
M03_L43:
       lea       r8,[rsp+88]
       mov       rcx,rsi
       mov       edx,edi
       mov       r11,7FFA3E6B0758
       call      qword ptr [r11]
       test      eax,eax
       je        near ptr M03_L06
M03_L44:
       mov       rdx,[rbp+8]
       mov       rcx,[rsp+88]
       cmp       [rcx],ecx
       call      qword ptr [7FFA3EC051E8]
       jmp       near ptr M03_L06
M03_L45:
       mov       rcx,[r8+8]
       call      qword ptr [r8+18]
       mov       r10d,eax
       jmp       near ptr M03_L07
M03_L46:
       mov       r14d,[r14+0C]
       inc       r13d
       cmp       esi,r13d
       jae       near ptr M03_L08
       jmp       near ptr M03_L58
M03_L47:
       mov       rcx,r13
       mov       r11,7FFA3E6B0780
       call      qword ptr [r11]
       mov       r14d,eax
       mov       rdx,[rsi+8]
       mov       ecx,r14d
       imul      rcx,[rsi+30]
       shr       rcx,20
       inc       rcx
       mov       r8d,[rdx+8]
       imul      rcx,r8
       shr       rcx,20
       cmp       ecx,[rdx+8]
       jae       near ptr M03_L80
       mov       ecx,ecx
       lea       rdx,[rdx+rcx*4+10]
       mov       edx,[rdx]
       mov       rsi,[rsi+10]
       xor       r15d,r15d
       dec       edx
       jmp       near ptr M03_L14
M03_L48:
       mov       [rsp+40],rax
       mov       edx,[rax+10]
       mov       rcx,r13
       mov       r8d,edi
       mov       r11,7FFA3E6B0788
       call      qword ptr [r11]
       test      eax,eax
       jne       short M03_L49
       mov       rax,[rsp+40]
       jmp       near ptr M03_L15
M03_L49:
       mov       r14,[rsp+40]
       jmp       near ptr M03_L09
M03_L50:
       xor       r8d,r8d
       mov       [rsp+78],r8
       jmp       short M03_L52
M03_L51:
       lea       r8,[rsp+78]
       mov       rcx,rsi
       mov       edx,edi
       mov       r11,7FFA3E6B0770
       call      qword ptr [r11]
       test      eax,eax
       jne       near ptr M03_L10
M03_L52:
       mov       rcx,offset MT_Stateless.StateMachine<Benchmark.HsmState, Benchmark.HsmTrigger>+StateRepresentation
       call      CORINFO_HELP_NEWSFAST
       mov       rsi,rax
       movzx     r8d,byte ptr [rbx+51]
       mov       rcx,rsi
       mov       edx,edi
       call      qword ptr [7FFA3EB051A0]; Stateless.StateMachine`2+StateRepresentation[[Benchmark.HsmState, Benchmark],[Benchmark.HsmTrigger, Benchmark]]..ctor(Benchmark.HsmState, Boolean)
       mov       [rsp+78],rsi
       mov       rcx,[rbx+8]
       mov       r8,[rsp+78]
       mov       edx,edi
       mov       r11,7FFA3E6B0778
       call      qword ptr [r11]
       jmp       near ptr M03_L10
M03_L53:
       mov       r12d,[r12+0C]
       inc       r8d
       cmp       r10d,r8d
       jae       near ptr M03_L11
       jmp       near ptr M03_L58
M03_L54:
       mov       rcx,r12
       mov       r11,7FFA3E6B0798
       call      qword ptr [r11]
       mov       rdx,[r14+8]
       mov       ecx,eax
       imul      rcx,[r14+30]
       shr       rcx,20
       inc       rcx
       mov       r8d,[rdx+8]
       imul      rcx,r8
       shr       rcx,20
       cmp       ecx,[rdx+8]
       jae       near ptr M03_L80
       mov       ecx,ecx
       lea       rdx,[rdx+rcx*4+10]
       mov       edx,[rdx]
       mov       r14,[r14+10]
       xor       ecx,ecx
       dec       edx
       jmp       near ptr M03_L17
M03_L55:
       mov       [rsp+60],ecx
       mov       [rsp+58],r10d
       mov       [rsp+38],r9
       mov       edx,[r9+10]
       mov       rcx,r12
       mov       r8d,esi
       mov       r11,7FFA3E6B07A0
       call      qword ptr [r11]
       test      eax,eax
       mov       ecx,[rsp+60]
       mov       r10d,[rsp+58]
       jne       short M03_L56
       mov       r9,[rsp+38]
       jmp       near ptr M03_L18
M03_L56:
       mov       r12,[rsp+38]
       jmp       near ptr M03_L12
M03_L57:
       lea       r8,[rsp+68]
       mov       rcx,r14
       mov       edx,esi
       mov       r11,7FFA3E6B0790
       call      qword ptr [r11]
       test      eax,eax
       jne       near ptr M03_L23
       jmp       near ptr M03_L21
M03_L58:
       call      qword ptr [7FFA3E75F2A0]
       int       3
M03_L59:
       mov       rcx,offset MT_System.Array+EmptyArray<Stateless.StateMachine<Benchmark.HsmState, Benchmark.HsmTrigger>+TriggerBehaviourResult>
       call      CORINFO_HELP_GET_GCSTATIC_BASE
       mov       rcx,202C7C01570
       mov       rdi,[rcx]
       jmp       near ptr M03_L27
M03_L60:
       mov       rcx,r12
       mov       rax,[r12]
       mov       rax,[rax+40]
       call      qword ptr [rax+20]
       mov       rdi,rax
       jmp       near ptr M03_L27
M03_L61:
       mov       rdx,r14
       mov       rcx,offset MT_System.Collections.Generic.ICollection<Stateless.StateMachine<Benchmark.HsmState, Benchmark.HsmTrigger>+TriggerBehaviourResult>
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfInterface(Void*, System.Object)
       test      rax,rax
       je        short M03_L62
       mov       rdx,rax
       mov       rcx,7FFA3EC7EED0
       call      qword ptr [7FFA3EA7D4A0]; System.Linq.Enumerable.ICollectionToArray[[System.__Canon, System.Private.CoreLib]](System.Collections.Generic.ICollection`1<System.__Canon>)
       mov       rdi,rax
       jmp       near ptr M03_L27
M03_L62:
       mov       rdx,r14
       mov       rcx,7FFA3EC7EF58
       call      qword ptr [7FFA3EC0D920]
       mov       rdi,rax
       jmp       near ptr M03_L27
M03_L63:
       mov       rcx,rdi
       call      qword ptr [7FFA3EC05428]
       mov       r14,rax
       jmp       near ptr M03_L28
M03_L64:
       xor       r12d,r12d
       jmp       near ptr M03_L29
M03_L65:
       xor       eax,eax
       jmp       near ptr M03_L30
M03_L66:
       mov       rdx,r12
       mov       rcx,offset MT_Stateless.StateMachine<Benchmark.HsmState, Benchmark.HsmTrigger>+IgnoredTriggerBehaviour
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       jmp       near ptr M03_L31
M03_L67:
       mov       rdx,r12
       mov       rcx,offset MT_Stateless.StateMachine<Benchmark.HsmState, Benchmark.HsmTrigger>+ReentryTriggerBehaviour
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rdi,rax
       jmp       near ptr M03_L32
M03_L68:
       mov       rdx,r12
       mov       rcx,offset MT_Stateless.StateMachine<Benchmark.HsmState, Benchmark.HsmTrigger>+DynamicTriggerBehaviourAsync
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rdi,rax
       jmp       near ptr M03_L33
M03_L69:
       mov       rdx,r12
       mov       rcx,offset MT_Stateless.StateMachine<Benchmark.HsmState, Benchmark.HsmTrigger>+DynamicTriggerBehaviour
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rcx,rax
       jmp       near ptr M03_L34
M03_L70:
       mov       rdx,r12
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       r15,rax
       jmp       near ptr M03_L35
M03_L71:
       mov       rcx,offset MT_System.Object[]
       xor       edx,edx
       call      CORINFO_HELP_NEWARR_1_OBJ
       jmp       near ptr M03_L36
M03_L72:
       mov       rdx,r12
       mov       rcx,offset MT_Stateless.StateMachine<Benchmark.HsmState, Benchmark.HsmTrigger>+InternalTriggerBehaviour
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       test      rax,rax
       jne       short M03_L73
       mov       rcx,offset MT_System.InvalidOperationException
       call      CORINFO_HELP_NEWSFAST
       mov       rbp,rax
       mov       ecx,0ED
       mov       rdx,7FFA3EB2F550
       call      CORINFO_HELP_STRCNS
       mov       rdx,rax
       mov       rcx,rbp
       call      qword ptr [7FFA3EA76C70]
       mov       rcx,rbp
       call      CORINFO_HELP_THROW
       int       3
M03_L73:
       mov       rcx,offset MT_Stateless.StateMachine<Benchmark.HsmState, Benchmark.HsmTrigger>+Transition
       call      CORINFO_HELP_NEWSFAST
       mov       rsi,rax
       mov       rdx,[rbp+8]
       mov       [rsp+20],rdx
       mov       edx,[rbp+20]
       mov       r8d,[rbp+20]
       mov       r9d,[rbp+24]
       mov       rcx,rsi
       call      qword ptr [7FFA3EC05278]; Stateless.StateMachine`2+Transition[[Benchmark.HsmState, Benchmark],[Benchmark.HsmTrigger, Benchmark]]..ctor(Benchmark.HsmState, Benchmark.HsmState, Benchmark.HsmTrigger, System.Object[])
       mov       rax,[rbx+18]
       mov       rcx,[rax+8]
       call      qword ptr [rax+18]
       mov       edx,eax
       mov       rcx,rbx
       call      qword ptr [7FFA3EB05128]; Stateless.StateMachine`2[[Benchmark.HsmState, Benchmark],[Benchmark.HsmTrigger, Benchmark]].GetRepresentation(Benchmark.HsmState)
       mov       rcx,rax
       mov       r8,[rbp+8]
       mov       rdx,rsi
       cmp       [rcx],ecx
       call      qword ptr [7FFA3EC05320]
       jmp       near ptr M03_L37
M03_L74:
       mov       edx,[rbp+20]
       mov       r8,[rbp+8]
       lea       r9,[rsp+80]
       call      qword ptr [7FFA3EC052C0]
       mov       rcx,offset MT_Stateless.StateMachine<Benchmark.HsmState, Benchmark.HsmTrigger>+Transition
       call      CORINFO_HELP_NEWSFAST
       mov       rsi,rax
       mov       r9,[rbp+8]
       mov       [rsp+20],r9
       mov       r9d,[rbp+24]
       mov       edx,[rbp+20]
       mov       rcx,rsi
       mov       r8d,[rsp+80]
       call      qword ptr [7FFA3EC05278]; Stateless.StateMachine`2+Transition[[Benchmark.HsmState, Benchmark],[Benchmark.HsmTrigger, Benchmark]]..ctor(Benchmark.HsmState, Benchmark.HsmState, Benchmark.HsmTrigger, System.Object[])
       mov       r8,[rbp+18]
       mov       rdx,[rbp+8]
       mov       rcx,rbx
       mov       r9,rsi
       call      qword ptr [7FFA3EC052D8]; Stateless.StateMachine`2[[Benchmark.HsmState, Benchmark],[Benchmark.HsmTrigger, Benchmark]].HandleTransitioningTrigger(System.Object[], StateRepresentation<Benchmark.HsmState,Benchmark.HsmTrigger>, Transition<Benchmark.HsmState,Benchmark.HsmTrigger>)
       jmp       near ptr M03_L37
M03_L75:
       mov       rcx,offset MT_System.Func<System.Threading.Tasks.Task<Benchmark.HsmState>, System.Threading.Tasks.Task>
       call      CORINFO_HELP_NEWSFAST
       mov       rbx,rax
       mov       r8,[rbp+8]
       mov       edx,[rbp+20]
       mov       rcx,rdi
       call      qword ptr [7FFA3EC052A8]
       mov       rsi,rax
       mov       rcx,rbx
       mov       rdx,rbp
       mov       r8,7FFA3EC011B8
       call      qword ptr [7FFA3E7569D0]; System.MulticastDelegate.CtorClosed(System.Object, IntPtr)
       mov       rcx,rsi
       mov       r8,rbx
       mov       rdx,7FFA3EC30B98
       cmp       [rcx],ecx
       call      qword ptr [7FFA3EC05128]
       jmp       near ptr M03_L37
M03_L76:
       mov       esi,[rbp+20]
       mov       rcx,offset MT_Stateless.StateMachine<Benchmark.HsmState, Benchmark.HsmTrigger>+Transition
       call      CORINFO_HELP_NEWSFAST
       mov       r14,rax
       mov       r9,[rbp+8]
       mov       [rsp+20],r9
       mov       r9d,[rbp+24]
       mov       r8d,[rdi+14]
       mov       rcx,r14
       mov       edx,esi
       call      qword ptr [7FFA3EC05278]; Stateless.StateMachine`2+Transition[[Benchmark.HsmState, Benchmark],[Benchmark.HsmTrigger, Benchmark]]..ctor(Benchmark.HsmState, Benchmark.HsmState, Benchmark.HsmTrigger, System.Object[])
       mov       r8,[rbp+18]
       mov       rdx,[rbp+8]
       mov       rcx,rbx
       mov       r9,r14
       call      qword ptr [7FFA3EC05290]
       jmp       near ptr M03_L37
M03_L77:
       mov       rax,[rbx+28]
       mov       rdx,[rbp+18]
       mov       edx,[rdx+40]
       mov       r8d,[rbp+24]
       test      rcx,rcx
       jne       short M03_L78
       xor       r9d,r9d
       jmp       short M03_L79
M03_L78:
       mov       r9,[rcx+10]
M03_L79:
       mov       rcx,rax
       mov       rax,[rax]
       mov       rax,[rax+40]
       call      qword ptr [rax+20]
       jmp       near ptr M03_L37
M03_L80:
       call      CORINFO_HELP_RNGCHKFAIL
       int       3
; Total bytes of code 3274
```
```assembly
; System.Runtime.CompilerServices.CastHelpers.StelemRef(System.Object[], IntPtr, System.Object)
       sub       rsp,28
       mov       eax,[rcx+8]
       cmp       rax,rdx
       jbe       short M04_L03
       lea       rax,[rcx+rdx*8+10]
       mov       rdx,[rcx]
       mov       rdx,[rdx+30]
       test      r8,r8
       je        short M04_L02
       cmp       rdx,[r8]
       jne       short M04_L01
M04_L00:
       mov       rcx,rax
       mov       rdx,r8
       add       rsp,28
       jmp       near ptr System.Runtime.CompilerServices.CastHelpers.WriteBarrier(System.Object ByRef, System.Object)
M04_L01:
       mov       r10,offset MT_System.Object[]
       cmp       [rcx],r10
       je        short M04_L00
       mov       rcx,rax
       add       rsp,28
       jmp       qword ptr [7FFA3EA74420]; System.Runtime.CompilerServices.CastHelpers.StelemRef_Helper(System.Object ByRef, Void*, System.Object)
M04_L02:
       xor       ecx,ecx
       mov       [rax],rcx
       add       rsp,28
       ret
M04_L03:
       call      qword ptr [7FFA3EC0C5B8]
       int       3
; Total bytes of code 94
```
```assembly
; System.Linq.Enumerable.Any[[System.__Canon, System.Private.CoreLib]](System.Collections.Generic.IEnumerable`1<System.__Canon>)
       push      rbp
       push      r15
       push      r14
       push      rdi
       push      rsi
       push      rbx
       sub       rsp,48
       lea       rbp,[rsp+70]
       mov       [rbp-50],rsp
       mov       [rbp-30],rcx
       mov       rbx,rcx
       mov       rsi,rdx
       test      rsi,rsi
       je        near ptr M05_L19
       mov       rcx,[rbx+18]
       mov       rcx,[rcx+28]
       test      rcx,rcx
       je        near ptr M05_L05
M05_L00:
       mov       rdx,rsi
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfInterface(Void*, System.Object)
       mov       rdi,rax
       test      rdi,rdi
       jne       near ptr M05_L08
       mov       rcx,[rbx+18]
       cmp       qword ptr [rcx+8],30
       jle       near ptr M05_L06
       mov       rcx,[rcx+30]
       test      rcx,rcx
       je        near ptr M05_L06
M05_L01:
       mov       rdx,rsi
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       r14,rax
       test      r14,r14
       jne       near ptr M05_L17
       mov       rcx,rsi
       mov       rax,offset MT_System.Collections.Generic.Queue<Stateless.StateMachine<Benchmark.HsmState, Benchmark.HsmTrigger>+QueuedTrigger>
       cmp       [rcx],rax
       jne       near ptr M05_L11
M05_L02:
       test      rcx,rcx
       je        near ptr M05_L13
       mov       rax,offset MT_System.Collections.Generic.Queue<Stateless.StateMachine<Benchmark.HsmState, Benchmark.HsmTrigger>+QueuedTrigger>
       cmp       [rcx],rax
       jne       near ptr M05_L12
       mov       r14d,[rcx+18]
M05_L03:
       test      r14d,r14d
       setne     r15b
       movzx     r15d,r15b
M05_L04:
       movzx     eax,r15b
       add       rsp,48
       pop       rbx
       pop       rsi
       pop       rdi
       pop       r14
       pop       r15
       pop       rbp
       ret
M05_L05:
       mov       rcx,rbx
       mov       rdx,7FFA3EC288B0
       call      CORINFO_HELP_RUNTIMEHANDLE_METHOD
       mov       rcx,rax
       jmp       near ptr M05_L00
M05_L06:
       mov       rcx,rbx
       mov       rdx,7FFA3EC28948
       call      CORINFO_HELP_RUNTIMEHANDLE_METHOD
       mov       rcx,rax
       jmp       near ptr M05_L01
M05_L07:
       mov       rcx,[rbp-40]
       mov       r11,7FFA3E6B0650
       call      qword ptr [r11]
       mov       r15d,eax
       jmp       near ptr M05_L16
M05_L08:
       mov       rcx,[rbx+18]
       cmp       qword ptr [rcx+8],40
       jle       short M05_L10
       mov       r11,[rcx+40]
       test      r11,r11
       je        short M05_L10
M05_L09:
       mov       rcx,rdi
       call      qword ptr [r11]
       test      eax,eax
       setne     r15b
       movzx     r15d,r15b
       jmp       near ptr M05_L04
M05_L10:
       mov       rcx,rbx
       mov       rdx,7FFA3EC28970
       call      CORINFO_HELP_RUNTIMEHANDLE_METHOD
       mov       r11,rax
       jmp       short M05_L09
M05_L11:
       mov       rdx,rsi
       mov       rcx,offset MT_System.Collections.ICollection
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfInterface(Void*, System.Object)
       mov       rcx,rax
       jmp       near ptr M05_L02
M05_L12:
       mov       r11,7FFA3E6B0660
       call      qword ptr [r11]
       mov       r14d,eax
       jmp       near ptr M05_L03
M05_L13:
       mov       rcx,[rbx+18]
       cmp       qword ptr [rcx+8],38
       jle       short M05_L14
       mov       r11,[rcx+38]
       test      r11,r11
       je        short M05_L14
       jmp       short M05_L15
M05_L14:
       mov       rcx,rbx
       mov       rdx,7FFA3EC28958
       call      CORINFO_HELP_RUNTIMEHANDLE_METHOD
       mov       r11,rax
M05_L15:
       mov       rcx,rsi
       call      qword ptr [r11]
       mov       [rbp-40],rax
       jmp       near ptr M05_L07
M05_L16:
       mov       rcx,[rbp-40]
       mov       r11,7FFA3E6B0658
       call      qword ptr [r11]
       jmp       near ptr M05_L04
M05_L17:
       mov       rcx,r14
       mov       edx,1
       mov       rax,[r14]
       mov       rax,[rax+40]
       call      qword ptr [rax+30]
       test      eax,eax
       jl        short M05_L18
       test      eax,eax
       setne     r15b
       movzx     r15d,r15b
       jmp       near ptr M05_L04
M05_L18:
       lea       rdx,[rbp-38]
       mov       rcx,r14
       mov       rax,[r14]
       mov       rax,[rax+48]
       call      qword ptr [rax+10]
       movzx     r15d,byte ptr [rbp-38]
       jmp       near ptr M05_L04
M05_L19:
       mov       ecx,11
       call      qword ptr [7FFA3E75F738]
       int       3
       push      rbp
       push      r15
       push      r14
       push      rdi
       push      rsi
       push      rbx
       sub       rsp,28
       mov       rbp,[rcx+20]
       mov       [rsp+20],rbp
       lea       rbp,[rbp+70]
       cmp       qword ptr [rbp-40],0
       je        short M05_L20
       mov       rcx,[rbp-40]
       mov       r11,7FFA3E6B0658
       call      qword ptr [r11]
M05_L20:
       nop
       add       rsp,28
       pop       rbx
       pop       rsi
       pop       rdi
       pop       r14
       pop       r15
       pop       rbp
       ret
; Total bytes of code 614
```
```assembly
; System.Collections.Generic.Queue`1[[System.__Canon, System.Private.CoreLib]].Grow(Int32)
       mov       rax,[rcx+8]
       mov       r8d,[rax+8]
       add       r8d,r8d
       mov       r10d,7FFFFFC7
       cmp       r8d,7FFFFFC7
       cmova     r8d,r10d
       mov       eax,[rax+8]
       add       eax,4
       cmp       r8d,eax
       cmovl     r8d,eax
       cmp       r8d,edx
       cmovl     r8d,edx
       mov       edx,r8d
       lea       rax,[System.Reflection.CustomAttributeExtensions.GetCustomAttribute[[System.__Canon, System.Private.CoreLib]](System.Reflection.Assembly)]
       jmp       qword ptr [rax]
; Total bytes of code 61
```
```assembly
; Stateless.StateMachine`2+<>c__DisplayClass46_0[[Benchmark.HsmState, Benchmark],[Benchmark.HsmTrigger, Benchmark]].<.ctor>b__0()
       mov       rax,[rcx+8]
       mov       eax,[rax+8]
       ret
; Total bytes of code 8
```
```assembly
; Stateless.StateMachine`2+StateRepresentation+<>c__DisplayClass41_0[[Benchmark.HsmState, Benchmark],[Benchmark.HsmTrigger, Benchmark]].<TryFindLocalHandler>b__0(TriggerBehaviour<Benchmark.HsmState,Benchmark.HsmTrigger>)
       push      rdi
       push      rsi
       push      rbx
       sub       rsp,20
       mov       rbx,rdx
       mov       rsi,[rcx+8]
       mov       rcx,offset MT_Stateless.StateMachine<Benchmark.HsmState, Benchmark.HsmTrigger>+TriggerBehaviourResult
       call      CORINFO_HELP_NEWSFAST
       mov       rdi,rax
       mov       rcx,[rbx+8]
       mov       rdx,rsi
       cmp       [rcx],ecx
       call      qword ptr [7FFA3EC055F0]; Stateless.StateMachine`2+TransitionGuard[[Benchmark.HsmState, Benchmark],[Benchmark.HsmTrigger, Benchmark]].UnmetGuardConditions(System.Object[])
       mov       rsi,rax
       lea       rcx,[rdi+8]
       mov       rdx,rbx
       call      CORINFO_HELP_ASSIGN_REF
       lea       rcx,[rdi+10]
       mov       rdx,rsi
       call      CORINFO_HELP_ASSIGN_REF
       mov       rax,rdi
       add       rsp,20
       pop       rbx
       pop       rsi
       pop       rdi
       ret
; Total bytes of code 85
```
```assembly
; System.Linq.Enumerable.Select[[System.__Canon, System.Private.CoreLib],[System.__Canon, System.Private.CoreLib]](System.Collections.Generic.IEnumerable`1<System.__Canon>, System.Func`2<System.__Canon,System.__Canon>)
       push      r15
       push      r14
       push      rdi
       push      rsi
       push      rbp
       push      rbx
       sub       rsp,28
       mov       [rsp+20],rcx
       mov       rbx,rcx
       mov       rsi,rdx
       mov       rdi,r8
       test      rsi,rsi
       je        near ptr M09_L27
       test      rdi,rdi
       je        near ptr M09_L26
       mov       rcx,[rbx+18]
       cmp       qword ptr [rcx+10],50
       jle       short M09_L02
       mov       rbp,[rcx+50]
       test      rbp,rbp
       je        short M09_L02
M09_L00:
       mov       rcx,rbp
       mov       rdx,rsi
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       r14,rax
       test      r14,r14
       je        short M09_L04
       mov       rcx,[rbx+18]
       cmp       qword ptr [rcx+10],98
       jle       short M09_L03
       mov       r8,[rcx+98]
       test      r8,r8
       je        short M09_L03
M09_L01:
       mov       rdx,rbp
       mov       rcx,r14
       call      CORINFO_HELP_VIRTUAL_FUNC_PTR
       mov       rcx,r14
       mov       rdx,rdi
       add       rsp,28
       pop       rbx
       pop       rbp
       pop       rsi
       pop       rdi
       pop       r14
       pop       r15
       jmp       rax
M09_L02:
       mov       rcx,rbx
       mov       rdx,7FFA3EC29940
       call      CORINFO_HELP_RUNTIMEHANDLE_METHOD
       mov       rbp,rax
       jmp       short M09_L00
M09_L03:
       mov       rcx,rbx
       mov       rdx,7FFA3EC2B260
       call      CORINFO_HELP_RUNTIMEHANDLE_METHOD
       mov       r8,rax
       jmp       short M09_L01
M09_L04:
       mov       rcx,[rbx+18]
       cmp       qword ptr [rcx+10],58
       jle       near ptr M09_L10
       mov       rcx,[rcx+58]
       test      rcx,rcx
       je        near ptr M09_L10
M09_L05:
       mov       rdx,rsi
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfInterface(Void*, System.Object)
       mov       rbp,rax
       test      rbp,rbp
       je        near ptr M09_L23
       mov       rcx,[rbx+18]
       cmp       qword ptr [rcx+10],68
       jle       near ptr M09_L11
       mov       rcx,[rcx+68]
       test      rcx,rcx
       je        near ptr M09_L11
M09_L06:
       mov       rdx,rsi
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfAny(Void*, System.Object)
       mov       r14,rax
       test      r14,r14
       jne       near ptr M09_L17
       mov       rcx,[rbx+18]
       cmp       qword ptr [rcx+10],70
       jle       near ptr M09_L12
       mov       rcx,[rcx+70]
       test      rcx,rcx
       je        near ptr M09_L12
M09_L07:
       mov       rdx,rsi
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       r14,rax
       test      r14,r14
       je        near ptr M09_L14
       mov       rcx,[rbx+18]
       cmp       qword ptr [rcx+10],80
       jle       near ptr M09_L13
       mov       rcx,[rcx+80]
       test      rcx,rcx
       je        near ptr M09_L13
M09_L08:
       call      CORINFO_HELP_NEWSFAST
       mov       r15,rax
       call      CORINFO_HELP_GETCURRENTMANAGEDTHREADID
       mov       [r15+10],eax
       lea       rcx,[r15+18]
       mov       rdx,r14
       call      CORINFO_HELP_ASSIGN_REF
       lea       rcx,[r15+20]
       mov       rdx,rdi
       call      CORINFO_HELP_ASSIGN_REF
       mov       rax,r15
M09_L09:
       add       rsp,28
       pop       rbx
       pop       rbp
       pop       rsi
       pop       rdi
       pop       r14
       pop       r15
       ret
M09_L10:
       mov       rcx,rbx
       mov       rdx,7FFA3EC29C10
       call      CORINFO_HELP_RUNTIMEHANDLE_METHOD
       mov       rcx,rax
       jmp       near ptr M09_L05
M09_L11:
       mov       rcx,rbx
       mov       rdx,7FFA3EC2A350
       call      CORINFO_HELP_RUNTIMEHANDLE_METHOD
       mov       rcx,rax
       jmp       near ptr M09_L06
M09_L12:
       mov       rcx,rbx
       mov       rdx,7FFA3EC2A370
       call      CORINFO_HELP_RUNTIMEHANDLE_METHOD
       mov       rcx,rax
       jmp       near ptr M09_L07
M09_L13:
       mov       rcx,rbx
       mov       rdx,7FFA3EC2AAF8
       call      CORINFO_HELP_RUNTIMEHANDLE_METHOD
       mov       rcx,rax
       jmp       near ptr M09_L08
M09_L14:
       mov       rcx,[rbx+18]
       cmp       qword ptr [rcx+10],78
       jle       short M09_L15
       mov       rcx,[rcx+78]
       test      rcx,rcx
       je        short M09_L15
       jmp       short M09_L16
M09_L15:
       mov       rcx,rbx
       mov       rdx,7FFA3EC2A9F8
       call      CORINFO_HELP_RUNTIMEHANDLE_METHOD
       mov       rcx,rax
M09_L16:
       call      CORINFO_HELP_NEWSFAST
       mov       rbx,rax
       mov       rcx,rbx
       mov       rdx,rbp
       mov       r8,rdi
       call      qword ptr [7FFA3EC0D3B0]
       mov       rax,rbx
       jmp       near ptr M09_L09
M09_L17:
       cmp       dword ptr [r14+8],0
       jne       short M09_L20
       mov       rcx,[rbx+18]
       cmp       qword ptr [rcx+10],90
       jle       short M09_L18
       mov       rcx,[rcx+90]
       test      rcx,rcx
       je        short M09_L18
       jmp       short M09_L19
M09_L18:
       mov       rcx,rbx
       mov       rdx,7FFA3EC2B008
       call      CORINFO_HELP_RUNTIMEHANDLE_METHOD
       mov       rcx,rax
M09_L19:
       add       rsp,28
       pop       rbx
       pop       rbp
       pop       rsi
       pop       rdi
       pop       r14
       pop       r15
       jmp       qword ptr [7FFA3EA772D0]; System.Array.Empty[[System.__Canon, System.Private.CoreLib]]()
M09_L20:
       mov       rcx,[rbx+18]
       cmp       qword ptr [rcx+10],88
       jle       short M09_L21
       mov       rcx,[rcx+88]
       test      rcx,rcx
       je        short M09_L21
       jmp       short M09_L22
M09_L21:
       mov       rcx,rbx
       mov       rdx,7FFA3EC2AFC0
       call      CORINFO_HELP_RUNTIMEHANDLE_METHOD
       mov       rcx,rax
M09_L22:
       call      CORINFO_HELP_NEWSFAST
       mov       rbx,rax
       mov       rcx,rbx
       mov       rdx,r14
       mov       r8,rdi
       call      qword ptr [7FFA3EC0E268]
       mov       rax,rbx
       jmp       near ptr M09_L09
M09_L23:
       mov       rcx,[rbx+18]
       cmp       qword ptr [rcx+10],60
       jle       short M09_L24
       mov       rcx,[rcx+60]
       test      rcx,rcx
       je        short M09_L24
       jmp       short M09_L25
M09_L24:
       mov       rcx,rbx
       mov       rdx,7FFA3EC2A338
       call      CORINFO_HELP_RUNTIMEHANDLE_METHOD
       mov       rcx,rax
M09_L25:
       call      CORINFO_HELP_NEWSFAST
       mov       rbx,rax
       mov       rcx,rbx
       mov       rdx,rsi
       mov       r8,rdi
       call      qword ptr [7FFA3EC0E280]
       mov       rax,rbx
       jmp       near ptr M09_L09
M09_L26:
       mov       ecx,10
       call      qword ptr [7FFA3E75F738]
       int       3
M09_L27:
       mov       ecx,11
       call      qword ptr [7FFA3E75F738]
       int       3
; Total bytes of code 852
```
```assembly
; System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       test      rdx,rdx
       je        short M10_L02
       cmp       [rdx],rcx
       je        short M10_L02
       mov       rax,[rdx]
       mov       r8,[rax+10]
M10_L00:
       cmp       r8,rcx
       je        short M10_L02
       test      r8,r8
       je        short M10_L01
       mov       r8,[r8+10]
       cmp       r8,rcx
       je        short M10_L02
       test      r8,r8
       je        short M10_L01
       mov       r8,[r8+10]
       cmp       r8,rcx
       je        short M10_L02
       test      r8,r8
       jne       short M10_L03
M10_L01:
       xor       edx,edx
M10_L02:
       mov       rax,rdx
       ret
M10_L03:
       mov       r8,[r8+10]
       cmp       r8,rcx
       je        short M10_L02
       test      r8,r8
       je        short M10_L01
       mov       r8,[r8+10]
       jmp       short M10_L00
; Total bytes of code 81
```
```assembly
; Stateless.StateMachine`2+StateRepresentation[[Benchmark.HsmState, Benchmark],[Benchmark.HsmTrigger, Benchmark]].TryFindLocalHandlerResult(Benchmark.HsmTrigger, System.Collections.Generic.IEnumerable`1<TriggerBehaviourResult<Benchmark.HsmState,Benchmark.HsmTrigger>>)
       push      r15
       push      r14
       push      r13
       push      r12
       push      rdi
       push      rsi
       push      rbp
       push      rbx
       sub       rsp,198
       xor       eax,eax
       mov       [rsp+28],rax
       vxorps    xmm4,xmm4,xmm4
       vmovdqu32 [rsp+30],zmm4
       vmovdqu32 [rsp+70],zmm4
       vmovdqu32 [rsp+0B0],zmm4
       vmovdqu32 [rsp+0F0],zmm4
       vmovdqu32 [rsp+130],zmm4
       vmovdqa   xmmword ptr [rsp+170],xmm4
       vmovdqa   xmmword ptr [rsp+180],xmm4
       mov       [rsp+190],rax
       mov       rsi,rcx
       mov       edi,edx
       mov       rbx,r8
       mov       rdx,202C7C01510
       mov       rbp,[rdx]
       test      rbp,rbp
       je        near ptr M11_L21
M11_L00:
       test      rbx,rbx
       je        near ptr M11_L39
       test      rbp,rbp
       je        near ptr M11_L38
       mov       rdx,rbx
       mov       rcx,offset MT_System.Linq.Enumerable+Iterator<Stateless.StateMachine<Benchmark.HsmState, Benchmark.HsmTrigger>+TriggerBehaviourResult>
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       test      rax,rax
       jne       near ptr M11_L24
       mov       rdx,rbx
       mov       rcx,offset MT_Stateless.StateMachine<Benchmark.HsmState, Benchmark.HsmTrigger>+TriggerBehaviourResult[]
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfAny(Void*, System.Object)
       mov       r14,rax
       test      r14,r14
       je        near ptr M11_L10
       cmp       dword ptr [r14+8],0
       je        near ptr M11_L22
       mov       rcx,offset MT_System.Linq.Enumerable+ArrayWhereIterator<Stateless.StateMachine<Benchmark.HsmState, Benchmark.HsmTrigger>+TriggerBehaviourResult>
       call      CORINFO_HELP_NEWSFAST
       mov       r15,rax
       call      CORINFO_HELP_GETCURRENTMANAGEDTHREADID
       mov       [r15+10],eax
       lea       rcx,[r15+18]
       mov       rdx,r14
       call      CORINFO_HELP_ASSIGN_REF
       lea       rcx,[r15+20]
       mov       rdx,rbp
       call      CORINFO_HELP_ASSIGN_REF
M11_L01:
       test      r15,r15
       je        near ptr M11_L39
       mov       rdx,r15
       mov       rcx,offset MT_System.Linq.Enumerable+Iterator<Stateless.StateMachine<Benchmark.HsmState, Benchmark.HsmTrigger>+TriggerBehaviourResult>
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       test      rax,rax
       je        near ptr M11_L35
       mov       rdx,offset MT_System.Linq.Enumerable+ArrayWhereIterator<Stateless.StateMachine<Benchmark.HsmState, Benchmark.HsmTrigger>+TriggerBehaviourResult>
       cmp       [rax],rdx
       jne       near ptr M11_L20
       mov       rdx,[rax+18]
       test      rdx,rdx
       je        near ptr M11_L11
       lea       r14,[rdx+10]
       mov       r13d,[rdx+8]
M11_L02:
       mov       r12,[rax+20]
       vxorps    ymm0,ymm0,ymm0
       vmovdqu32 [rsp+158],zmm0
       vxorps    ymm0,ymm0,ymm0
       vmovdqu32 [rsp+60],zmm0
       vmovdqu32 [rsp+0A0],zmm0
       vmovdqu32 [rsp+0E0],zmm0
       vmovdqu   ymmword ptr [rsp+118],ymm0
       xor       edx,edx
       mov       [rsp+50],edx
       mov       [rsp+54],edx
       mov       [rsp+58],edx
       lea       rdx,[rsp+158]
       mov       [rsp+138],rdx
       mov       dword ptr [rsp+140],8
       lea       rdx,[rsp+158]
       mov       [rsp+148],rdx
       mov       dword ptr [rsp+150],8
       test      r13d,r13d
       jle       near ptr M11_L05
       test      r12,r12
       je        near ptr M11_L13
       mov       rdx,offset Stateless.StateMachine`2+StateRepresentation+<>c[[Benchmark.HsmState, Benchmark],[Benchmark.HsmTrigger, Benchmark]].<TryFindLocalHandlerResult>b__42_0(TriggerBehaviourResult<Benchmark.HsmState,Benchmark.HsmTrigger>)
       cmp       [r12+18],rdx
       jne       near ptr M11_L13
       xor       r12d,r12d
M11_L03:
       mov       r15,[r14+r12]
       mov       rdx,[r15+10]
       mov       rcx,7FFA3EC327B8
       call      qword ptr [7FFA3EC04FC0]; System.Linq.Enumerable.Any[[System.__Canon, System.Private.CoreLib]](System.Collections.Generic.IEnumerable`1<System.__Canon>)
       test      eax,eax
       jne       short M11_L04
       mov       rax,[rsp+148]
       mov       r8d,[rsp+150]
       mov       r10d,[rsp+58]
       cmp       r10d,r8d
       jae       near ptr M11_L12
       mov       ecx,r10d
       lea       rcx,[rax+rcx*8]
       mov       rdx,r15
       call      CORINFO_HELP_CHECKED_ASSIGN_REF
       mov       ecx,[rsp+58]
       inc       ecx
       mov       [rsp+58],ecx
M11_L04:
       add       r12,8
       dec       r13d
       jne       short M11_L03
M11_L05:
       mov       r14d,[rsp+54]
       add       r14d,[rsp+58]
       jo        near ptr M11_L40
       test      r14d,r14d
       jne       near ptr M11_L17
       mov       rcx,offset MT_System.Collections.Generic.List<Stateless.StateMachine<Benchmark.HsmState, Benchmark.HsmTrigger>+TriggerBehaviourResult>
       call      CORINFO_HELP_NEWSFAST
       mov       r14,rax
       test      byte ptr [7FFA3EC36558],1
       je        near ptr M11_L27
M11_L06:
       mov       rcx,202C7C01568
       mov       rdx,[rcx]
       lea       rcx,[r14+8]
       call      CORINFO_HELP_ASSIGN_REF
M11_L07:
       mov       r8d,[rsp+50]
       test      r8d,r8d
       jne       near ptr M11_L19
M11_L08:
       cmp       dword ptr [r14+10],1
       jg        near ptr M11_L37
       mov       rdx,r14
       mov       rcx,offset MT_System.Linq.Enumerable+Iterator<Stateless.StateMachine<Benchmark.HsmState, Benchmark.HsmTrigger>+TriggerBehaviourResult>
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       test      rax,rax
       jne       near ptr M11_L36
       lea       r8,[rsp+38]
       mov       rdx,r14
       mov       rcx,7FFA3EC38420
       call      qword ptr [7FFA3EC05C80]; System.Linq.Enumerable.TryGetFirstNonIterator[[System.__Canon, System.Private.CoreLib]](System.Collections.Generic.IEnumerable`1<System.__Canon>, Boolean ByRef)
M11_L09:
       nop
       add       rsp,198
       pop       rbx
       pop       rbp
       pop       rsi
       pop       rdi
       pop       r12
       pop       r13
       pop       r14
       pop       r15
       ret
M11_L10:
       mov       rdx,rbx
       mov       rcx,offset MT_System.Collections.Generic.List<Stateless.StateMachine<Benchmark.HsmState, Benchmark.HsmTrigger>+TriggerBehaviourResult>
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       r15,rax
       test      r15,r15
       je        near ptr M11_L23
       mov       rcx,offset MT_System.Linq.Enumerable+ListWhereIterator<Stateless.StateMachine<Benchmark.HsmState, Benchmark.HsmTrigger>+TriggerBehaviourResult>
       call      CORINFO_HELP_NEWSFAST
       mov       r13,rax
       call      CORINFO_HELP_GETCURRENTMANAGEDTHREADID
       mov       [r13+10],eax
       lea       rcx,[r13+18]
       mov       rdx,r15
       call      CORINFO_HELP_ASSIGN_REF
       lea       rcx,[r13+20]
       mov       rdx,rbp
       call      CORINFO_HELP_ASSIGN_REF
       mov       r15,r13
       jmp       near ptr M11_L01
M11_L11:
       xor       r14d,r14d
       xor       r13d,r13d
       jmp       near ptr M11_L02
M11_L12:
       lea       rcx,[rsp+50]
       mov       r8,r15
       mov       rdx,offset MT_System.Collections.Generic.SegmentedArrayBuilder<Stateless.StateMachine<Benchmark.HsmState, Benchmark.HsmTrigger>+TriggerBehaviourResult>
       call      qword ptr [7FFA3EC0DEC0]
       jmp       near ptr M11_L04
M11_L13:
       xor       ebp,ebp
       mov       ebx,r13d
M11_L14:
       mov       r15,[r14+rbp]
       mov       rdx,offset Stateless.StateMachine`2+StateRepresentation+<>c[[Benchmark.HsmState, Benchmark],[Benchmark.HsmTrigger, Benchmark]].<TryFindLocalHandlerResult>b__42_0(TriggerBehaviourResult<Benchmark.HsmState,Benchmark.HsmTrigger>)
       cmp       [r12+18],rdx
       jne       near ptr M11_L25
       mov       rdx,[r15+10]
       mov       rcx,7FFA3EC327B8
       call      qword ptr [7FFA3EC04FC0]; System.Linq.Enumerable.Any[[System.__Canon, System.Private.CoreLib]](System.Collections.Generic.IEnumerable`1<System.__Canon>)
       test      eax,eax
       jne       short M11_L16
M11_L15:
       mov       rax,[rsp+148]
       mov       r8d,[rsp+150]
       mov       r10d,[rsp+58]
       cmp       r10d,r8d
       jae       near ptr M11_L26
       mov       ecx,r10d
       lea       rcx,[rax+rcx*8]
       mov       rdx,r15
       call      CORINFO_HELP_CHECKED_ASSIGN_REF
       mov       ecx,[rsp+58]
       inc       ecx
       mov       [rsp+58],ecx
M11_L16:
       add       rbp,8
       dec       ebx
       jne       short M11_L14
       jmp       near ptr M11_L05
M11_L17:
       mov       rcx,offset MT_System.Collections.Generic.List<Stateless.StateMachine<Benchmark.HsmState, Benchmark.HsmTrigger>+TriggerBehaviourResult>
       call      CORINFO_HELP_NEWSFAST
       mov       r15,rax
       mov       rcx,r15
       mov       edx,r14d
       call      qword ptr [7FFA3EC05BD8]; System.Collections.Generic.List`1[[System.__Canon, System.Private.CoreLib]]..ctor(Int32)
       mov       r8d,r14d
       mov       rdx,r15
       mov       rcx,7FFA3EC381D8
       call      qword ptr [7FFA3EC05BF0]; System.Runtime.InteropServices.CollectionsMarshal.SetCount[[System.__Canon, System.Private.CoreLib]](System.Collections.Generic.List`1<System.__Canon>, Int32)
       mov       r14d,[r15+10]
       mov       r13,[r15+8]
       cmp       [r13+8],r14d
       jb        near ptr M11_L34
       add       r13,10
       mov       rbx,r13
       mov       ebp,r14d
       mov       r12d,[rsp+50]
       test      r12d,r12d
       jne       near ptr M11_L28
M11_L18:
       mov       r8d,[rsp+58]
       cmp       r8d,[rsp+150]
       ja        near ptr M11_L32
       mov       rdx,[rsp+148]
       cmp       r8d,ebp
       ja        near ptr M11_L33
       shl       r8,3
       mov       rcx,rbx
       call      qword ptr [7FFA3E755740]; System.Buffer.BulkMoveWithWriteBarrier(Byte ByRef, Byte ByRef, UIntPtr)
       mov       r14,r15
       jmp       near ptr M11_L07
M11_L19:
       lea       rcx,[rsp+50]
       mov       rdx,offset MT_System.Collections.Generic.SegmentedArrayBuilder<Stateless.StateMachine<Benchmark.HsmState, Benchmark.HsmTrigger>+TriggerBehaviourResult>
       call      qword ptr [7FFA3EC0DED8]
       jmp       near ptr M11_L08
M11_L20:
       mov       rcx,rax
       mov       rax,[rax]
       mov       rax,[rax+40]
       call      qword ptr [rax+28]
       mov       r14,rax
       jmp       near ptr M11_L08
M11_L21:
       mov       rcx,offset MT_System.Func<Stateless.StateMachine<Benchmark.HsmState, Benchmark.HsmTrigger>+TriggerBehaviourResult, System.Boolean>
       call      CORINFO_HELP_NEWSFAST
       mov       rbp,rax
       mov       rdx,202C7C01508
       mov       rdx,[rdx]
       mov       rcx,rbp
       mov       r8,offset Stateless.StateMachine`2+StateRepresentation+<>c[[Benchmark.HsmState, Benchmark],[Benchmark.HsmTrigger, Benchmark]].<TryFindLocalHandlerResult>b__42_0(TriggerBehaviourResult<Benchmark.HsmState,Benchmark.HsmTrigger>)
       call      qword ptr [7FFA3E7569D0]; System.MulticastDelegate.CtorClosed(System.Object, IntPtr)
       mov       rcx,202C7C01510
       mov       rdx,rbp
       call      CORINFO_HELP_ASSIGN_REF
       jmp       near ptr M11_L00
M11_L22:
       mov       rcx,offset MT_System.Array+EmptyArray<Stateless.StateMachine<Benchmark.HsmState, Benchmark.HsmTrigger>+TriggerBehaviourResult>
       call      CORINFO_HELP_GET_GCSTATIC_BASE
       mov       rcx,202C7C01570
       mov       r15,[rcx]
       jmp       near ptr M11_L01
M11_L23:
       mov       rcx,offset MT_System.Linq.Enumerable+IEnumerableWhereIterator<Stateless.StateMachine<Benchmark.HsmState, Benchmark.HsmTrigger>+TriggerBehaviourResult>
       call      CORINFO_HELP_NEWSFAST
       mov       r15,rax
       mov       rcx,r15
       mov       rdx,rbx
       mov       r8,rbp
       call      qword ptr [7FFA3EC0E298]
       jmp       near ptr M11_L01
M11_L24:
       mov       rcx,rax
       mov       rdx,rbp
       mov       rax,[rax]
       mov       rax,[rax+50]
       call      qword ptr [rax+8]
       mov       r15,rax
       jmp       near ptr M11_L01
M11_L25:
       mov       rdx,r15
       mov       rcx,[r12+8]
       call      qword ptr [r12+18]
       test      eax,eax
       je        near ptr M11_L16
       jmp       near ptr M11_L15
M11_L26:
       lea       rcx,[rsp+50]
       mov       r8,r15
       mov       rdx,offset MT_System.Collections.Generic.SegmentedArrayBuilder<Stateless.StateMachine<Benchmark.HsmState, Benchmark.HsmTrigger>+TriggerBehaviourResult>
       call      qword ptr [7FFA3EC0DEC0]
       jmp       near ptr M11_L16
M11_L27:
       mov       rcx,offset MT_System.Collections.Generic.List<Stateless.StateMachine<Benchmark.HsmState, Benchmark.HsmTrigger>+TriggerBehaviourResult>
       call      CORINFO_HELP_GET_GCSTATIC_BASE
       jmp       near ptr M11_L06
M11_L28:
       vmovdqu   xmm0,xmmword ptr [rsp+138]
       vmovdqu   xmmword ptr [rsp+28],xmm0
       lea       r8,[rsp+28]
       lea       rcx,[rsp+40]
       mov       rdx,offset MT_System.Span<Stateless.StateMachine<Benchmark.HsmState, Benchmark.HsmTrigger>+TriggerBehaviourResult>
       call      qword ptr [7FFA3EC05590]; System.Span`1[[System.__Canon, System.Private.CoreLib]].op_Implicit(System.Span`1<System.__Canon>)
       mov       ebx,[rsp+48]
       cmp       ebx,r14d
       ja        near ptr M11_L33
       mov       ebp,ebx
       shl       rbp,3
       mov       r8,rbp
       mov       rcx,r13
       mov       rdx,[rsp+40]
       call      qword ptr [7FFA3E755740]; System.Buffer.BulkMoveWithWriteBarrier(Byte ByRef, Byte ByRef, UIntPtr)
       add       rbp,r13
       sub       r14d,ebx
       mov       rbx,rbp
       mov       ebp,r14d
       dec       r12d
       je        near ptr M11_L18
       cmp       r12d,1B
       ja        short M11_L32
       mov       r14d,r12d
       test      r14d,r14d
       jle       near ptr M11_L18
       xor       r13d,r13d
M11_L29:
       lea       r8,[rsp+60]
       mov       r8,[r8+r13]
       test      r8,r8
       je        short M11_L30
       lea       rdx,[r8+10]
       mov       r12d,[r8+8]
       jmp       short M11_L31
M11_L30:
       xor       edx,edx
       xor       r12d,r12d
M11_L31:
       cmp       r12d,ebp
       ja        short M11_L33
       mov       eax,r12d
       shl       rax,3
       mov       [rsp+20],rax
       mov       r8,rax
       mov       rcx,rbx
       call      qword ptr [7FFA3E755740]; System.Buffer.BulkMoveWithWriteBarrier(Byte ByRef, Byte ByRef, UIntPtr)
       mov       rcx,[rsp+20]
       add       rbx,rcx
       sub       ebp,r12d
       add       r13,8
       dec       r14d
       jne       short M11_L29
       jmp       near ptr M11_L18
M11_L32:
       call      qword ptr [7FFA3E967798]
       int       3
M11_L33:
       call      qword ptr [7FFA3EC0D8D8]
       int       3
M11_L34:
       call      qword ptr [7FFA3E75F2A0]
       int       3
M11_L35:
       mov       rcx,offset MT_System.Collections.Generic.List<Stateless.StateMachine<Benchmark.HsmState, Benchmark.HsmTrigger>+TriggerBehaviourResult>
       call      CORINFO_HELP_NEWSFAST
       mov       r14,rax
       mov       rcx,r14
       mov       rdx,r15
       call      qword ptr [7FFA3EB05950]; System.Collections.Generic.List`1[[System.__Canon, System.Private.CoreLib]]..ctor(System.Collections.Generic.IEnumerable`1<System.__Canon>)
       jmp       near ptr M11_L08
M11_L36:
       lea       rdx,[rsp+38]
       mov       rcx,rax
       mov       rax,[rax]
       mov       rax,[rax+48]
       call      qword ptr [rax+10]
       jmp       near ptr M11_L09
M11_L37:
       mov       rcx,offset MT_Benchmark.HsmTrigger
       call      CORINFO_HELP_NEWSFAST
       mov       rbx,rax
       call      qword ptr [7FFA3EC05B30]
       mov       rbp,rax
       mov       [rbx+8],edi
       mov       rcx,offset MT_Benchmark.HsmState
       call      CORINFO_HELP_NEWSFAST
       mov       rdi,rax
       mov       ecx,[rsi+40]
       mov       [rdi+8],ecx
       mov       rcx,offset MT_System.InvalidOperationException
       call      CORINFO_HELP_NEWSFAST
       mov       rsi,rax
       mov       r8,rdi
       mov       rdx,rbx
       mov       rcx,rbp
       call      qword ptr [7FFA3EC05B48]
       mov       rdx,rax
       mov       rcx,rsi
       call      qword ptr [7FFA3EA76C70]
       mov       rcx,rsi
       call      CORINFO_HELP_THROW
       int       3
M11_L38:
       mov       ecx,0C
       call      qword ptr [7FFA3E75F738]
       int       3
M11_L39:
       mov       ecx,11
       call      qword ptr [7FFA3E75F738]
       int       3
M11_L40:
       call      CORINFO_HELP_OVERFLOW
       int       3
; Total bytes of code 1907
```
```assembly
; System.Enum.Equals(System.Object)
       push      rdi
       push      rsi
       push      rbx
       sub       rsp,20
       mov       rbx,rcx
M12_L00:
       test      rdx,rdx
       je        near ptr M12_L03
       cmp       rbx,rdx
       je        short M12_L02
       mov       rcx,[rbx]
       cmp       rcx,[rdx]
       jne       short M12_L03
       lea       rsi,[rbx+8]
       lea       rdi,[rdx+8]
       mov       rcx,[rbx]
       call      System.Runtime.CompilerServices.MethodTable.GetPrimitiveCorElementType()
       add       eax,0FFFFFFFE
       cmp       eax,17
       ja        short M12_L03
       lea       rcx,[7FFA3E81CFA8]
       mov       ecx,[rcx+rax*4]
       lea       rdx,[M12_L00]
       add       rcx,rdx
       jmp       rcx
       mov       eax,[rsi]
       cmp       eax,[rdi]
       sete      cl
       movzx     ecx,cl
M12_L01:
       movzx     eax,cl
       add       rsp,20
       pop       rbx
       pop       rsi
       pop       rdi
       ret
       mov       rcx,[rsi]
       cmp       rcx,[rdi]
       sete      cl
       movzx     ecx,cl
       jmp       short M12_L01
       movzx     ecx,word ptr [rsi]
       cmp       cx,[rdi]
       sete      cl
       movzx     ecx,cl
       jmp       short M12_L01
       movzx     ecx,byte ptr [rsi]
       cmp       cl,[rdi]
       sete      cl
       movzx     ecx,cl
       jmp       short M12_L01
M12_L02:
       mov       eax,1
       add       rsp,20
       pop       rbx
       pop       rsi
       pop       rdi
       ret
M12_L03:
       xor       eax,eax
       add       rsp,20
       pop       rbx
       pop       rsi
       pop       rdi
       ret
; Total bytes of code 163
```
```assembly
; Stateless.StateMachine`2[[Benchmark.HsmState, Benchmark],[Benchmark.HsmTrigger, Benchmark]].HandleTransitioningTrigger(System.Object[], StateRepresentation<Benchmark.HsmState,Benchmark.HsmTrigger>, Transition<Benchmark.HsmState,Benchmark.HsmTrigger>)
       push      r15
       push      r14
       push      r13
       push      r12
       push      rdi
       push      rsi
       push      rbp
       push      rbx
       sub       rsp,38
       xor       eax,eax
       mov       [rsp+30],rax
       mov       rsi,rcx
       mov       rbp,rdx
       mov       rdi,r8
       mov       rbx,r9
       cmp       [rdi],dil
       mov       r14d,[rbx+10]
       mov       r15,offset MT_Benchmark.HsmState
       mov       rcx,r15
       call      CORINFO_HELP_NEWSFAST
       mov       r13,rax
       mov       ecx,[rbx+14]
       mov       [r13+8],ecx
       mov       rcx,r15
       call      CORINFO_HELP_NEWSFAST
       mov       [rax+8],r14d
       mov       rcx,rax
       mov       rdx,r13
       call      qword ptr [7FFA3E6A6098]; System.Enum.Equals(System.Object)
       test      eax,eax
       jne       near ptr M13_L12
       mov       r14d,[rbx+14]
       mov       rcx,offset MT_Stateless.StateMachine<Benchmark.HsmState, Benchmark.HsmTrigger>+StateRepresentation+<>c__DisplayClass65_0
       call      CORINFO_HELP_NEWSFAST
       mov       r13,rax
       mov       [r13+8],r14d
       mov       r14d,[rdi+40]
       mov       rcx,r15
       call      CORINFO_HELP_NEWSFAST
       mov       r12,rax
       mov       ecx,[r13+8]
       mov       [r12+8],ecx
       mov       rcx,r15
       call      CORINFO_HELP_NEWSFAST
       mov       [rax+8],r14d
       mov       rcx,rax
       mov       rdx,r12
       call      qword ptr [7FFA3E6A6098]; System.Enum.Equals(System.Object)
       test      eax,eax
       jne       short M13_L00
       mov       rcx,offset MT_System.Func<Stateless.StateMachine<Benchmark.HsmState, Benchmark.HsmTrigger>+StateRepresentation, System.Boolean>
       call      CORINFO_HELP_NEWSFAST
       mov       r14,rax
       mov       r15,[rdi+38]
       lea       rcx,[r14+8]
       mov       rdx,r13
       call      CORINFO_HELP_ASSIGN_REF
       mov       r8,offset Stateless.StateMachine`2+StateRepresentation+<>c__DisplayClass65_0[[Benchmark.HsmState, Benchmark],[Benchmark.HsmTrigger, Benchmark]].<Includes>b__0(StateRepresentation<Benchmark.HsmState,Benchmark.HsmTrigger>)
       mov       [r14+18],r8
       mov       r8,r14
       mov       rdx,r15
       mov       rcx,7FFA3EC38BF0
       call      qword ptr [7FFA3EB06B08]; System.Linq.Enumerable.Any[[System.__Canon, System.Private.CoreLib]](System.Collections.Generic.IEnumerable`1<System.__Canon>, System.Func`2<System.__Canon,Boolean>)
       test      eax,eax
       jne       short M13_L00
       mov       rcx,rdi
       mov       rdx,rbx
       call      qword ptr [7FFA3EC05DA0]; Stateless.StateMachine`2+StateRepresentation[[Benchmark.HsmState, Benchmark],[Benchmark.HsmTrigger, Benchmark]].ExecuteExitActions(Transition<Benchmark.HsmState,Benchmark.HsmTrigger>)
       cmp       qword ptr [rdi+30],0
       jne       near ptr M13_L11
M13_L00:
       mov       edx,[rbx+14]
       mov       r14,[rsi+20]
       mov       rcx,offset Stateless.StateMachine`2+<>c__DisplayClass46_0[[Benchmark.HsmState, Benchmark],[Benchmark.HsmTrigger, Benchmark]].<.ctor>b__1(Benchmark.HsmState)
       cmp       [r14+18],rcx
       jne       near ptr M13_L19
       mov       rcx,[r14+8]
       mov       rcx,[rcx+8]
       mov       [rcx+8],edx
M13_L01:
       mov       r15d,[rbx+14]
       mov       r13,[rsi+8]
       mov       rcx,offset MT_System.Collections.Generic.Dictionary<Benchmark.HsmState, Stateless.StateMachine<Benchmark.HsmState, Benchmark.HsmTrigger>+StateRepresentation>
       cmp       [r13],rcx
       jne       near ptr M13_L26
       mov       edx,r15d
       cmp       qword ptr [r13+8],0
       je        near ptr M13_L15
       mov       r12,[r13+18]
       test      r12,r12
       jne       near ptr M13_L21
       mov       rcx,[r13+8]
       mov       edx,edx
       imul      rdx,[r13+30]
       shr       rdx,20
       inc       rdx
       mov       r8d,[rcx+8]
       imul      rdx,r8
       shr       rdx,20
       cmp       edx,[rcx+8]
       jae       near ptr M13_L36
       mov       edx,edx
       lea       rcx,[rcx+rdx*4+10]
       mov       edi,[rcx]
       mov       r14,[r13+10]
       xor       r12d,r12d
       dec       edi
       mov       r13d,[r14+8]
M13_L02:
       cmp       r13d,edi
       jbe       near ptr M13_L15
       mov       ecx,edi
       lea       rcx,[rcx+rcx*2]
       lea       rdi,[r14+rcx*8+10]
       cmp       [rdi+8],r15d
       jne       near ptr M13_L20
       cmp       [rdi+10],r15d
       jne       near ptr M13_L20
M13_L03:
       test      rdi,rdi
       je        near ptr M13_L25
       mov       rcx,[rdi]
       mov       [rsp+30],rcx
M13_L04:
       mov       rdi,[rsp+30]
       xor       ecx,ecx
       mov       [rsp+30],rcx
       mov       rcx,[rsi+30]
       mov       rdx,[rcx+10]
       cmp       dword ptr [rdx+10],0
       jne       near ptr M13_L35
       mov       rax,[rcx+8]
       test      rax,rax
       jne       near ptr M13_L28
M13_L05:
       mov       rcx,rsi
       mov       rdx,rdi
       mov       r8,rbx
       mov       r9,rbp
       call      qword ptr [7FFA3EC05D28]; Stateless.StateMachine`2[[Benchmark.HsmState, Benchmark],[Benchmark.HsmTrigger, Benchmark]].EnterState(StateRepresentation<Benchmark.HsmState,Benchmark.HsmTrigger>, Transition<Benchmark.HsmState,Benchmark.HsmTrigger>, System.Object[])
       mov       rdi,rax
       mov       ebp,[rdi+40]
       mov       rax,[rsi+18]
       mov       rcx,offset Stateless.StateMachine`2+<>c__DisplayClass46_0[[Benchmark.HsmState, Benchmark],[Benchmark.HsmTrigger, Benchmark]].<.ctor>b__0()
       cmp       [rax+18],rcx
       jne       near ptr M13_L29
       mov       rcx,[rax+8]
       mov       rcx,[rcx+8]
       mov       r14d,[rcx+8]
M13_L06:
       mov       r13,offset MT_Benchmark.HsmState
       mov       rcx,r13
       call      CORINFO_HELP_NEWSFAST
       mov       r12,rax
       mov       [r12+8],r14d
       mov       rcx,r13
       call      CORINFO_HELP_NEWSFAST
       mov       [rax+8],ebp
       mov       rcx,rax
       mov       rdx,r12
       call      qword ptr [7FFA3E6A6098]; System.Enum.Equals(System.Object)
       test      eax,eax
       je        near ptr M13_L16
M13_L07:
       mov       r13,[rsi+38]
       mov       r12d,[rbx+10]
       mov       rax,[rsi+18]
       mov       rcx,offset Stateless.StateMachine`2+<>c__DisplayClass46_0[[Benchmark.HsmState, Benchmark],[Benchmark.HsmTrigger, Benchmark]].<.ctor>b__0()
       cmp       [rax+18],rcx
       jne       near ptr M13_L31
       mov       rcx,[rax+8]
       mov       rcx,[rcx+8]
       mov       ebp,[rcx+8]
M13_L08:
       mov       edi,[rbx+18]
       mov       rcx,offset MT_Stateless.StateMachine<Benchmark.HsmState, Benchmark.HsmTrigger>+Transition
       call      CORINFO_HELP_NEWSFAST
       mov       r14,rax
       mov       rdx,[rbx+8]
       mov       [r14+10],r12d
       mov       [r14+14],ebp
       mov       [r14+18],edi
       test      rdx,rdx
       je        near ptr M13_L32
M13_L09:
       lea       rcx,[r14+8]
       call      CORINFO_HELP_ASSIGN_REF
       mov       rcx,[r13+10]
       cmp       dword ptr [rcx+10],0
       jne       near ptr M13_L34
       mov       rdi,[r13+8]
       test      rdi,rdi
       jne       near ptr M13_L33
M13_L10:
       add       rsp,38
       pop       rbx
       pop       rbp
       pop       rsi
       pop       rdi
       pop       r12
       pop       r13
       pop       r14
       pop       r15
       ret
M13_L11:
       mov       r14d,[rbx+14]
       mov       r15d,[rdi+40]
       mov       rcx,offset MT_Benchmark.HsmState
       mov       r13,rcx
       call      CORINFO_HELP_NEWSFAST
       mov       r12,rax
       mov       [r12+8],r14d
       mov       rcx,r13
       call      CORINFO_HELP_NEWSFAST
       mov       [rax+8],r15d
       mov       rcx,rax
       mov       rdx,r12
       call      qword ptr [7FFA3E6A6098]; System.Enum.Equals(System.Object)
       test      eax,eax
       jne       near ptr M13_L17
       cmp       qword ptr [rdi+30],0
       je        near ptr M13_L18
       mov       rcx,[rdi+30]
       mov       edx,r14d
       cmp       [rcx],ecx
       call      qword ptr [7FFA3EC05DD0]; Stateless.StateMachine`2+StateRepresentation[[Benchmark.HsmState, Benchmark],[Benchmark.HsmTrigger, Benchmark]].IsIncludedIn(Benchmark.HsmState)
       test      eax,eax
       je        near ptr M13_L18
       jmp       short M13_L17
M13_L12:
       mov       rcx,rdi
       mov       rdx,rbx
       call      qword ptr [7FFA3EC05DA0]; Stateless.StateMachine`2+StateRepresentation[[Benchmark.HsmState, Benchmark],[Benchmark.HsmTrigger, Benchmark]].ExecuteExitActions(Transition<Benchmark.HsmState,Benchmark.HsmTrigger>)
       jmp       near ptr M13_L00
M13_L13:
       mov       eax,[r13+8]
       cmp       eax,edx
       jbe       short M13_L15
       mov       edx,edx
       lea       rdx,[rdx+rdx*2]
       lea       rdx,[r13+rdx*8+10]
       mov       r10,rdx
       cmp       [r10+8],edi
       je        near ptr M13_L22
M13_L14:
       mov       edx,[r10+0C]
       inc       r14d
       cmp       eax,r14d
       jae       short M13_L13
       jmp       near ptr M13_L24
M13_L15:
       xor       edi,edi
       jmp       near ptr M13_L03
M13_L16:
       mov       edx,[rdi+40]
       mov       rbp,[rsi+20]
       mov       rcx,offset Stateless.StateMachine`2+<>c__DisplayClass46_0[[Benchmark.HsmState, Benchmark],[Benchmark.HsmTrigger, Benchmark]].<.ctor>b__1(Benchmark.HsmState)
       cmp       [rbp+18],rcx
       jne       near ptr M13_L30
       mov       rcx,[rbp+8]
       mov       rcx,[rcx+8]
       mov       [rcx+8],edx
       jmp       near ptr M13_L07
M13_L17:
       mov       rcx,[rdi+30]
       mov       r15d,[rcx+40]
       mov       r13,offset MT_Benchmark.HsmState
       mov       rcx,r13
       call      CORINFO_HELP_NEWSFAST
       mov       r14,rax
       mov       ecx,[rbx+14]
       mov       [r14+8],ecx
       mov       rcx,r13
       call      CORINFO_HELP_NEWSFAST
       mov       [rax+8],r15d
       mov       rcx,rax
       mov       rdx,r14
       call      qword ptr [7FFA3E6A6098]; System.Enum.Equals(System.Object)
       test      eax,eax
       jne       near ptr M13_L00
M13_L18:
       mov       rcx,[rdi+30]
       mov       rdx,rbx
       cmp       [rcx],ecx
       call      qword ptr [7FFA3EC05CC8]; Stateless.StateMachine`2+StateRepresentation[[Benchmark.HsmState, Benchmark],[Benchmark.HsmTrigger, Benchmark]].Exit(Transition<Benchmark.HsmState,Benchmark.HsmTrigger>)
       mov       rbx,rax
       jmp       near ptr M13_L00
M13_L19:
       mov       rcx,[r14+8]
       call      qword ptr [r14+18]
       jmp       near ptr M13_L01
M13_L20:
       mov       edi,[rdi+0C]
       inc       r12d
       cmp       r13d,r12d
       jae       near ptr M13_L02
       jmp       near ptr M13_L24
M13_L21:
       mov       rcx,r12
       mov       r11,7FFA3E6B0748
       call      qword ptr [r11]
       mov       edi,eax
       mov       rdx,[r13+8]
       mov       ecx,edi
       imul      rcx,[r13+30]
       shr       rcx,20
       inc       rcx
       mov       r8d,[rdx+8]
       imul      rcx,r8
       shr       rcx,20
       cmp       ecx,[rdx+8]
       jae       near ptr M13_L36
       mov       ecx,ecx
       lea       rdx,[rdx+rcx*4+10]
       mov       edx,[rdx]
       mov       r13,[r13+10]
       xor       r14d,r14d
       dec       edx
       jmp       near ptr M13_L13
M13_L22:
       mov       [rsp+2C],eax
       mov       [rsp+20],r10
       mov       edx,[r10+10]
       mov       rcx,r12
       mov       r8d,r15d
       mov       r11,7FFA3E6B0750
       call      qword ptr [r11]
       test      eax,eax
       mov       eax,[rsp+2C]
       jne       short M13_L23
       mov       r10,[rsp+20]
       jmp       near ptr M13_L14
M13_L23:
       mov       rdi,[rsp+20]
       jmp       near ptr M13_L03
M13_L24:
       call      qword ptr [7FFA3E75F2A0]
       int       3
M13_L25:
       xor       r8d,r8d
       mov       [rsp+30],r8
       jmp       short M13_L27
M13_L26:
       lea       r8,[rsp+30]
       mov       rcx,r13
       mov       edx,r15d
       mov       r11,7FFA3E6B0738
       call      qword ptr [r11]
       test      eax,eax
       jne       near ptr M13_L04
M13_L27:
       mov       rcx,offset MT_Stateless.StateMachine<Benchmark.HsmState, Benchmark.HsmTrigger>+StateRepresentation
       call      CORINFO_HELP_NEWSFAST
       mov       rdi,rax
       movzx     r8d,byte ptr [rsi+51]
       mov       rcx,rdi
       mov       edx,r15d
       call      qword ptr [7FFA3EB051A0]; Stateless.StateMachine`2+StateRepresentation[[Benchmark.HsmState, Benchmark],[Benchmark.HsmTrigger, Benchmark]]..ctor(Benchmark.HsmState, Boolean)
       mov       [rsp+30],rdi
       mov       rcx,[rsi+8]
       mov       r8,[rsp+30]
       mov       edx,r15d
       mov       r11,7FFA3E6B0740
       call      qword ptr [r11]
       jmp       near ptr M13_L04
M13_L28:
       mov       rdx,rbx
       mov       rcx,[rax+8]
       call      qword ptr [rax+18]
       jmp       near ptr M13_L05
M13_L29:
       mov       rcx,[rax+8]
       call      qword ptr [rax+18]
       mov       r14d,eax
       jmp       near ptr M13_L06
M13_L30:
       mov       rcx,[rbp+8]
       call      qword ptr [rbp+18]
       jmp       near ptr M13_L07
M13_L31:
       mov       rcx,[rax+8]
       call      qword ptr [rax+18]
       mov       ebp,eax
       jmp       near ptr M13_L08
M13_L32:
       mov       rcx,offset MT_System.Object[]
       xor       edx,edx
       call      CORINFO_HELP_NEWARR_1_OBJ
       mov       rdx,rax
       jmp       near ptr M13_L09
M13_L33:
       mov       rdx,r14
       mov       rcx,[rdi+8]
       call      qword ptr [rdi+18]
       jmp       near ptr M13_L10
M13_L34:
       mov       rcx,offset MT_System.InvalidOperationException
       call      CORINFO_HELP_NEWSFAST
       mov       rbx,rax
       mov       ecx,0ADE
       mov       rdx,7FFA3EB2F550
       call      CORINFO_HELP_STRCNS
       mov       rdx,rax
       mov       rcx,rbx
       call      qword ptr [7FFA3EA76C70]
       mov       rcx,rbx
       call      CORINFO_HELP_THROW
       int       3
M13_L35:
       mov       rcx,offset MT_System.InvalidOperationException
       call      CORINFO_HELP_NEWSFAST
       mov       rbx,rax
       mov       ecx,0ADE
       mov       rdx,7FFA3EB2F550
       call      CORINFO_HELP_STRCNS
       mov       rdx,rax
       mov       rcx,rbx
       call      qword ptr [7FFA3EA76C70]
       mov       rcx,rbx
       call      CORINFO_HELP_THROW
       int       3
M13_L36:
       call      CORINFO_HELP_RNGCHKFAIL
       int       3
; Total bytes of code 1611
```
```assembly
; Stateless.StateMachine`2+StateRepresentation[[Benchmark.HsmState, Benchmark],[Benchmark.HsmTrigger, Benchmark]].TryFindHandler(Benchmark.HsmTrigger, System.Object[], TriggerBehaviourResult<Benchmark.HsmState,Benchmark.HsmTrigger> ByRef)
M14_L00:
       push      r15
       push      r14
       push      r13
       push      r12
       push      rdi
       push      rsi
       push      rbp
       push      rbx
       sub       rsp,68
       vxorps    xmm4,xmm4,xmm4
       vmovdqa   xmmword ptr [rsp+50],xmm4
       xor       eax,eax
       mov       [rsp+60],rax
       mov       rsi,rcx
       mov       ebx,edx
       mov       rdi,r8
       mov       rbp,r9
       mov       rcx,offset MT_Stateless.StateMachine<Benchmark.HsmState, Benchmark.HsmTrigger>+StateRepresentation+<>c__DisplayClass41_0
       call      CORINFO_HELP_NEWSFAST
       mov       r14,rax
       lea       rcx,[r14+8]
       mov       rdx,rdi
       call      CORINFO_HELP_ASSIGN_REF
       mov       r15,[rsi+8]
       mov       rcx,offset MT_System.Collections.Generic.Dictionary<Benchmark.HsmTrigger, System.Collections.Generic.ICollection<Stateless.StateMachine<Benchmark.HsmState, Benchmark.HsmTrigger>+TriggerBehaviour>>
       cmp       [r15],rcx
       jne       near ptr M14_L21
       mov       edx,ebx
       cmp       qword ptr [r15+8],0
       je        near ptr M14_L05
       mov       r13,[r15+18]
       test      r13,r13
       jne       near ptr M14_L18
       mov       rcx,[r15+8]
       mov       eax,edx
       imul      rax,[r15+30]
       shr       rax,20
       inc       rax
       mov       edx,[rcx+8]
       mov       r8d,edx
       imul      rax,r8
       shr       rax,20
       cmp       eax,edx
       jae       near ptr M14_L30
       mov       eax,eax
       lea       rcx,[rcx+rax*4+10]
       mov       r13d,[rcx]
       mov       r15,[r15+10]
       xor       r12d,r12d
       dec       r13d
       mov       eax,[r15+8]
M14_L01:
       cmp       eax,r13d
       jbe       short M14_L05
       mov       ecx,r13d
       lea       rcx,[rcx+rcx*2]
       lea       r13,[r15+rcx*8+10]
       cmp       [r13+8],ebx
       jne       near ptr M14_L17
       cmp       [r13+10],ebx
       jne       near ptr M14_L17
M14_L02:
       jmp       short M14_L06
M14_L03:
       mov       eax,[r15+8]
       cmp       eax,edx
       jbe       short M14_L05
       mov       edx,edx
       lea       rdx,[rdx+rdx*2]
       lea       rdx,[r15+rdx*8+10]
       mov       r10,rdx
       cmp       [r10+8],r12d
       je        near ptr M14_L19
M14_L04:
       mov       edx,[r10+0C]
       inc       ecx
       mov       [rsp+4C],ecx
       cmp       eax,ecx
       mov       ecx,[rsp+4C]
       jae       short M14_L03
       jmp       near ptr M14_L22
M14_L05:
       xor       r13d,r13d
M14_L06:
       test      r13,r13
       jne       short M14_L08
       xor       ecx,ecx
       mov       [rsp+50],rcx
M14_L07:
       xor       r15d,r15d
       jmp       near ptr M14_L28
M14_L08:
       mov       rcx,[r13]
       mov       [rsp+50],rcx
M14_L09:
       mov       rcx,offset MT_System.Func<Stateless.StateMachine<Benchmark.HsmState, Benchmark.HsmTrigger>+TriggerBehaviour, Stateless.StateMachine<Benchmark.HsmState, Benchmark.HsmTrigger>+TriggerBehaviourResult>
       call      CORINFO_HELP_NEWSFAST
       mov       r15,rax
       mov       r12,[rsp+50]
       lea       rcx,[r15+8]
       mov       rdx,r14
       call      CORINFO_HELP_ASSIGN_REF
       mov       r8,offset Stateless.StateMachine`2+StateRepresentation+<>c__DisplayClass41_0[[Benchmark.HsmState, Benchmark],[Benchmark.HsmTrigger, Benchmark]].<TryFindLocalHandler>b__0(TriggerBehaviour<Benchmark.HsmState,Benchmark.HsmTrigger>)
       mov       [r15+18],r8
       mov       r8,r15
       mov       rdx,r12
       mov       rcx,7FFA3EC32568
       call      qword ptr [7FFA3EA77540]; System.Linq.Enumerable.Select[[System.__Canon, System.Private.CoreLib],[System.__Canon, System.Private.CoreLib]](System.Collections.Generic.IEnumerable`1<System.__Canon>, System.Func`2<System.__Canon,System.__Canon>)
       mov       r15,rax
       mov       rdx,r15
       mov       rcx,offset MT_System.Linq.Enumerable+Iterator<Stateless.StateMachine<Benchmark.HsmState, Benchmark.HsmTrigger>+TriggerBehaviourResult>
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       r13,rax
       test      r13,r13
       je        near ptr M14_L25
       mov       rdx,offset MT_System.Linq.Enumerable+ListSelectIterator<Stateless.StateMachine<Benchmark.HsmState, Benchmark.HsmTrigger>+TriggerBehaviour, Stateless.StateMachine<Benchmark.HsmState, Benchmark.HsmTrigger>+TriggerBehaviourResult>
       cmp       [r13],rdx
       jne       near ptr M14_L24
       mov       rdx,[r13+18]
       xor       r14d,r14d
       xor       r15d,r15d
       test      rdx,rdx
       je        short M14_L10
       mov       r15d,[rdx+10]
       mov       r14,[rdx+8]
       cmp       [r14+8],r15d
       jb        near ptr M14_L22
       add       r14,10
M14_L10:
       test      r15d,r15d
       je        near ptr M14_L23
       mov       edx,r15d
       mov       rcx,offset MT_Stateless.StateMachine<Benchmark.HsmState, Benchmark.HsmTrigger>+TriggerBehaviourResult[]
       call      CORINFO_HELP_NEWARR_1_OBJ
       mov       r12,rax
       lea       rax,[r12+10]
       mov       r8d,[r12+8]
       mov       [rsp+28],rax
       mov       [rsp+48],r8d
       mov       r13,[r13+20]
       xor       r10d,r10d
       test      r8d,r8d
       jle       short M14_L12
M14_L11:
       lea       rcx,[rax+r10*8]
       mov       [rsp+60],rcx
       cmp       r10d,r15d
       jae       near ptr M14_L30
       mov       [rsp+38],r10
       mov       rdx,[r14+r10*8]
       mov       rcx,[r13+8]
       call      qword ptr [r13+18]
       mov       rcx,[rsp+60]
       mov       rdx,rax
       call      CORINFO_HELP_CHECKED_ASSIGN_REF
       mov       rcx,[rsp+38]
       inc       ecx
       mov       edx,[rsp+48]
       cmp       ecx,edx
       mov       r10,rcx
       mov       rax,[rsp+28]
       jl        short M14_L11
M14_L12:
       mov       rcx,rsi
       mov       edx,ebx
       mov       r8,r12
       call      qword ptr [7FFA3EC05410]; Stateless.StateMachine`2+StateRepresentation[[Benchmark.HsmState, Benchmark],[Benchmark.HsmTrigger, Benchmark]].TryFindLocalHandlerResult(Benchmark.HsmTrigger, System.Collections.Generic.IEnumerable`1<TriggerBehaviourResult<Benchmark.HsmState,Benchmark.HsmTrigger>>)
       mov       r15,rax
       test      r15,r15
       je        near ptr M14_L27
M14_L13:
       test      r15,r15
       je        near ptr M14_L28
       mov       rdx,[r15+10]
       mov       rcx,7FFA3EC327B8
       call      qword ptr [7FFA3EC04FC0]; System.Linq.Enumerable.Any[[System.__Canon, System.Private.CoreLib]](System.Collections.Generic.IEnumerable`1<System.__Canon>)
       test      eax,eax
       sete      r14b
       movzx     r14d,r14b
M14_L14:
       xor       ecx,ecx
       mov       [rsp+50],rcx
       test      r14d,r14d
       je        short M14_L16
       mov       r13d,1
M14_L15:
       mov       rdx,[rsp+58]
       cmp       qword ptr [rsp+58],0
       cmove     rdx,r15
       mov       rcx,rbp
       call      CORINFO_HELP_CHECKED_ASSIGN_REF
       movzx     eax,r13b
       add       rsp,68
       pop       rbx
       pop       rbp
       pop       rsi
       pop       rdi
       pop       r12
       pop       r13
       pop       r14
       pop       r15
       ret
M14_L16:
       mov       rcx,[rsi+30]
       test      rcx,rcx
       je        near ptr M14_L29
       lea       r9,[rsp+58]
       mov       edx,ebx
       mov       r8,rdi
       cmp       [rcx],ecx
       call      qword ptr [7FFA3EC05218]
       mov       r13d,eax
       jmp       short M14_L15
M14_L17:
       mov       r13d,[r13+0C]
       inc       r12d
       cmp       eax,r12d
       jae       near ptr M14_L01
       jmp       near ptr M14_L22
M14_L18:
       mov       rcx,r13
       mov       r11,7FFA3E6B0710
       call      qword ptr [r11]
       mov       r12d,eax
       mov       rdx,[r15+8]
       mov       ecx,r12d
       imul      rcx,[r15+30]
       shr       rcx,20
       inc       rcx
       mov       r8d,[rdx+8]
       imul      rcx,r8
       shr       rcx,20
       cmp       ecx,[rdx+8]
       jae       near ptr M14_L30
       mov       ecx,ecx
       lea       rdx,[rdx+rcx*4+10]
       mov       edx,[rdx]
       mov       r15,[r15+10]
       xor       ecx,ecx
       dec       edx
       jmp       near ptr M14_L03
M14_L19:
       mov       [rsp+4C],ecx
       mov       [rsp+44],eax
       mov       [rsp+30],r10
       mov       edx,[r10+10]
       mov       rcx,r13
       mov       r8d,ebx
       mov       r11,7FFA3E6B0718
       call      qword ptr [r11]
       test      eax,eax
       mov       eax,[rsp+44]
       mov       ecx,[rsp+4C]
       jne       short M14_L20
       mov       r10,[rsp+30]
       jmp       near ptr M14_L04
M14_L20:
       mov       r13,[rsp+30]
       jmp       near ptr M14_L02
M14_L21:
       lea       r8,[rsp+50]
       mov       rcx,r15
       mov       edx,ebx
       mov       r11,7FFA3E6B0708
       call      qword ptr [r11]
       test      eax,eax
       jne       near ptr M14_L09
       jmp       near ptr M14_L07
M14_L22:
       call      qword ptr [7FFA3E75F2A0]
       int       3
M14_L23:
       mov       rcx,offset MT_System.Array+EmptyArray<Stateless.StateMachine<Benchmark.HsmState, Benchmark.HsmTrigger>+TriggerBehaviourResult>
       call      CORINFO_HELP_GET_GCSTATIC_BASE
       mov       rcx,202C7C01570
       mov       r12,[rcx]
       jmp       near ptr M14_L12
M14_L24:
       mov       rcx,r13
       mov       rax,[r13]
       mov       rax,[rax+40]
       call      qword ptr [rax+20]
       mov       r12,rax
       jmp       near ptr M14_L12
M14_L25:
       mov       rdx,r15
       mov       rcx,offset MT_System.Collections.Generic.ICollection<Stateless.StateMachine<Benchmark.HsmState, Benchmark.HsmTrigger>+TriggerBehaviourResult>
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfInterface(Void*, System.Object)
       test      rax,rax
       je        short M14_L26
       mov       rdx,rax
       mov       rcx,7FFA3EC7EED0
       call      qword ptr [7FFA3EA7D4A0]; System.Linq.Enumerable.ICollectionToArray[[System.__Canon, System.Private.CoreLib]](System.Collections.Generic.ICollection`1<System.__Canon>)
       mov       r12,rax
       jmp       near ptr M14_L12
M14_L26:
       mov       rdx,r15
       mov       rcx,7FFA3EC7EF58
       call      qword ptr [7FFA3EC0D920]
       mov       r12,rax
       jmp       near ptr M14_L12
M14_L27:
       mov       rcx,r12
       call      qword ptr [7FFA3EC05428]
       mov       r15,rax
       jmp       near ptr M14_L13
M14_L28:
       xor       r14d,r14d
       jmp       near ptr M14_L14
M14_L29:
       xor       r13d,r13d
       jmp       near ptr M14_L15
M14_L30:
       call      CORINFO_HELP_RNGCHKFAIL
       int       3
; Total bytes of code 1154
```
```assembly
; Stateless.StateMachine`2+StateRepresentation[[Benchmark.HsmState, Benchmark],[Benchmark.HsmTrigger, Benchmark]]..ctor(Benchmark.HsmState, Boolean)
       push      rbp
       sub       rsp,50
       lea       rbp,[rsp+50]
       vxorps    xmm4,xmm4,xmm4
       vmovdqu   ymmword ptr [rbp-30],ymm4
       vmovdqa   xmmword ptr [rbp-10],xmm4
       mov       [rbp+10],rcx
       mov       [rbp+18],edx
       mov       [rbp+20],r8d
       mov       rcx,offset MT_System.Collections.Generic.Dictionary<Benchmark.HsmTrigger, System.Collections.Generic.ICollection<Stateless.StateMachine<Benchmark.HsmState, Benchmark.HsmTrigger>+TriggerBehaviour>>
       call      CORINFO_HELP_NEWSFAST
       mov       [rbp-8],rax
       mov       rcx,[rbp-8]
       call      qword ptr [7FFA3EB05008]; System.Collections.Generic.Dictionary`2[[Benchmark.HsmTrigger, Benchmark],[System.__Canon, System.Private.CoreLib]]..ctor()
       mov       rax,[rbp+10]
       lea       rcx,[rax+8]
       mov       rdx,[rbp-8]
       call      CORINFO_HELP_ASSIGN_REF
       mov       rcx,offset MT_System.Collections.Generic.List<Stateless.StateMachine<Benchmark.HsmState, Benchmark.HsmTrigger>+EntryActionBehavior>
       call      CORINFO_HELP_NEWSFAST
       mov       [rbp-10],rax
       mov       rcx,[rbp-10]
       call      qword ptr [7FFA3EA773F0]; System.Collections.Generic.List`1[[System.__Canon, System.Private.CoreLib]]..ctor()
       mov       rax,[rbp+10]
       lea       rcx,[rax+10]
       mov       rdx,[rbp-10]
       call      CORINFO_HELP_ASSIGN_REF
       mov       rcx,offset MT_System.Collections.Generic.List<Stateless.StateMachine<Benchmark.HsmState, Benchmark.HsmTrigger>+ExitActionBehavior>
       call      CORINFO_HELP_NEWSFAST
       mov       [rbp-18],rax
       mov       rcx,[rbp-18]
       call      qword ptr [7FFA3EA773F0]; System.Collections.Generic.List`1[[System.__Canon, System.Private.CoreLib]]..ctor()
       mov       rax,[rbp+10]
       lea       rcx,[rax+18]
       mov       rdx,[rbp-18]
       call      CORINFO_HELP_ASSIGN_REF
       mov       rcx,offset MT_System.Collections.Generic.List<Stateless.StateMachine<Benchmark.HsmState, Benchmark.HsmTrigger>+ActivateActionBehaviour>
       call      CORINFO_HELP_NEWSFAST
       mov       [rbp-20],rax
       mov       rcx,[rbp-20]
       call      qword ptr [7FFA3EA773F0]; System.Collections.Generic.List`1[[System.__Canon, System.Private.CoreLib]]..ctor()
       mov       rax,[rbp+10]
       lea       rcx,[rax+20]
       mov       rdx,[rbp-20]
       call      CORINFO_HELP_ASSIGN_REF
       mov       rcx,offset MT_System.Collections.Generic.List<Stateless.StateMachine<Benchmark.HsmState, Benchmark.HsmTrigger>+DeactivateActionBehaviour>
       call      CORINFO_HELP_NEWSFAST
       mov       [rbp-28],rax
       mov       rcx,[rbp-28]
       call      qword ptr [7FFA3EA773F0]; System.Collections.Generic.List`1[[System.__Canon, System.Private.CoreLib]]..ctor()
       mov       rax,[rbp+10]
       lea       rcx,[rax+28]
       mov       rdx,[rbp-28]
       call      CORINFO_HELP_ASSIGN_REF
       mov       rcx,offset MT_System.Collections.Generic.List<Stateless.StateMachine<Benchmark.HsmState, Benchmark.HsmTrigger>+StateRepresentation>
       call      CORINFO_HELP_NEWSFAST
       mov       [rbp-30],rax
       mov       rcx,[rbp-30]
       call      qword ptr [7FFA3EA773F0]; System.Collections.Generic.List`1[[System.__Canon, System.Private.CoreLib]]..ctor()
       mov       rax,[rbp+10]
       lea       rcx,[rax+38]
       mov       rdx,[rbp-30]
       call      CORINFO_HELP_ASSIGN_REF
       mov       rcx,[rbp+10]
       call      qword ptr [7FFA3E75C8A0]; System.Object..ctor()
       mov       rax,[rbp+10]
       mov       ecx,[rbp+18]
       mov       [rax+40],ecx
       mov       rax,[rbp+10]
       mov       ecx,[rbp+20]
       mov       [rax+48],cl
       add       rsp,50
       pop       rbp
       ret
; Total bytes of code 347
```
```assembly
; System.Runtime.CompilerServices.CastHelpers.IsInstanceOfInterface(Void*, System.Object)
       test      rdx,rdx
       je        short M16_L01
       mov       r8,[rdx]
       movzx     r10d,word ptr [r8+0E]
       test      r10,r10
       je        short M16_L03
       mov       r9,[r8+38]
       cmp       r10,4
       jl        short M16_L04
M16_L00:
       cmp       [r9],rcx
       je        short M16_L01
       cmp       [r9+8],rcx
       je        short M16_L01
       cmp       [r9+10],rcx
       jne       short M16_L02
M16_L01:
       mov       rax,rdx
       ret
M16_L02:
       cmp       [r9+18],rcx
       je        short M16_L01
       add       r9,20
       add       r10,0FFFFFFFFFFFFFFFC
       cmp       r10,4
       jge       short M16_L00
       test      r10,r10
       jne       short M16_L04
M16_L03:
       test      dword ptr [r8],504C0000
       jne       short M16_L05
       xor       edx,edx
       jmp       short M16_L01
M16_L04:
       cmp       [r9],rcx
       je        short M16_L01
       add       r9,8
       dec       r10
       test      r10,r10
       jg        short M16_L04
       jmp       short M16_L03
M16_L05:
       jmp       qword ptr [7FFA3EA7EE68]; System.Runtime.CompilerServices.CastHelpers.IsInstance_Helper(Void*, System.Object)
; Total bytes of code 112
```
```assembly
; System.Linq.Enumerable.ICollectionToArray[[System.__Canon, System.Private.CoreLib]](System.Collections.Generic.ICollection`1<System.__Canon>)
       push      rdi
       push      rsi
       push      rbx
       sub       rsp,30
       mov       [rsp+28],rcx
       mov       rbx,rcx
       mov       rsi,rdx
       mov       rcx,rbx
       call      qword ptr [7FFAE4A9F030]
       mov       rcx,rsi
       mov       r11,rax
       call      qword ptr [rax]
       mov       edi,eax
       test      edi,edi
       je        short M17_L00
       mov       rcx,rbx
       call      qword ptr [7FFAE4A9E510]
       mov       rcx,rax
       movsxd    rdx,edi
       call      qword ptr [7FFAE4A9C678]; CORINFO_HELP_NEWARR_1_DIRECT
       mov       rdi,rax
       mov       rcx,rbx
       call      qword ptr [7FFAE4A9F038]
       mov       rcx,rsi
       mov       r11,rax
       mov       rdx,rdi
       xor       r8d,r8d
       call      qword ptr [rax]
       mov       rax,rdi
       add       rsp,30
       pop       rbx
       pop       rsi
       pop       rdi
       ret
M17_L00:
       mov       rcx,rbx
       call      qword ptr [7FFAE4A9EC78]
       mov       rcx,rax
       lea       rax,[System.Linq.Enumerable.Select[[System.__Canon, System.Private.CoreLib],[System.__Canon, System.Private.CoreLib]](System.Collections.Generic.IEnumerable`1<System.__Canon>, System.Func`2<System.__Canon,System.__Canon>)]
       add       rsp,30
       pop       rbx
       pop       rsi
       pop       rdi
       jmp       qword ptr [rax]
; Total bytes of code 128
```
```assembly
; Stateless.StateMachine`2+Transition[[Benchmark.HsmState, Benchmark],[Benchmark.HsmTrigger, Benchmark]]..ctor(Benchmark.HsmState, Benchmark.HsmState, Benchmark.HsmTrigger, System.Object[])
       push      rbx
       sub       rsp,20
       mov       rbx,rcx
       mov       [rbx+10],edx
       mov       [rbx+14],r8d
       mov       [rbx+18],r9d
       mov       rdx,[rsp+50]
       test      rdx,rdx
       je        short M18_L01
M18_L00:
       lea       rcx,[rbx+8]
       call      CORINFO_HELP_ASSIGN_REF
       nop
       add       rsp,20
       pop       rbx
       ret
M18_L01:
       mov       rcx,offset MT_System.Object[]
       xor       edx,edx
       call      CORINFO_HELP_NEWARR_1_OBJ
       mov       rdx,rax
       jmp       short M18_L00
; Total bytes of code 67
```
```assembly
; Stateless.StateMachine`2[[Benchmark.HsmState, Benchmark],[Benchmark.HsmTrigger, Benchmark]].GetRepresentation(Benchmark.HsmState)
       push      r15
       push      r14
       push      r13
       push      r12
       push      rdi
       push      rsi
       push      rbp
       push      rbx
       sub       rsp,28
       xor       eax,eax
       mov       [rsp+20],rax
       mov       rsi,rcx
       mov       ebx,edx
       mov       rdi,[rsi+8]
       mov       rax,offset MT_System.Collections.Generic.Dictionary<Benchmark.HsmState, Stateless.StateMachine<Benchmark.HsmState, Benchmark.HsmTrigger>+StateRepresentation>
       cmp       [rdi],rax
       jne       near ptr M19_L11
       mov       edx,ebx
       mov       rax,[rdi+8]
       test      rax,rax
       je        near ptr M19_L05
       mov       rbp,[rdi+18]
       test      rbp,rbp
       jne       near ptr M19_L07
       mov       ecx,edx
       imul      rcx,[rdi+30]
       shr       rcx,20
       inc       rcx
       mov       edx,[rax+8]
       mov       r11d,edx
       imul      rcx,r11
       shr       rcx,20
       cmp       ecx,edx
       jae       near ptr M19_L13
       mov       ecx,ecx
       lea       rax,[rax+rcx*4+10]
       mov       ecx,[rax]
       mov       rdx,[rdi+10]
       xor       r11d,r11d
       dec       ecx
       mov       r8d,[rdx+8]
M19_L00:
       cmp       r8d,ecx
       jbe       short M19_L05
       mov       eax,ecx
       lea       rax,[rax+rax*2]
       lea       r14,[rdx+rax*8+10]
       cmp       [r14+8],ebx
       jne       short M19_L06
       cmp       [r14+10],ebx
       jne       short M19_L06
M19_L01:
       test      r14,r14
       je        near ptr M19_L10
       mov       rax,[r14]
       mov       [rsp+20],rax
M19_L02:
       mov       rax,[rsp+20]
       add       rsp,28
       pop       rbx
       pop       rbp
       pop       rsi
       pop       rdi
       pop       r12
       pop       r13
       pop       r14
       pop       r15
       ret
M19_L03:
       mov       r12d,[rdi+8]
       cmp       r12d,edx
       jbe       short M19_L05
       mov       edx,edx
       lea       rdx,[rdx+rdx*2]
       lea       r14,[rdi+rdx*8+10]
       cmp       [r14+8],r15d
       je        short M19_L08
M19_L04:
       mov       edx,[r14+0C]
       inc       r13d
       cmp       r12d,r13d
       jae       short M19_L03
       jmp       near ptr M19_L09
M19_L05:
       xor       r14d,r14d
       jmp       short M19_L01
M19_L06:
       mov       ecx,[r14+0C]
       inc       r11d
       cmp       r8d,r11d
       jae       short M19_L00
       jmp       short M19_L09
M19_L07:
       mov       rcx,rbp
       mov       r11,7FFA3E6B0678
       call      qword ptr [r11]
       mov       r15d,eax
       mov       rdx,[rdi+8]
       mov       ecx,r15d
       imul      rcx,[rdi+30]
       shr       rcx,20
       inc       rcx
       mov       r8d,[rdx+8]
       imul      rcx,r8
       shr       rcx,20
       cmp       ecx,[rdx+8]
       jae       near ptr M19_L13
       mov       ecx,ecx
       lea       rdx,[rdx+rcx*4+10]
       mov       edx,[rdx]
       mov       rdi,[rdi+10]
       xor       r13d,r13d
       dec       edx
       jmp       near ptr M19_L03
M19_L08:
       mov       edx,[r14+10]
       mov       rcx,rbp
       mov       r8d,ebx
       mov       r11,7FFA3E6B0680
       call      qword ptr [r11]
       test      eax,eax
       jne       near ptr M19_L01
       jmp       near ptr M19_L04
M19_L09:
       call      qword ptr [7FFA3E75F2A0]
       int       3
M19_L10:
       xor       r8d,r8d
       mov       [rsp+20],r8
       jmp       short M19_L12
M19_L11:
       lea       r8,[rsp+20]
       mov       rcx,rdi
       mov       edx,ebx
       mov       r11,7FFA3E6B0668
       call      qword ptr [r11]
       test      eax,eax
       jne       near ptr M19_L02
M19_L12:
       mov       rcx,offset MT_Stateless.StateMachine<Benchmark.HsmState, Benchmark.HsmTrigger>+StateRepresentation
       call      CORINFO_HELP_NEWSFAST
       mov       rdi,rax
       movzx     r8d,byte ptr [rsi+51]
       mov       rcx,rdi
       mov       edx,ebx
       call      qword ptr [7FFA3EB051A0]; Stateless.StateMachine`2+StateRepresentation[[Benchmark.HsmState, Benchmark],[Benchmark.HsmTrigger, Benchmark]]..ctor(Benchmark.HsmState, Boolean)
       mov       [rsp+20],rdi
       mov       rcx,[rsi+8]
       mov       r8,[rsp+20]
       mov       edx,ebx
       mov       r11,7FFA3E6B0670
       call      qword ptr [r11]
       jmp       near ptr M19_L02
M19_L13:
       call      CORINFO_HELP_RNGCHKFAIL
       int       3
; Total bytes of code 506
```
```assembly
; System.MulticastDelegate.CtorClosed(System.Object, IntPtr)
       push      rsi
       push      rbx
       sub       rsp,28
       mov       rbx,rcx
       mov       rsi,r8
       test      rdx,rdx
       je        short M20_L00
       lea       rcx,[rbx+8]
       call      CORINFO_HELP_ASSIGN_REF
       mov       [rbx+18],rsi
       add       rsp,28
       pop       rbx
       pop       rsi
       ret
M20_L00:
       call      qword ptr [7FFA3EC0C318]
       int       3
; Total bytes of code 44
```

## .NET 9.0.5 (9.0.525.21509), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
```assembly
; Benchmark.HsmBenchmarks.FastFSM_Hsm_History_Shallow()
       push      r15
       push      r14
       push      r13
       push      r12
       push      rdi
       push      rsi
       push      rbp
       push      rbx
       sub       rsp,68
       mov       rbx,rcx
M00_L00:
       mov       esi,400
M00_L01:
       mov       rcx,[rbx+18]
       mov       rdx,offset MT_Benchmark.FastFsmHsmHistoryShallow
       cmp       [rcx],rdx
       jne       near ptr M00_L109
       mov       rdi,rcx
       mov       [rsp+30],rdi
       cmp       byte ptr [rdi+14],0
       je        near ptr M00_L108
       xor       ebp,ebp
       mov       dword ptr [rsp+64],80000000
       mov       dword ptr [rsp+60],7FFFFFFF
       mov       r10d,7FFFFFFF
       xor       r14d,r14d
       mov       r15d,0FFFFFFFF
       xor       r13d,r13d
       mov       r12d,[rdi+10]
       mov       r9d,r12d
       mov       [rsp+5C],r9d
       mov       r11d,r9d
       test      r9d,r9d
       jl        near ptr M00_L07
       mov       rdx,214EEC009C0
       mov       rdx,[rdx]
M00_L02:
       cmp       r11d,r9d
       jne       short M00_L04
       xor       ecx,ecx
M00_L03:
       cmp       r11d,3
       ja        short M00_L05
       mov       r9d,r11d
       lea       rdi,[7FFA3E8372C0]
       mov       edi,[rdi+r9*4]
       lea       r8,[M00_L00]
       add       rdi,r8
       jmp       rdi
       mov       rdi,[rsp+30]
       mov       r9d,[rsp+5C]
       jmp       short M00_L05
M00_L04:
       mov       rcx,214EEC009C8
       mov       rcx,[rcx]
       mov       rax,rcx
       cmp       r9d,5
       jae       near ptr M00_L113
       mov       r8d,r9d
       mov       eax,[rax+r8*4+10]
       cmp       r11d,5
       jae       near ptr M00_L113
       mov       r8d,r11d
       sub       eax,[rcx+r8*4+10]
       mov       ecx,eax
       jmp       short M00_L03
       xor       r8d,r8d
       test      ebp,ebp
       je        near ptr M00_L84
       jmp       near ptr M00_L82
M00_L05:
       cmp       r11d,5
       jae       near ptr M00_L12
       mov       rcx,rdx
       mov       r11d,r11d
       mov       r11d,[rcx+r11*4+10]
M00_L06:
       test      r11d,r11d
       jge       near ptr M00_L02
M00_L07:
       test      ebp,ebp
       je        near ptr M00_L17
       test      r14d,r14d
       jne       near ptr M00_L17
       mov       ecx,r12d
       cmp       ecx,r15d
       je        near ptr M00_L14
       mov       r11d,r15d
       mov       rdx,214EEC009C8
       mov       rax,[rdx]
M00_L08:
       mov       r8,rax
       cmp       ecx,5
       jae       near ptr M00_L113
       mov       r10d,ecx
       mov       r8d,[r8+r10*4+10]
       mov       r9,rax
       cmp       r15d,5
       jae       near ptr M00_L113
       mov       edx,r15d
       cmp       r8d,[r9+rdx*4+10]
       jg        short M00_L13
       nop       dword ptr [rax]
M00_L09:
       mov       rdx,rax
       cmp       r11d,5
       jae       near ptr M00_L113
       mov       r10d,r11d
       cmp       [rdx+r10*4+10],r8d
       jg        near ptr M00_L86
       jmp       short M00_L11
       nop       dword ptr [rax]
M00_L10:
       mov       rax,214EEC009C0
       mov       rdx,[rax]
       mov       rax,rdx
       cmp       ecx,5
       jae       near ptr M00_L113
       mov       ecx,ecx
       mov       ecx,[rax+rcx*4+10]
       cmp       r11d,5
       jae       near ptr M00_L113
       mov       r11d,r11d
       mov       r11d,[rdx+r11*4+10]
M00_L11:
       cmp       ecx,r11d
       jne       short M00_L10
       jmp       short M00_L14
M00_L12:
       mov       r11d,0FFFFFFFF
       jmp       near ptr M00_L06
M00_L13:
       mov       rdx,214EEC009C0
       mov       rdx,[rdx]
       mov       ecx,ecx
       mov       ecx,[rdx+rcx*4+10]
       jmp       near ptr M00_L08
M00_L14:
       mov       rdx,214EEC009C0
       mov       rdx,[rdx]
       mov       ecx,5
       cmp       ecx,r12d
       jbe       near ptr M00_L20
       mov       rcx,rdx
       mov       eax,r12d
       mov       ebp,[rcx+rax*4+10]
M00_L15:
       test      ebp,ebp
       jge       near ptr M00_L21
M00_L16:
       mov       [rdi+10],r15d
       mov       edx,[rdi+10]
       mov       rcx,rdi
       call      qword ptr [7FFA3EB24D50]; Benchmark.FastFsmHsmHistoryShallow.GetCompositeEntryTarget(Int32)
       mov       [rdi+10],eax
M00_L17:
       mov       rcx,[rbx+18]
       mov       rdx,offset MT_Benchmark.FastFsmHsmHistoryShallow
       cmp       [rcx],rdx
       jne       near ptr M00_L110
       mov       r14,rcx
       mov       [rsp+28],r14
       cmp       byte ptr [r14+14],0
       je        near ptr M00_L107
       xor       eax,eax
       mov       r8d,80000000
       mov       r10d,7FFFFFFF
       mov       r9d,7FFFFFFF
       xor       r11d,r11d
       mov       ecx,0FFFFFFFF
       xor       edx,edx
       mov       [rsp+58],edx
       mov       r13d,[r14+10]
       mov       r15d,r13d
       mov       [rsp+54],r15d
       mov       r12d,r15d
       test      r15d,r15d
       jl        near ptr M00_L27
M00_L18:
       cmp       r12d,r15d
       jne       near ptr M00_L24
       xor       edi,edi
M00_L19:
       cmp       r12d,3
       ja        near ptr M00_L25
       mov       ebp,r12d
       lea       r15,[7FFA3E8372D0]
       mov       r15d,[r15+rbp*4]
       lea       r14,[M00_L00]
       add       r15,r14
       jmp       r15
       mov       r14,[rsp+28]
       mov       r15d,[rsp+54]
       jmp       near ptr M00_L25
M00_L20:
       mov       ebp,0FFFFFFFF
       jmp       near ptr M00_L15
M00_L21:
       mov       r8,214EEC009D8
       mov       r8,[r8]
M00_L22:
       mov       r10,r8
       cmp       ebp,5
       jae       near ptr M00_L113
       mov       r9d,ebp
       cmp       dword ptr [r10+r9*4+10],0
       je        short M00_L23
       mov       rax,[rdi+18]
       cmp       ebp,[rax+8]
       jae       near ptr M00_L113
       mov       r11d,ebp
       mov       [rax+r11*4+10],r12d
M00_L23:
       mov       rcx,rdx
       mov       eax,ebp
       mov       ebp,[rcx+rax*4+10]
       test      ebp,ebp
       jge       short M00_L22
       jmp       near ptr M00_L16
M00_L24:
       mov       rdi,214EEC009C8
       mov       rdi,[rdi]
       mov       rbp,rdi
       cmp       r15d,5
       jae       near ptr M00_L113
       mov       edx,r15d
       mov       edx,[rbp+rdx*4+10]
       cmp       r12d,5
       jae       near ptr M00_L113
       mov       ebp,r12d
       sub       edx,[rdi+rbp*4+10]
       mov       edi,edx
       jmp       near ptr M00_L19
       xor       ebp,ebp
       test      eax,eax
       je        near ptr M00_L89
       jmp       near ptr M00_L87
       xor       r14d,r14d
       test      eax,eax
       je        near ptr M00_L92
       jmp       near ptr M00_L90
M00_L25:
       mov       rdi,214EEC009C0
       mov       rdi,[rdi]
       mov       ebp,5
       cmp       ebp,r12d
       jbe       near ptr M00_L32
       cmp       r12d,5
       jae       near ptr M00_L113
       mov       r12d,r12d
       mov       r12d,[rdi+r12*4+10]
M00_L26:
       test      r12d,r12d
       jge       near ptr M00_L18
M00_L27:
       test      eax,eax
       je        near ptr M00_L37
       test      r11d,r11d
       jne       near ptr M00_L37
       mov       edi,r13d
       cmp       edi,ecx
       je        near ptr M00_L34
       mov       eax,ecx
       mov       rdx,214EEC009C8
       mov       r8,[rdx]
M00_L28:
       mov       r10,r8
       cmp       edi,5
       jae       near ptr M00_L113
       mov       r9d,edi
       mov       ebp,[r10+r9*4+10]
       mov       r11,r8
       cmp       ecx,5
       jae       near ptr M00_L113
       mov       edx,ecx
       cmp       ebp,[r11+rdx*4+10]
       jg        short M00_L33
M00_L29:
       mov       rdx,r8
       cmp       eax,5
       jae       near ptr M00_L113
       mov       r10d,eax
       cmp       [rdx+r10*4+10],ebp
       jg        near ptr M00_L94
       jmp       short M00_L31
M00_L30:
       mov       r8,214EEC009C0
       mov       rdx,[r8]
       mov       r8,rdx
       cmp       edi,5
       jae       near ptr M00_L113
       mov       r10d,edi
       mov       edi,[r8+r10*4+10]
       cmp       eax,5
       jae       near ptr M00_L113
       mov       eax,eax
       mov       eax,[rdx+rax*4+10]
M00_L31:
       cmp       edi,eax
       jne       short M00_L30
       jmp       short M00_L34
M00_L32:
       mov       r12d,0FFFFFFFF
       jmp       near ptr M00_L26
M00_L33:
       mov       rdx,214EEC009C0
       mov       rdx,[rdx]
       cmp       edi,5
       jae       near ptr M00_L113
       mov       r10d,edi
       mov       edi,[rdx+r10*4+10]
       jmp       near ptr M00_L28
M00_L34:
       mov       rdx,214EEC009C0
       mov       rdx,[rdx]
       mov       eax,5
       cmp       eax,r13d
       jbe       near ptr M00_L40
       mov       rax,rdx
       cmp       r13d,5
       jae       near ptr M00_L113
       mov       r8d,r13d
       mov       r15d,[rax+r8*4+10]
M00_L35:
       test      r15d,r15d
       jge       near ptr M00_L41
M00_L36:
       mov       [r14+10],ecx
       mov       edx,[r14+10]
       mov       rcx,r14
       call      qword ptr [7FFA3EB24D50]; Benchmark.FastFsmHsmHistoryShallow.GetCompositeEntryTarget(Int32)
       mov       [r14+10],eax
M00_L37:
       mov       rcx,[rbx+18]
       mov       rdx,offset MT_Benchmark.FastFsmHsmHistoryShallow
       cmp       [rcx],rdx
       jne       near ptr M00_L111
       mov       r12,rcx
       mov       [rsp+20],r12
       cmp       byte ptr [r12+14],0
       je        near ptr M00_L106
       xor       eax,eax
       mov       dword ptr [rsp+50],80000000
       mov       r10d,7FFFFFFF
       mov       r9d,7FFFFFFF
       xor       r11d,r11d
       mov       ecx,0FFFFFFFF
       xor       edx,edx
       mov       [rsp+4C],edx
       mov       edi,[r12+10]
       mov       ebp,edi
       mov       [rsp+48],ebp
       mov       r13d,ebp
       test      ebp,ebp
       jl        near ptr M00_L47
M00_L38:
       mov       r14d,r13d
       cmp       r14d,ebp
       jne       near ptr M00_L44
       xor       r14d,r14d
M00_L39:
       cmp       r13d,3
       ja        near ptr M00_L45
       mov       r15d,r13d
       lea       rbp,[7FFA3E8372E0]
       mov       ebp,[rbp+r15*4]
       lea       r12,[M00_L00]
       add       rbp,r12
       jmp       rbp
       mov       ebp,[rsp+48]
       mov       r12,[rsp+20]
       jmp       near ptr M00_L45
M00_L40:
       mov       r15d,0FFFFFFFF
       jmp       near ptr M00_L35
M00_L41:
       mov       r8,214EEC009D8
       mov       r8,[r8]
M00_L42:
       mov       r10,r8
       cmp       r15d,5
       jae       near ptr M00_L113
       mov       r9d,r15d
       cmp       dword ptr [r10+r9*4+10],0
       je        short M00_L43
       mov       rax,[r14+18]
       cmp       r15d,[rax+8]
       jae       near ptr M00_L113
       mov       r11d,r15d
       mov       [rax+r11*4+10],r13d
M00_L43:
       mov       rax,rdx
       cmp       r15d,5
       jae       near ptr M00_L113
       mov       r10d,r15d
       mov       r15d,[rax+r10*4+10]
       test      r15d,r15d
       jge       short M00_L42
       jmp       near ptr M00_L36
M00_L44:
       mov       r15,214EEC009C8
       mov       r15,[r15]
       mov       rdx,r15
       cmp       ebp,5
       jae       near ptr M00_L113
       mov       r8d,ebp
       mov       edx,[rdx+r8*4+10]
       mov       r8,r15
       cmp       r14d,5
       jae       near ptr M00_L113
       mov       r15d,r14d
       mov       r14d,edx
       sub       r14d,[r8+r15*4+10]
       jmp       near ptr M00_L39
       xor       ebp,ebp
       test      eax,eax
       je        near ptr M00_L97
       jmp       near ptr M00_L95
M00_L45:
       mov       r14,214EEC009C0
       mov       r14,[r14]
       mov       r15d,5
       cmp       r15d,r13d
       jbe       near ptr M00_L52
       cmp       r13d,5
       jae       near ptr M00_L113
       mov       r13d,r13d
       mov       r13d,[r14+r13*4+10]
M00_L46:
       test      r13d,r13d
       jge       near ptr M00_L38
M00_L47:
       test      eax,eax
       je        near ptr M00_L57
       test      r11d,r11d
       jne       near ptr M00_L57
       mov       r14d,edi
       cmp       r14d,ecx
       je        near ptr M00_L54
       mov       eax,ecx
       mov       rdx,214EEC009C8
       mov       r15,[rdx]
M00_L48:
       mov       r8,r15
       cmp       r14d,5
       jae       near ptr M00_L113
       mov       r10d,r14d
       mov       r8d,[r8+r10*4+10]
       mov       r9,r15
       cmp       ecx,5
       jae       near ptr M00_L113
       mov       r11d,ecx
       cmp       r8d,[r9+r11*4+10]
       jg        short M00_L53
       nop       dword ptr [rax]
M00_L49:
       mov       rdx,r15
       cmp       eax,5
       jae       near ptr M00_L113
       mov       r10d,eax
       cmp       [rdx+r10*4+10],r8d
       jg        near ptr M00_L99
       jmp       short M00_L51
       nop       word ptr [rax+rax]
M00_L50:
       mov       rdx,214EEC009C0
       mov       rdx,[rdx]
       mov       r8,rdx
       cmp       r14d,5
       jae       near ptr M00_L113
       mov       r10d,r14d
       mov       r14d,[r8+r10*4+10]
       cmp       eax,5
       jae       near ptr M00_L113
       mov       eax,eax
       mov       eax,[rdx+rax*4+10]
M00_L51:
       cmp       r14d,eax
       jne       short M00_L50
       jmp       short M00_L54
M00_L52:
       mov       r13d,0FFFFFFFF
       jmp       near ptr M00_L46
M00_L53:
       mov       rdx,214EEC009C0
       mov       rdx,[rdx]
       cmp       r14d,5
       jae       near ptr M00_L113
       mov       r8d,r14d
       mov       r14d,[rdx+r8*4+10]
       jmp       near ptr M00_L48
M00_L54:
       mov       rdx,214EEC009C0
       mov       rdx,[rdx]
       mov       eax,5
       cmp       eax,edi
       jbe       near ptr M00_L60
       mov       rax,rdx
       cmp       edi,5
       jae       near ptr M00_L113
       mov       r8d,edi
       mov       r15d,[rax+r8*4+10]
M00_L55:
       test      r15d,r15d
       jge       near ptr M00_L61
M00_L56:
       mov       [r12+10],ecx
       mov       edx,[r12+10]
       mov       rcx,r12
       call      qword ptr [7FFA3EB24D50]; Benchmark.FastFsmHsmHistoryShallow.GetCompositeEntryTarget(Int32)
       mov       [r12+10],eax
M00_L57:
       mov       r13,[rbx+18]
       mov       rdx,offset MT_Benchmark.FastFsmHsmHistoryShallow
       cmp       [r13],rdx
       jne       near ptr M00_L112
       cmp       byte ptr [r13+14],0
       je        near ptr M00_L105
       xor       eax,eax
       mov       dword ptr [rsp+44],80000000
       mov       r10d,7FFFFFFF
       mov       r9d,7FFFFFFF
       xor       r11d,r11d
       mov       ecx,0FFFFFFFF
       xor       edx,edx
       mov       [rsp+40],edx
       mov       ebp,[r13+10]
       mov       r14d,ebp
       mov       [rsp+3C],r14d
       mov       edi,r14d
       test      r14d,r14d
       jl        near ptr M00_L67
M00_L58:
       mov       r12d,edi
       cmp       r12d,r14d
       jne       near ptr M00_L64
       xor       r12d,r12d
M00_L59:
       cmp       edi,3
       ja        near ptr M00_L65
       mov       r15d,edi
       lea       r14,[7FFA3E8372F0]
       mov       r14d,[r14+r15*4]
       lea       rdx,[M00_L00]
       add       r14,rdx
       jmp       r14
       mov       r14d,[rsp+3C]
       jmp       near ptr M00_L65
M00_L60:
       mov       r15d,0FFFFFFFF
       jmp       near ptr M00_L55
M00_L61:
       mov       r8,214EEC009D8
       mov       r8,[r8]
M00_L62:
       mov       r10,r8
       cmp       r15d,5
       jae       near ptr M00_L113
       mov       r9d,r15d
       cmp       dword ptr [r10+r9*4+10],0
       je        short M00_L63
       mov       rax,[r12+18]
       cmp       r15d,[rax+8]
       jae       near ptr M00_L113
       mov       r11d,r15d
       mov       [rax+r11*4+10],edi
M00_L63:
       mov       rax,rdx
       cmp       r15d,5
       jae       near ptr M00_L113
       mov       r10d,r15d
       mov       r15d,[rax+r10*4+10]
       test      r15d,r15d
       jge       short M00_L62
       jmp       near ptr M00_L56
M00_L64:
       mov       r15,214EEC009C8
       mov       r15,[r15]
       mov       rdx,r15
       cmp       r14d,5
       jae       near ptr M00_L113
       mov       r8d,r14d
       mov       edx,[rdx+r8*4+10]
       mov       r8,r15
       cmp       r12d,5
       jae       near ptr M00_L113
       mov       r15d,r12d
       mov       r12d,edx
       sub       r12d,[r8+r15*4+10]
       jmp       near ptr M00_L59
       xor       edx,edx
       test      eax,eax
       je        near ptr M00_L102
       jmp       near ptr M00_L100
M00_L65:
       mov       r15,214EEC009C0
       mov       r15,[r15]
       mov       r12d,5
       cmp       r12d,edi
       jbe       near ptr M00_L72
       cmp       edi,5
       jae       near ptr M00_L113
       mov       edi,edi
       mov       edi,[r15+rdi*4+10]
M00_L66:
       test      edi,edi
       jge       near ptr M00_L58
M00_L67:
       test      eax,eax
       je        near ptr M00_L77
       test      r11d,r11d
       jne       near ptr M00_L77
       mov       r15d,ebp
       cmp       r15d,ecx
       je        near ptr M00_L74
       mov       eax,ecx
       mov       rdx,214EEC009C8
       mov       r8,[rdx]
M00_L68:
       mov       r10,r8
       cmp       r15d,5
       jae       near ptr M00_L113
       mov       r9d,r15d
       mov       r12d,[r10+r9*4+10]
       mov       r11,r8
       cmp       ecx,5
       jae       near ptr M00_L113
       mov       edx,ecx
       cmp       r12d,[r11+rdx*4+10]
       jg        short M00_L73
M00_L69:
       mov       rdx,r8
       cmp       eax,5
       jae       near ptr M00_L113
       mov       r10d,eax
       cmp       [rdx+r10*4+10],r12d
       jg        near ptr M00_L104
       jmp       short M00_L71
M00_L70:
       mov       r8,214EEC009C0
       mov       rdx,[r8]
       mov       r8,rdx
       cmp       r15d,5
       jae       near ptr M00_L113
       mov       r10d,r15d
       mov       r15d,[r8+r10*4+10]
       cmp       eax,5
       jae       near ptr M00_L113
       mov       eax,eax
       mov       eax,[rdx+rax*4+10]
M00_L71:
       cmp       r15d,eax
       jne       short M00_L70
       jmp       short M00_L74
M00_L72:
       mov       edi,0FFFFFFFF
       jmp       near ptr M00_L66
M00_L73:
       mov       rdx,214EEC009C0
       mov       rdx,[rdx]
       cmp       r15d,5
       jae       near ptr M00_L113
       mov       r10d,r15d
       mov       r15d,[rdx+r10*4+10]
       jmp       near ptr M00_L68
M00_L74:
       mov       rdx,214EEC009C0
       mov       rdx,[rdx]
       mov       eax,5
       cmp       eax,ebp
       jbe       short M00_L78
       mov       rax,rdx
       cmp       ebp,5
       jae       near ptr M00_L113
       mov       r8d,ebp
       mov       r14d,[rax+r8*4+10]
M00_L75:
       test      r14d,r14d
       jge       short M00_L79
M00_L76:
       mov       [r13+10],ecx
       mov       edx,[r13+10]
       mov       rcx,r13
       call      qword ptr [7FFA3EB24D50]; Benchmark.FastFsmHsmHistoryShallow.GetCompositeEntryTarget(Int32)
       mov       [r13+10],eax
M00_L77:
       dec       esi
       jne       near ptr M00_L01
       mov       rcx,[rbx+18]
       mov       ecx,[rcx+10]
       add       rsp,68
       pop       rbx
       pop       rbp
       pop       rsi
       pop       rdi
       pop       r12
       pop       r13
       pop       r14
       pop       r15
       jmp       qword ptr [7FFA3EC24F18]; BenchmarkDotNet.Engines.DeadCodeEliminationHelper.KeepAliveWithoutBoxing[[Benchmark.HsmState, Benchmark]](Benchmark.HsmState)
M00_L78:
       mov       r14d,0FFFFFFFF
       jmp       short M00_L75
M00_L79:
       mov       rax,214EEC009D8
       mov       r8,[rax]
M00_L80:
       mov       rax,r8
       cmp       r14d,5
       jae       near ptr M00_L113
       mov       r10d,r14d
       cmp       dword ptr [rax+r10*4+10],0
       je        short M00_L81
       mov       rax,[r13+18]
       cmp       r14d,[rax+8]
       jae       near ptr M00_L113
       mov       r10d,r14d
       mov       [rax+r10*4+10],ebp
M00_L81:
       mov       rax,rdx
       cmp       r14d,5
       jae       near ptr M00_L113
       mov       r10d,r14d
       mov       r14d,[rax+r10*4+10]
       test      r14d,r14d
       jge       short M00_L80
       jmp       near ptr M00_L76
M00_L82:
       mov       eax,[rsp+64]
       test      eax,eax
       jl        short M00_L84
       test      eax,eax
       jne       short M00_L83
       mov       r8d,[rsp+60]
       cmp       ecx,r8d
       jl        short M00_L84
       jne       short M00_L85
       cmp       r13d,r10d
       jge       short M00_L85
       jmp       short M00_L84
M00_L83:
       test      r8d,r8d
       mov       r8d,[rsp+60]
       je        short M00_L85
M00_L84:
       mov       ebp,1
       xor       eax,eax
       mov       r8d,ecx
       mov       r10d,r13d
       xor       r14d,r14d
       mov       r15d,1
M00_L85:
       inc       r13d
       mov       [rsp+64],eax
       mov       [rsp+60],r8d
       mov       rdi,[rsp+30]
       mov       r9d,[rsp+5C]
       jmp       near ptr M00_L05
M00_L86:
       mov       rdx,214EEC009C0
       mov       rdx,[rdx]
       mov       r11d,r11d
       mov       r11d,[rdx+r11*4+10]
       jmp       near ptr M00_L09
M00_L87:
       test      r8d,r8d
       jl        short M00_L89
       test      r8d,r8d
       jne       short M00_L88
       cmp       edi,r10d
       jl        short M00_L89
       jne       short M00_L93
       mov       edx,[rsp+58]
       cmp       edx,r9d
       mov       [rsp+58],edx
       jge       short M00_L93
       jmp       short M00_L89
M00_L88:
       test      ebp,ebp
       je        short M00_L93
M00_L89:
       mov       eax,1
       xor       r8d,r8d
       mov       r10d,edi
       mov       edx,[rsp+58]
       mov       r9d,edx
       xor       r11d,r11d
       mov       ecx,2
       mov       [rsp+58],edx
       jmp       short M00_L93
M00_L90:
       test      r8d,r8d
       jl        short M00_L92
       test      r8d,r8d
       jne       short M00_L91
       cmp       edi,r10d
       jl        short M00_L92
       jne       short M00_L93
       mov       edx,[rsp+58]
       cmp       edx,r9d
       mov       [rsp+58],edx
       jge       short M00_L93
       jmp       short M00_L92
M00_L91:
       test      r14d,r14d
       je        short M00_L93
M00_L92:
       mov       eax,1
       xor       r8d,r8d
       mov       r10d,edi
       mov       edx,[rsp+58]
       mov       r9d,edx
       xor       r11d,r11d
       mov       ecx,3
       mov       [rsp+58],edx
M00_L93:
       mov       edx,[rsp+58]
       inc       edx
       mov       [rsp+58],edx
       mov       r14,[rsp+28]
       mov       r15d,[rsp+54]
       jmp       near ptr M00_L25
M00_L94:
       mov       rdx,214EEC009C0
       mov       rdx,[rdx]
       cmp       eax,5
       jae       near ptr M00_L113
       mov       eax,eax
       mov       eax,[rdx+rax*4+10]
       jmp       near ptr M00_L29
M00_L95:
       mov       r8d,[rsp+50]
       test      r8d,r8d
       jl        short M00_L97
       test      r8d,r8d
       jne       short M00_L96
       cmp       r14d,r10d
       jl        short M00_L97
       jne       short M00_L98
       mov       edx,[rsp+4C]
       cmp       edx,r9d
       mov       [rsp+4C],edx
       jge       short M00_L98
       jmp       short M00_L97
M00_L96:
       test      ebp,ebp
       je        short M00_L98
M00_L97:
       mov       eax,1
       xor       r8d,r8d
       mov       r10d,r14d
       mov       edx,[rsp+4C]
       mov       r9d,edx
       xor       r11d,r11d
       xor       ecx,ecx
       mov       [rsp+4C],edx
M00_L98:
       mov       edx,[rsp+4C]
       inc       edx
       mov       [rsp+4C],edx
       mov       [rsp+50],r8d
       mov       ebp,[rsp+48]
       mov       r12,[rsp+20]
       jmp       near ptr M00_L45
M00_L99:
       mov       rdx,214EEC009C0
       mov       rdx,[rdx]
       cmp       eax,5
       jae       near ptr M00_L113
       mov       eax,eax
       mov       eax,[rdx+rax*4+10]
       jmp       near ptr M00_L49
M00_L100:
       mov       r8d,[rsp+44]
       test      r8d,r8d
       jl        short M00_L102
       test      r8d,r8d
       jne       short M00_L101
       cmp       r12d,r10d
       jl        short M00_L102
       jne       short M00_L103
       mov       edx,[rsp+40]
       cmp       edx,r9d
       mov       [rsp+40],edx
       jge       short M00_L103
       jmp       short M00_L102
M00_L101:
       test      edx,edx
       je        short M00_L103
M00_L102:
       mov       eax,1
       xor       r8d,r8d
       mov       r10d,r12d
       mov       edx,[rsp+40]
       mov       r9d,edx
       xor       r11d,r11d
       mov       ecx,1
       mov       [rsp+40],edx
M00_L103:
       mov       edx,[rsp+40]
       inc       edx
       mov       [rsp+40],edx
       mov       [rsp+44],r8d
       mov       r14d,[rsp+3C]
       jmp       near ptr M00_L65
M00_L104:
       mov       rdx,214EEC009C0
       mov       rdx,[rdx]
       cmp       eax,5
       jae       near ptr M00_L113
       mov       eax,eax
       mov       eax,[rdx+rax*4+10]
       jmp       near ptr M00_L69
M00_L105:
       mov       rcx,r13
       call      System.Object.GetType()
       mov       rbx,rax
       mov       rcx,offset MT_System.InvalidOperationException
       call      CORINFO_HELP_NEWSFAST
       mov       rsi,rax
       mov       rcx,rbx
       xor       edx,edx
       call      qword ptr [7FFA3EB277B0]; System.RuntimeType.GetCachedName(System.TypeNameKind)
       mov       rbx,rax
       mov       ecx,166
       mov       rdx,7FFA3EB17080
       call      CORINFO_HELP_STRCNS
       mov       rdx,rax
       mov       rcx,rbx
       call      qword ptr [7FFA3E77D788]; System.String.Concat(System.String, System.String)
       mov       rdx,rax
       mov       rcx,rsi
       call      qword ptr [7FFA3EA96C70]
       mov       rcx,rsi
       call      CORINFO_HELP_THROW
       int       3
M00_L106:
       call      System.Object.GetType()
       mov       rbx,rax
       mov       rcx,offset MT_System.InvalidOperationException
       call      CORINFO_HELP_NEWSFAST
       mov       rsi,rax
       mov       rcx,rbx
       xor       edx,edx
       call      qword ptr [7FFA3EB277B0]; System.RuntimeType.GetCachedName(System.TypeNameKind)
       mov       rbx,rax
       mov       ecx,166
       mov       rdx,7FFA3EB17080
       call      CORINFO_HELP_STRCNS
       mov       rdx,rax
       mov       rcx,rbx
       call      qword ptr [7FFA3E77D788]; System.String.Concat(System.String, System.String)
       mov       rdx,rax
       mov       rcx,rsi
       call      qword ptr [7FFA3EA96C70]
       mov       rcx,rsi
       call      CORINFO_HELP_THROW
       int       3
M00_L107:
       call      System.Object.GetType()
       mov       rbx,rax
       mov       rcx,offset MT_System.InvalidOperationException
       call      CORINFO_HELP_NEWSFAST
       mov       rsi,rax
       mov       rcx,rbx
       xor       edx,edx
       call      qword ptr [7FFA3EB277B0]; System.RuntimeType.GetCachedName(System.TypeNameKind)
       mov       rbx,rax
       mov       ecx,166
       mov       rdx,7FFA3EB17080
       call      CORINFO_HELP_STRCNS
       mov       rdx,rax
       mov       rcx,rbx
       call      qword ptr [7FFA3E77D788]; System.String.Concat(System.String, System.String)
       mov       rdx,rax
       mov       rcx,rsi
       call      qword ptr [7FFA3EA96C70]
       mov       rcx,rsi
       call      CORINFO_HELP_THROW
       int       3
M00_L108:
       call      System.Object.GetType()
       mov       rbx,rax
       mov       rcx,offset MT_System.InvalidOperationException
       call      CORINFO_HELP_NEWSFAST
       mov       rsi,rax
       mov       rcx,rbx
       xor       edx,edx
       call      qword ptr [7FFA3EB277B0]; System.RuntimeType.GetCachedName(System.TypeNameKind)
       mov       rbx,rax
       mov       ecx,166
       mov       rdx,7FFA3EB17080
       call      CORINFO_HELP_STRCNS
       mov       rdx,rax
       mov       rcx,rbx
       call      qword ptr [7FFA3E77D788]; System.String.Concat(System.String, System.String)
       mov       rdx,rax
       mov       rcx,rsi
       call      qword ptr [7FFA3EA96C70]
       mov       rcx,rsi
       call      CORINFO_HELP_THROW
       int       3
M00_L109:
       xor       edx,edx
       xor       r8d,r8d
       mov       rax,[rcx]
       mov       rax,[rax+48]
       call      qword ptr [rax]
       mov       rcx,[rbx+18]
M00_L110:
       mov       edx,2
       xor       r8d,r8d
       mov       rax,[rcx]
       mov       rax,[rax+48]
       call      qword ptr [rax]
       mov       rcx,[rbx+18]
M00_L111:
       mov       edx,1
       xor       r8d,r8d
       mov       rax,[rcx]
       mov       rax,[rax+48]
       call      qword ptr [rax]
       mov       r13,[rbx+18]
M00_L112:
       mov       rcx,r13
       xor       edx,edx
       xor       r8d,r8d
       mov       rax,[r13]
       mov       rax,[rax+48]
       call      qword ptr [rax]
       jmp       near ptr M00_L77
M00_L113:
       call      CORINFO_HELP_RNGCHKFAIL
       int       3
; Total bytes of code 3797
```
```assembly
; Benchmark.FastFsmHsmHistoryShallow.GetCompositeEntryTarget(Int32)
       sub       rsp,28
       mov       rax,214EEC009D0
       mov       r8,[rax]
M01_L00:
       cmp       edx,5
       jae       short M01_L01
       mov       rax,r8
       mov       r10d,edx
       cmp       dword ptr [rax+r10*4+10],0
       jge       short M01_L02
M01_L01:
       mov       eax,edx
       add       rsp,28
       ret
M01_L02:
       mov       rax,214EEC009D8
       mov       rax,[rax]
       mov       r10d,edx
       mov       eax,[rax+r10*4+10]
       test      eax,eax
       je        short M01_L06
       mov       r10,[rcx+18]
       cmp       edx,[r10+8]
       jae       short M01_L09
       mov       r9d,edx
       mov       r10d,[r10+r9*4+10]
       test      r10d,r10d
       jl        short M01_L06
       cmp       eax,1
       jne       short M01_L08
M01_L03:
       test      r10d,r10d
       jl        short M01_L05
       mov       rax,214EEC009C0
       mov       rax,[rax]
       cmp       r10d,5
       jae       short M01_L09
       mov       r9d,r10d
       cmp       [rax+r9*4+10],edx
       jne       short M01_L07
M01_L04:
       test      r10d,r10d
       jl        short M01_L01
       mov       edx,r10d
       jmp       short M01_L00
M01_L05:
       mov       r10,r8
       mov       eax,edx
       mov       r10d,[r10+rax*4+10]
       jmp       short M01_L04
M01_L06:
       mov       r10,r8
       mov       eax,edx
       mov       r10d,[r10+rax*4+10]
       jmp       short M01_L04
M01_L07:
       mov       rax,214EEC009C0
       mov       rax,[rax]
       mov       r10d,r10d
       mov       r10d,[rax+r10*4+10]
       jmp       short M01_L03
M01_L08:
       jmp       short M01_L04
M01_L09:
       call      CORINFO_HELP_RNGCHKFAIL
       int       3
; Total bytes of code 195
```
```assembly
; BenchmarkDotNet.Engines.DeadCodeEliminationHelper.KeepAliveWithoutBoxing[[Benchmark.HsmState, Benchmark]](Benchmark.HsmState)
       ret
; Total bytes of code 1
```
```assembly
; System.RuntimeType.GetCachedName(System.TypeNameKind)
       push      rbx
       sub       rsp,20
       mov       ebx,edx
       mov       rax,[rcx+10]
       test      rax,rax
       je        short M03_L01
       mov       rax,[rax]
       test      rax,rax
       je        short M03_L01
M03_L00:
       mov       rcx,rax
       mov       edx,ebx
       cmp       [rcx],ecx
       call      qword ptr [7FFA960C2D08]; Precode of System.RuntimeType+RuntimeTypeCache.GetName(System.TypeNameKind)
       nop
       add       rsp,20
       pop       rbx
       ret
M03_L01:
       call      qword ptr [7FFA960C29E8]; Precode of System.RuntimeType.InitializeCache()
       jmp       short M03_L00
; Total bytes of code 52
```
```assembly
; System.String.Concat(System.String, System.String)
       push      r15
       push      r14
       push      rdi
       push      rsi
       push      rbp
       push      rbx
       sub       rsp,28
       mov       rsi,rcx
       mov       rbx,rdx
       test      rsi,rsi
       je        near ptr M04_L02
       mov       edi,[rsi+8]
       test      edi,edi
       je        short M04_L02
       test      rbx,rbx
       je        short M04_L01
       mov       ebp,[rbx+8]
       test      ebp,ebp
       je        short M04_L01
       mov       r14d,edi
       lea       ecx,[r14+rbp]
       test      ecx,ecx
       jl        short M04_L00
       call      00007FFA3E7724D8
       mov       r15,rax
       cmp       [r15],r15b
       lea       rcx,[r15+0C]
       mov       r8d,edi
       add       r8,r8
       lea       rdx,[rsi+0C]
       call      qword ptr [7FFA3E7757B8]; System.SpanHelpers.Memmove(Byte ByRef, Byte ByRef, UIntPtr)
       movsxd    rcx,r14d
       lea       rcx,[r15+rcx*2+0C]
       mov       r8d,ebp
       add       r8,r8
       lea       rdx,[rbx+0C]
       call      qword ptr [7FFA3E7757B8]; System.SpanHelpers.Memmove(Byte ByRef, Byte ByRef, UIntPtr)
       mov       rax,r15
       add       rsp,28
       pop       rbx
       pop       rbp
       pop       rsi
       pop       rdi
       pop       r14
       pop       r15
       ret
M04_L00:
       call      qword ptr [7FFA3EC2C4F8]
       int       3
M04_L01:
       mov       rax,rsi
       add       rsp,28
       pop       rbx
       pop       rbp
       pop       rsi
       pop       rdi
       pop       r14
       pop       r15
       ret
M04_L02:
       test      rbx,rbx
       je        short M04_L03
       mov       ebp,[rbx+8]
       test      ebp,ebp
       sete      al
       movzx     eax,al
       test      eax,eax
       jne       short M04_L03
       mov       rax,rbx
       add       rsp,28
       pop       rbx
       pop       rbp
       pop       rsi
       pop       rdi
       pop       r14
       pop       r15
       ret
M04_L03:
       mov       rax,21480600008
       add       rsp,28
       pop       rbx
       pop       rbp
       pop       rsi
       pop       rdi
       pop       r14
       pop       r15
       ret
; Total bytes of code 210
```
```assembly
; System.SpanHelpers.Memmove(Byte ByRef, Byte ByRef, UIntPtr)
       mov       rax,rcx
       sub       rax,rdx
       cmp       rax,r8
       jb        near ptr M05_L12
       mov       rax,rdx
       sub       rax,rcx
       cmp       rax,r8
       jb        near ptr M05_L12
       lea       rax,[rdx+r8]
       lea       r10,[rcx+r8]
       cmp       r8,10
       ja        short M05_L05
       test      r8b,18
       je        short M05_L03
       mov       r8,[rdx]
       mov       [rcx],r8
       mov       rdx,[rax-8]
       mov       [r10-8],rdx
       jmp       short M05_L02
M05_L00:
       vmovups   xmm0,[rdx+20]
       vmovups   [rcx+20],xmm0
M05_L01:
       vmovups   xmm0,[rax-10]
       vmovups   [r10-10],xmm0
M05_L02:
       vzeroupper
       ret
M05_L03:
       test      r8b,4
       je        short M05_L04
       mov       r8d,[rdx]
       mov       [rcx],r8d
       mov       eax,[rax-4]
       mov       [r10-4],eax
       jmp       short M05_L02
M05_L04:
       test      r8,r8
       je        short M05_L02
       movzx     edx,byte ptr [rdx]
       mov       [rcx],dl
       test      r8b,2
       je        short M05_L02
       movsx     r8,word ptr [rax-2]
       mov       [r10-2],r8w
       jmp       short M05_L02
M05_L05:
       cmp       r8,40
       ja        short M05_L07
M05_L06:
       vmovups   xmm0,[rdx]
       vmovups   [rcx],xmm0
       cmp       r8,20
       jbe       short M05_L01
       jmp       short M05_L11
M05_L07:
       cmp       r8,800
       ja        near ptr M05_L13
       cmp       r8,100
       jae       short M05_L10
M05_L08:
       mov       r9,r8
       shr       r9,6
M05_L09:
       vmovdqu32 zmm0,[rdx]
       vmovdqu32 [rcx],zmm0
       add       rcx,40
       add       rdx,40
       dec       r9
       jne       short M05_L09
       and       r8,3F
       cmp       r8,10
       ja        short M05_L06
       jmp       near ptr M05_L01
M05_L10:
       mov       r9,rcx
       and       r9,3F
       neg       r9
       add       r9,40
       vmovdqu32 zmm0,[rdx]
       vmovdqu32 [rcx],zmm0
       add       rdx,r9
       add       rcx,r9
       sub       r8,r9
       jmp       short M05_L08
M05_L11:
       vmovups   xmm0,[rdx+10]
       vmovups   [rcx+10],xmm0
       cmp       r8,30
       jbe       near ptr M05_L01
       jmp       near ptr M05_L00
M05_L12:
       cmp       rcx,rdx
       jne       short M05_L13
       cmp       [rdx],dl
       jmp       near ptr M05_L02
M05_L13:
       cmp       [rcx],cl
       cmp       [rdx],dl
       vzeroupper
       jmp       qword ptr [7FFA3E776538]; System.Buffer._Memmove(Byte ByRef, Byte ByRef, UIntPtr)
; Total bytes of code 318
```
**Extern method**
System.Object.GetType()

## .NET 9.0.5 (9.0.525.21509), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
```assembly
; Benchmark.HsmBenchmarks.FastFSM_Hsm_Internal()
       push      rdi
       push      rsi
       push      rbx
       sub       rsp,20
       mov       rbx,rcx
       mov       rsi,offset MT_Benchmark.FastFsmHsmBasic
       mov       edi,400
M00_L00:
       mov       rcx,[rbx+8]
       cmp       [rcx],rsi
       jne       near ptr M00_L03
       cmp       byte ptr [rcx+14],0
       je        short M00_L02
       mov       edx,3
       xor       r8d,r8d
       call      qword ptr [7FFA3EB06158]; Benchmark.FastFsmHsmBasic.TryFireInternal(Benchmark.HsmTrigger, System.Object)
M00_L01:
       dec       edi
       jne       short M00_L00
       mov       rcx,[rbx+8]
       mov       ecx,[rcx+10]
       add       rsp,20
       pop       rbx
       pop       rsi
       pop       rdi
       jmp       qword ptr [7FFA3EC15188]; BenchmarkDotNet.Engines.DeadCodeEliminationHelper.KeepAliveWithoutBoxing[[Benchmark.HsmState, Benchmark]](Benchmark.HsmState)
M00_L02:
       call      System.Object.GetType()
       mov       rbx,rax
       mov       rcx,offset MT_System.InvalidOperationException
       call      CORINFO_HELP_NEWSFAST
       mov       rsi,rax
       mov       rcx,rbx
       xor       edx,edx
       call      qword ptr [7FFA3EB27A68]; System.RuntimeType.GetCachedName(System.TypeNameKind)
       mov       rdi,rax
       mov       ecx,166
       mov       rdx,7FFA3EB04E98
       call      CORINFO_HELP_STRCNS
       mov       rdx,rax
       mov       rcx,rdi
       call      qword ptr [7FFA3E76D788]; System.String.Concat(System.String, System.String)
       mov       rdx,rax
       mov       rcx,rsi
       call      qword ptr [7FFA3EA86CA0]
       mov       rcx,rsi
       call      CORINFO_HELP_THROW
       int       3
M00_L03:
       mov       edx,3
       xor       r8d,r8d
       mov       rax,[rcx]
       mov       rax,[rax+48]
       call      qword ptr [rax]
       jmp       near ptr M00_L01
; Total bytes of code 197
```
```assembly
; Benchmark.FastFsmHsmBasic.TryFireInternal(Benchmark.HsmTrigger, System.Object)
       push      rbp
       push      r15
       push      r14
       push      r13
       push      r12
       push      rdi
       push      rsi
       push      rbx
       sub       rsp,38
       lea       rbp,[rsp+20]
       mov       [rbp],rsp
       mov       rax,2056AB45FFF
       mov       [rbp+8],rax
       mov       rbx,rcx
M01_L00:
       xor       esi,esi
       mov       r8d,80000000
       mov       r10d,7FFFFFFF
       mov       r9d,7FFFFFFF
       xor       edi,edi
       mov       ecx,0FFFFFFFF
       xor       r14d,r14d
       mov       [rbp+14],r14d
       xor       r15d,r15d
       mov       r13d,[rbx+10]
       mov       [rbp+10],r13d
       mov       r11d,r13d
       test      r13d,r13d
       jl        near ptr M01_L26
M01_L01:
       cmp       r11d,r13d
       je        short M01_L03
       mov       rax,1892D000900
       mov       r12,[rax]
       mov       rax,r12
       cmp       r13d,5
       jae       near ptr M01_L63
       mov       r14d,r13d
       mov       eax,[rax+r14*4+10]
       cmp       r11d,5
       jae       near ptr M01_L63
       mov       r14d,r11d
       sub       eax,[r12+r14*4+10]
M01_L02:
       cmp       r11d,3
       ja        near ptr M01_L24
       mov       r12d,r11d
       lea       r13,[7FFA3E80C118]
       mov       r13d,[r13+r12*4]
       lea       r14,[M01_L00]
       add       r13,r14
       jmp       r13
M01_L03:
       xor       eax,eax
       jmp       short M01_L02
       cmp       edx,2
       mov       r13d,[rbp+10]
       jne       near ptr M01_L24
       test      esi,esi
       jne       short M01_L05
M01_L04:
       mov       esi,1
       xor       r8d,r8d
       mov       r10d,eax
       mov       r9d,r15d
       xor       edi,edi
       mov       ecx,2
       jmp       near ptr M01_L22
M01_L05:
       test      r8d,r8d
       jl        short M01_L04
       test      r8d,r8d
       jne       short M01_L07
       cmp       eax,r10d
       jl        short M01_L04
       cmp       eax,r10d
       jne       short M01_L06
       cmp       r15d,r9d
       mov       r14d,[rbp+14]
       jge       near ptr M01_L23
       jmp       short M01_L04
M01_L06:
       mov       r14d,[rbp+14]
       jmp       near ptr M01_L23
M01_L07:
       mov       r14d,[rbp+14]
       jmp       near ptr M01_L23
       cmp       edx,2
       mov       r13d,[rbp+10]
       jne       near ptr M01_L24
       test      esi,esi
       jne       short M01_L09
M01_L08:
       mov       esi,1
       xor       r8d,r8d
       mov       r10d,eax
       mov       r9d,r15d
       xor       edi,edi
       mov       ecx,3
       jmp       near ptr M01_L22
M01_L09:
       test      r8d,r8d
       jl        short M01_L08
       test      r8d,r8d
       jne       short M01_L11
       cmp       eax,r10d
       jl        short M01_L08
       cmp       eax,r10d
       jne       short M01_L10
       cmp       r15d,r9d
       mov       r14d,[rbp+14]
       jge       near ptr M01_L23
       jmp       short M01_L08
M01_L10:
       mov       r14d,[rbp+14]
       jmp       near ptr M01_L23
M01_L11:
       mov       r14d,[rbp+14]
       jmp       near ptr M01_L23
       cmp       edx,1
       je        short M01_L16
       cmp       edx,3
       mov       r13d,[rbp+10]
       jne       near ptr M01_L24
       test      esi,esi
       jne       short M01_L13
M01_L12:
       mov       esi,1
       xor       r8d,r8d
       mov       r10d,eax
       mov       r9d,r15d
       mov       edi,1
       mov       r14d,1
       jmp       near ptr M01_L23
M01_L13:
       test      r8d,r8d
       jl        short M01_L12
       test      r8d,r8d
       jne       short M01_L15
       cmp       eax,r10d
       jl        short M01_L12
       cmp       eax,r10d
       jne       short M01_L14
       cmp       r15d,r9d
       mov       r14d,[rbp+14]
       jge       near ptr M01_L23
       jmp       short M01_L12
M01_L14:
       mov       r14d,[rbp+14]
       jmp       near ptr M01_L23
M01_L15:
       mov       r14d,[rbp+14]
       jmp       short M01_L23
M01_L16:
       test      esi,esi
       jne       short M01_L18
M01_L17:
       mov       esi,1
       xor       r8d,r8d
       mov       r10d,eax
       mov       r9d,r15d
       xor       edi,edi
       xor       ecx,ecx
       mov       r13d,[rbp+10]
       jmp       short M01_L22
M01_L18:
       test      r8d,r8d
       jl        short M01_L17
       test      r8d,r8d
       jne       short M01_L20
       cmp       eax,r10d
       jl        short M01_L17
       cmp       eax,r10d
       jne       short M01_L19
       cmp       r15d,r9d
       mov       r14d,[rbp+14]
       jl        short M01_L17
       mov       r13d,[rbp+10]
       jmp       short M01_L23
M01_L19:
       mov       r13d,[rbp+10]
       mov       r14d,[rbp+14]
       jmp       short M01_L23
M01_L20:
       mov       r13d,[rbp+10]
       mov       r14d,[rbp+14]
       jmp       short M01_L23
       test      edx,edx
       mov       r13d,[rbp+10]
       jne       short M01_L24
       test      esi,esi
       jne       short M01_L27
M01_L21:
       mov       esi,1
       xor       r8d,r8d
       mov       r10d,eax
       mov       r9d,r15d
       xor       edi,edi
       mov       ecx,1
M01_L22:
       xor       r14d,r14d
M01_L23:
       inc       r15d
       mov       [rbp+14],r14d
M01_L24:
       mov       rax,1892D0008F8
       mov       r12,[rax]
       mov       eax,5
       cmp       eax,r11d
       ja        short M01_L28
       mov       r11d,0FFFFFFFF
M01_L25:
       test      r11d,r11d
       jge       near ptr M01_L01
M01_L26:
       test      esi,esi
       je        short M01_L32
       test      edi,edi
       je        near ptr M01_L34
       mov       r14d,[rbp+14]
       test      r14d,r14d
       je        short M01_L30
       cmp       r14d,1
       jne       short M01_L30
       jmp       short M01_L29
M01_L27:
       test      r8d,r8d
       jl        short M01_L21
       test      r8d,r8d
       mov       r14d,[rbp+14]
       jne       short M01_L23
       cmp       eax,r10d
       mov       [rbp+14],r14d
       jl        short M01_L21
       cmp       eax,r10d
       mov       r14d,[rbp+14]
       jne       short M01_L23
       cmp       r15d,r9d
       jge       short M01_L23
       jmp       near ptr M01_L21
M01_L28:
       mov       eax,r11d
       mov       r11d,[r12+rax*4+10]
       jmp       short M01_L25
M01_L29:
       jmp       short M01_L30
M01_L30:
       mov       eax,1
       mov       rcx,2056AB45FFF
       cmp       [rbp+8],rcx
       je        short M01_L31
       call      CORINFO_HELP_FAIL_FAST
M01_L31:
       nop
       lea       rsp,[rbp+18]
       pop       rbx
       pop       rsi
       pop       rdi
       pop       r12
       pop       r13
       pop       r14
       pop       r15
       pop       rbp
       ret
M01_L32:
       xor       eax,eax
       mov       rcx,2056AB45FFF
       cmp       [rbp+8],rcx
       je        short M01_L33
       call      CORINFO_HELP_FAIL_FAST
M01_L33:
       nop
       lea       rsp,[rbp+18]
       pop       rbx
       pop       rsi
       pop       rdi
       pop       r12
       pop       r13
       pop       r14
       pop       r15
       pop       rbp
       ret
M01_L34:
       mov       edx,[rbx+10]
       cmp       edx,ecx
       jne       near ptr M01_L42
       mov       esi,edx
M01_L35:
       mov       eax,[rbx+10]
       mov       r8,1892D0008F8
       mov       r12,[r8]
       mov       r8d,5
       cmp       r8d,eax
       ja        near ptr M01_L49
       mov       r8d,0FFFFFFFF
M01_L36:
       test      r8d,r8d
       jl        short M01_L39
       mov       r10,1892D000910
       mov       r10,[r10]
M01_L37:
       mov       r9,r10
       cmp       r8d,5
       jae       near ptr M01_L63
       mov       r11d,r8d
       cmp       dword ptr [r9+r11*4+10],0
       je        short M01_L38
       mov       r9,[rbx+18]
       cmp       r8d,[r9+8]
       jae       near ptr M01_L63
       mov       r11d,r8d
       mov       [r9+r11*4+10],eax
M01_L38:
       mov       r9,r12
       mov       r8d,r8d
       mov       r8d,[r9+r8*4+10]
       test      r8d,r8d
       jge       short M01_L37
M01_L39:
       jmp       short M01_L41
M01_L40:
       mov       rax,r12
       cmp       edx,5
       jae       near ptr M01_L63
       mov       edx,edx
       mov       edx,[rax+rdx*4+10]
M01_L41:
       cmp       edx,esi
       je        near ptr M01_L50
       test      edx,edx
       jge       short M01_L40
       jmp       near ptr M01_L50
M01_L42:
       mov       esi,edx
       mov       eax,ecx
       mov       r8,1892D000900
       mov       r12,[r8]
       jmp       short M01_L44
M01_L43:
       mov       r8,1892D0008F8
       mov       r8,[r8]
       mov       r15,r8
       mov       r8,r15
       mov       r10d,esi
       mov       esi,[r8+r10*4+10]
M01_L44:
       mov       r8,r12
       cmp       esi,5
       jae       near ptr M01_L63
       mov       r10d,esi
       mov       r8d,[r8+r10*4+10]
       mov       r10,r12
       cmp       ecx,5
       jae       near ptr M01_L63
       mov       r9d,ecx
       cmp       r8d,[r10+r9*4+10]
       jg        short M01_L43
       jmp       short M01_L46
M01_L45:
       mov       r10,1892D0008F8
       mov       r15,[r10]
       mov       r10,r15
       mov       eax,eax
       mov       eax,[r10+rax*4+10]
M01_L46:
       mov       r10,r12
       cmp       eax,5
       jae       near ptr M01_L63
       mov       r9d,eax
       cmp       [r10+r9*4+10],r8d
       jg        short M01_L45
       jmp       short M01_L48
M01_L47:
       mov       r8,1892D0008F8
       mov       r15,[r8]
       mov       r8,r15
       cmp       esi,5
       jae       near ptr M01_L63
       mov       r10d,esi
       mov       esi,[r8+r10*4+10]
       mov       r8,r15
       cmp       eax,5
       jae       near ptr M01_L63
       mov       eax,eax
       mov       eax,[r8+rax*4+10]
M01_L48:
       cmp       esi,eax
       jne       short M01_L47
       jmp       near ptr M01_L35
M01_L49:
       mov       r8,r12
       mov       r10d,eax
       mov       r8d,[r8+r10*4+10]
       jmp       near ptr M01_L36
M01_L50:
       mov       [rbx+10],ecx
       mov       edx,[rbx+10]
       mov       rcx,rbx
       call      qword ptr [7FFA3EA8FE88]; Benchmark.FastFsmHsmBasic.GetCompositeEntryTarget(Int32)
       mov       [rbx+10],eax
       xor       ecx,ecx
       mov       rax,1892D000900
       mov       rax,[rax]
       mov       r8d,[rbx+10]
       mov       edx,r8d
       cmp       edx,5
       jae       near ptr M01_L63
       mov       r10d,[rax+rdx*4+10]
       inc       r10d
       mov       eax,r10d
       mov       r9d,4
       mul       r9
       jb        near ptr M01_L62
       test      rax,rax
       je        short M01_L52
       add       rax,0F
       shr       rax,4
       add       rsp,20
M01_L51:
       push      0
       push      0
       dec       rax
       jne       short M01_L51
       sub       rsp,20
       lea       rax,[rsp+20]
M01_L52:
       test      r10d,r10d
       jl        short M01_L59
       jmp       short M01_L55
M01_L53:
       mov       rcx,r12
       mov       r8d,r8d
       mov       r8d,[rcx+r8*4+10]
M01_L54:
       mov       ecx,edx
M01_L55:
       test      r8d,r8d
       jl        short M01_L56
       cmp       r8d,esi
       je        short M01_L56
       lea       edx,[rcx+1]
       cmp       ecx,r10d
       jae       short M01_L63
       mov       ecx,ecx
       mov       [rax+rcx*4],r8d
       cmp       r8d,5
       jl        short M01_L53
       mov       r8d,0FFFFFFFF
       jmp       short M01_L54
M01_L56:
       dec       ecx
       js        short M01_L58
M01_L57:
       cmp       ecx,r10d
       jae       short M01_L63
       mov       edx,[rax+rcx*4]
       dec       ecx
       jns       short M01_L57
M01_L58:
       mov       r14d,[rbp+14]
       test      r14d,r14d
       je        near ptr M01_L30
       cmp       r14d,1
       jne       near ptr M01_L30
       jmp       near ptr M01_L30
M01_L59:
       call      qword ptr [7FFA3E977798]
       int       3
M01_L60:
       xor       eax,eax
       mov       rcx,2056AB45FFF
       cmp       [rbp+8],rcx
       je        short M01_L61
       call      CORINFO_HELP_FAIL_FAST
M01_L61:
       nop
       lea       rsp,[rbp+18]
       pop       rbx
       pop       rsi
       pop       rdi
       pop       r12
       pop       r13
       pop       r14
       pop       r15
       pop       rbp
       ret
M01_L62:
       call      CORINFO_HELP_OVERFLOW
       int       3
M01_L63:
       call      CORINFO_HELP_RNGCHKFAIL
       int       3
       push      rbp
       push      r15
       push      r14
       push      r13
       push      r12
       push      rdi
       push      rsi
       push      rbx
       sub       rsp,28
       mov       rbp,[rcx+20]
       mov       [rsp+20],rbp
       lea       rbp,[rbp+20]
       lea       rax,[M01_L60]
       add       rsp,28
       pop       rbx
       pop       rsi
       pop       rdi
       pop       r12
       pop       r13
       pop       r14
       pop       r15
       pop       rbp
       ret
       push      rbp
       push      r15
       push      r14
       push      r13
       push      r12
       push      rdi
       push      rsi
       push      rbx
       sub       rsp,28
       mov       rbp,[rcx+20]
       mov       [rsp+20],rbp
       lea       rbp,[rbp+20]
       lea       rax,[M01_L30]
       add       rsp,28
       pop       rbx
       pop       rsi
       pop       rdi
       pop       r12
       pop       r13
       pop       r14
       pop       r15
       pop       rbp
       ret
; Total bytes of code 1583
```
```assembly
; BenchmarkDotNet.Engines.DeadCodeEliminationHelper.KeepAliveWithoutBoxing[[Benchmark.HsmState, Benchmark]](Benchmark.HsmState)
       ret
; Total bytes of code 1
```
```assembly
; System.RuntimeType.GetCachedName(System.TypeNameKind)
       push      rbx
       sub       rsp,20
       mov       ebx,edx
       mov       rax,[rcx+10]
       test      rax,rax
       je        short M03_L01
       mov       rax,[rax]
       test      rax,rax
       je        short M03_L01
M03_L00:
       mov       rcx,rax
       mov       edx,ebx
       cmp       [rcx],ecx
       call      qword ptr [7FFA960C2D08]; Precode of System.RuntimeType+RuntimeTypeCache.GetName(System.TypeNameKind)
       nop
       add       rsp,20
       pop       rbx
       ret
M03_L01:
       call      qword ptr [7FFA960C29E8]; Precode of System.RuntimeType.InitializeCache()
       jmp       short M03_L00
; Total bytes of code 52
```
```assembly
; System.String.Concat(System.String, System.String)
       push      r15
       push      r14
       push      rdi
       push      rsi
       push      rbp
       push      rbx
       sub       rsp,28
       mov       rsi,rcx
       mov       rbx,rdx
       test      rsi,rsi
       je        near ptr M04_L02
       mov       edi,[rsi+8]
       test      edi,edi
       je        short M04_L02
       test      rbx,rbx
       je        short M04_L01
       mov       ebp,[rbx+8]
       test      ebp,ebp
       je        short M04_L01
       mov       r14d,edi
       lea       ecx,[r14+rbp]
       test      ecx,ecx
       jl        short M04_L00
       call      00007FFA3E7624D8
       mov       r15,rax
       cmp       [r15],r15b
       lea       rcx,[r15+0C]
       mov       r8d,edi
       add       r8,r8
       lea       rdx,[rsi+0C]
       call      qword ptr [7FFA3E7657B8]; System.SpanHelpers.Memmove(Byte ByRef, Byte ByRef, UIntPtr)
       movsxd    rcx,r14d
       lea       rcx,[r15+rcx*2+0C]
       mov       r8d,ebp
       add       r8,r8
       lea       rdx,[rbx+0C]
       call      qword ptr [7FFA3E7657B8]; System.SpanHelpers.Memmove(Byte ByRef, Byte ByRef, UIntPtr)
       mov       rax,r15
       add       rsp,28
       pop       rbx
       pop       rbp
       pop       rsi
       pop       rdi
       pop       r14
       pop       r15
       ret
M04_L00:
       call      qword ptr [7FFA3EC1C768]
       int       3
M04_L01:
       mov       rax,rsi
       add       rsp,28
       pop       rbx
       pop       rbp
       pop       rsi
       pop       rdi
       pop       r14
       pop       r15
       ret
M04_L02:
       test      rbx,rbx
       je        short M04_L03
       mov       ebp,[rbx+8]
       test      ebp,ebp
       sete      al
       movzx     eax,al
       test      eax,eax
       jne       short M04_L03
       mov       rax,rbx
       add       rsp,28
       pop       rbx
       pop       rbp
       pop       rsi
       pop       rdi
       pop       r14
       pop       r15
       ret
M04_L03:
       mov       rax,18900700008
       add       rsp,28
       pop       rbx
       pop       rbp
       pop       rsi
       pop       rdi
       pop       r14
       pop       r15
       ret
; Total bytes of code 210
```
```assembly
; Benchmark.FastFsmHsmBasic.GetCompositeEntryTarget(Int32)
       push      rbp
       sub       rsp,80
       lea       rbp,[rsp+80]
       vxorps    xmm4,xmm4,xmm4
       vmovdqa   xmmword ptr [rbp-50],xmm4
       xor       eax,eax
       mov       [rbp-40],rax
       mov       [rbp+10],rcx
       mov       [rbp+18],edx
       mov       eax,[rbp+18]
       mov       [rbp-3C],eax
       mov       dword ptr [rbp-58],3E8
M05_L00:
       mov       eax,[rbp-58]
       dec       eax
       mov       [rbp-58],eax
       cmp       dword ptr [rbp-58],0
       jg        short M05_L01
       lea       rcx,[rbp-58]
       mov       edx,2
       call      CORINFO_HELP_PATCHPOINT
M05_L01:
       mov       rax,1892D000908
       mov       rax,[rax]
       mov       eax,[rax+8]
       cmp       eax,[rbp-3C]
       jbe       short M05_L02
       mov       rax,1892D000908
       mov       rax,[rax]
       mov       ecx,[rbp-3C]
       cmp       ecx,[rax+8]
       jae       near ptr M05_L15
       mov       edx,ecx
       lea       rax,[rax+rdx*4+10]
       cmp       dword ptr [rax],0
       jge       short M05_L03
       mov       rcx,7FFA3EB0C4B8
       call      CORINFO_HELP_COUNTPROFILE32
M05_L02:
       mov       rcx,7FFA3EB0C4BC
       call      CORINFO_HELP_COUNTPROFILE32
       mov       eax,[rbp-3C]
       add       rsp,80
       pop       rbp
       ret
M05_L03:
       mov       rax,1892D000910
       mov       rax,[rax]
       mov       ecx,[rbp-3C]
       cmp       ecx,[rax+8]
       jae       near ptr M05_L15
       mov       edx,ecx
       lea       rax,[rax+rdx*4+10]
       mov       eax,[rax]
       mov       [rbp-40],eax
       mov       dword ptr [rbp-44],0FFFFFFFF
       cmp       dword ptr [rbp-40],0
       je        near ptr M05_L12
       mov       rax,[rbp+10]
       mov       rax,[rax+18]
       mov       ecx,[rbp-3C]
       cmp       ecx,[rax+8]
       jae       near ptr M05_L15
       mov       edx,ecx
       lea       rax,[rax+rdx*4+10]
       cmp       dword ptr [rax],0
       jl        short M05_L04
       mov       rax,[rbp+10]
       mov       rax,[rax+18]
       mov       ecx,[rbp-3C]
       cmp       ecx,[rax+8]
       jae       near ptr M05_L15
       mov       edx,ecx
       lea       rax,[rax+rdx*4+10]
       mov       eax,[rax]
       mov       [rbp-48],eax
       cmp       dword ptr [rbp-40],1
       jne       near ptr M05_L11
       mov       eax,[rbp-48]
       mov       [rbp-4C],eax
       jmp       short M05_L06
M05_L04:
       mov       rcx,7FFA3EB0C4C0
       call      CORINFO_HELP_COUNTPROFILE32
       jmp       near ptr M05_L12
M05_L05:
       mov       rcx,7FFA3EB0C4C4
       call      CORINFO_HELP_COUNTPROFILE32
       mov       rax,1892D0008F8
       mov       rax,[rax]
       mov       ecx,[rbp-4C]
       cmp       ecx,[rax+8]
       jae       near ptr M05_L15
       mov       edx,ecx
       lea       rax,[rax+rdx*4+10]
       mov       eax,[rax]
       mov       [rbp-4C],eax
M05_L06:
       cmp       dword ptr [rbp-4C],0
       jl        short M05_L08
       mov       eax,[rbp-58]
       dec       eax
       mov       [rbp-58],eax
       cmp       dword ptr [rbp-58],0
       jg        short M05_L07
       lea       rcx,[rbp-58]
       mov       edx,51
       call      CORINFO_HELP_PATCHPOINT
M05_L07:
       mov       rax,1892D0008F8
       mov       rax,[rax]
       mov       ecx,[rbp-4C]
       cmp       ecx,[rax+8]
       jae       near ptr M05_L15
       mov       edx,ecx
       lea       rax,[rax+rdx*4+10]
       mov       eax,[rax]
       cmp       eax,[rbp-3C]
       jne       short M05_L05
       mov       rcx,7FFA3EB0C4C8
       call      CORINFO_HELP_COUNTPROFILE32
M05_L08:
       cmp       dword ptr [rbp-4C],0
       jge       short M05_L09
       mov       rax,1892D000908
       mov       rax,[rax]
       mov       ecx,[rbp-3C]
       cmp       ecx,[rax+8]
       jae       near ptr M05_L15
       mov       edx,ecx
       lea       rax,[rax+rdx*4+10]
       mov       eax,[rax]
       mov       [rbp-50],eax
       jmp       short M05_L10
M05_L09:
       mov       rcx,7FFA3EB0C4CC
       call      CORINFO_HELP_COUNTPROFILE32
       mov       eax,[rbp-4C]
       mov       [rbp-50],eax
M05_L10:
       mov       rcx,7FFA3EB0C4D0
       call      CORINFO_HELP_COUNTPROFILE32
       mov       eax,[rbp-50]
       mov       [rbp-44],eax
       jmp       short M05_L13
M05_L11:
       mov       rcx,7FFA3EB0C4D4
       call      CORINFO_HELP_COUNTPROFILE32
       mov       eax,[rbp-48]
       mov       [rbp-44],eax
       jmp       short M05_L13
M05_L12:
       mov       rax,1892D000908
       mov       rax,[rax]
       mov       ecx,[rbp-3C]
       cmp       ecx,[rax+8]
       jae       short M05_L15
       mov       edx,ecx
       lea       rax,[rax+rdx*4+10]
       mov       eax,[rax]
       mov       [rbp-44],eax
M05_L13:
       cmp       dword ptr [rbp-44],0
       jge       short M05_L14
       mov       rcx,7FFA3EB0C4D8
       call      CORINFO_HELP_COUNTPROFILE32
       mov       eax,[rbp-3C]
       add       rsp,80
       pop       rbp
       ret
M05_L14:
       mov       rcx,7FFA3EB0C4DC
       call      CORINFO_HELP_COUNTPROFILE32
       mov       eax,[rbp-44]
       mov       [rbp-3C],eax
       jmp       near ptr M05_L00
M05_L15:
       call      CORINFO_HELP_RNGCHKFAIL
       int       3
; Total bytes of code 685
```
```assembly
; System.SpanHelpers.Memmove(Byte ByRef, Byte ByRef, UIntPtr)
       mov       rax,rcx
       sub       rax,rdx
       cmp       rax,r8
       jb        near ptr M06_L12
       mov       rax,rdx
       sub       rax,rcx
       cmp       rax,r8
       jb        near ptr M06_L12
       lea       rax,[rdx+r8]
       lea       r10,[rcx+r8]
       cmp       r8,10
       ja        short M06_L05
       test      r8b,18
       jne       short M06_L00
       test      r8b,4
       je        short M06_L04
       mov       edx,[rdx]
       mov       [rcx],edx
       mov       ecx,[rax-4]
       mov       [r10-4],ecx
       jmp       short M06_L03
M06_L00:
       mov       r8,[rdx]
       mov       [rcx],r8
       mov       rax,[rax-8]
       mov       [r10-8],rax
       jmp       short M06_L03
M06_L01:
       vmovups   xmm0,[rdx+20]
       vmovups   [rcx+20],xmm0
M06_L02:
       vmovups   xmm0,[rax-10]
       vmovups   [r10-10],xmm0
M06_L03:
       vzeroupper
       ret
M06_L04:
       test      r8,r8
       je        short M06_L03
       movzx     edx,byte ptr [rdx]
       mov       [rcx],dl
       test      r8b,2
       je        short M06_L03
       movsx     r8,word ptr [rax-2]
       mov       [r10-2],r8w
       jmp       short M06_L03
M06_L05:
       cmp       r8,40
       ja        short M06_L07
M06_L06:
       vmovups   xmm0,[rdx]
       vmovups   [rcx],xmm0
       cmp       r8,20
       jbe       short M06_L02
       jmp       short M06_L10
M06_L07:
       cmp       r8,800
       ja        near ptr M06_L13
       cmp       r8,100
       jae       short M06_L11
M06_L08:
       mov       r9,r8
       shr       r9,6
M06_L09:
       vmovdqu32 zmm0,[rdx]
       vmovdqu32 [rcx],zmm0
       add       rcx,40
       add       rdx,40
       dec       r9
       jne       short M06_L09
       and       r8,3F
       cmp       r8,10
       ja        short M06_L06
       jmp       near ptr M06_L02
M06_L10:
       vmovups   xmm0,[rdx+10]
       vmovups   [rcx+10],xmm0
       cmp       r8,30
       jbe       near ptr M06_L02
       jmp       near ptr M06_L01
M06_L11:
       mov       r9,rcx
       and       r9,3F
       neg       r9
       add       r9,40
       vmovdqu32 zmm0,[rdx]
       vmovdqu32 [rcx],zmm0
       add       rdx,r9
       add       rcx,r9
       sub       r8,r9
       jmp       short M06_L08
M06_L12:
       cmp       rcx,rdx
       jne       short M06_L13
       cmp       [rdx],dl
       jmp       near ptr M06_L03
M06_L13:
       cmp       [rcx],cl
       cmp       [rdx],dl
       vzeroupper
       jmp       qword ptr [7FFA3E766538]; System.Buffer._Memmove(Byte ByRef, Byte ByRef, UIntPtr)
; Total bytes of code 316
```
**Extern method**
System.Object.GetType()

## .NET 9.0.5 (9.0.525.21509), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
```assembly
; Benchmark.HsmBenchmarks.Stateless_Hsm_Internal()
       push      rdi
       push      rsi
       push      rbp
       push      rbx
       sub       rsp,28
       mov       rbx,rcx
       mov       rsi,offset MT_System.Object[]
       mov       edi,400
M00_L00:
       mov       rcx,[rbx+20]
       mov       rbp,[rcx+8]
       cmp       [rbp],bpl
       mov       rcx,rsi
       xor       edx,edx
       call      CORINFO_HELP_NEWARR_1_OBJ
       mov       ecx,[rbp+4C]
       test      ecx,ecx
       je        short M00_L03
       cmp       ecx,1
       jne       short M00_L02
       mov       rcx,rbp
       mov       r8,rax
       mov       edx,3
       call      qword ptr [7FFA3EB45218]; Stateless.StateMachine`2[[Benchmark.HsmState, Benchmark],[Benchmark.HsmTrigger, Benchmark]].InternalFireQueued(Benchmark.HsmTrigger, System.Object[])
M00_L01:
       dec       edi
       jne       short M00_L00
       mov       rdx,[rbx+20]
       mov       rdx,[rdx+8]
       mov       rcx,7FFA3EC459C8
       add       rsp,28
       pop       rbx
       pop       rbp
       pop       rsi
       pop       rdi
       jmp       qword ptr [7FFA3EC35158]; BenchmarkDotNet.Engines.DeadCodeEliminationHelper.KeepAliveWithoutBoxing[[System.__Canon, System.Private.CoreLib]](System.__Canon)
M00_L02:
       mov       rcx,offset MT_System.InvalidOperationException
       call      CORINFO_HELP_NEWSFAST
       mov       rbp,rax
       mov       ecx,9B
       mov       rdx,7FFA3EB26780
       call      CORINFO_HELP_STRCNS
       mov       rdx,rax
       mov       rcx,rbp
       call      qword ptr [7FFA3EAA6C70]
       mov       rcx,rbp
       call      CORINFO_HELP_THROW
       int       3
M00_L03:
       mov       rcx,rbp
       mov       r8,rax
       mov       edx,3
       call      qword ptr [7FFA3EB45200]; Stateless.StateMachine`2[[Benchmark.HsmState, Benchmark],[Benchmark.HsmTrigger, Benchmark]].InternalFireOne(Benchmark.HsmTrigger, System.Object[])
       jmp       short M00_L01
; Total bytes of code 191
```
```assembly
; Stateless.StateMachine`2[[Benchmark.HsmState, Benchmark],[Benchmark.HsmTrigger, Benchmark]].InternalFireQueued(Benchmark.HsmTrigger, System.Object[])
       push      rbp
       push      r15
       push      r14
       push      rdi
       push      rsi
       push      rbx
       sub       rsp,28
       lea       rbp,[rsp+50]
       mov       [rbp-30],rsp
       mov       [rbp+10],rcx
       mov       rbx,rcx
       mov       edi,edx
       mov       rsi,r8
       mov       rcx,offset MT_Stateless.StateMachine<Benchmark.HsmState, Benchmark.HsmTrigger>+QueuedTrigger
       call      CORINFO_HELP_NEWSFAST
       mov       r14,rax
       mov       r15,[rbx+40]
       mov       [r14+10],edi
       lea       rcx,[r14+8]
       mov       rdx,rsi
       call      CORINFO_HELP_ASSIGN_REF
       mov       edx,[r15+18]
       mov       rcx,[r15+8]
       cmp       edx,[rcx+8]
       je        near ptr M01_L07
M01_L00:
       movsxd    rdx,dword ptr [r15+14]
       mov       rcx,[r15+8]
       mov       r8,r14
       call      System.Runtime.CompilerServices.CastHelpers.StelemRef(System.Object[], IntPtr, System.Object)
       lea       rdx,[r15+14]
       mov       ecx,[rdx]
       inc       ecx
       mov       rax,[r15+8]
       xor       r8d,r8d
       cmp       [rax+8],ecx
       cmove     ecx,r8d
       mov       [rdx],ecx
       inc       dword ptr [r15+18]
       inc       dword ptr [r15+1C]
       cmp       byte ptr [rbx+50],0
       jne       near ptr M01_L08
       mov       byte ptr [rbx+50],1
       mov       rdx,[rbx+40]
       mov       rcx,7FFA3EB83D68
       call      qword ptr [7FFA3EB45230]; System.Linq.Enumerable.Any[[System.__Canon, System.Private.CoreLib]](System.Collections.Generic.IEnumerable`1<System.__Canon>)
       test      eax,eax
       je        near ptr M01_L06
M01_L01:
       mov       rcx,[rbx+40]
       mov       r8d,[rcx+10]
       mov       rdx,[rcx+8]
       mov       rax,rdx
       cmp       dword ptr [rcx+18],0
       je        short M01_L05
       mov       r10d,[rax+8]
       cmp       r8d,r10d
       jae       short M01_L04
       mov       r9d,r8d
       mov       r9,[rax+r9*8+10]
       movsxd    r11,r8d
       cmp       r11,r10
       jae       short M01_L04
       movsxd    r8,r8d
       xor       r10d,r10d
       mov       [rax+r8*8+10],r10
       lea       r8,[rcx+10]
       mov       eax,[r8]
       inc       eax
       cmp       [rdx+8],eax
       je        short M01_L03
M01_L02:
       mov       [r8],eax
       dec       dword ptr [rcx+18]
       inc       dword ptr [rcx+1C]
       mov       r8,[r9+8]
       mov       edx,[r9+10]
       mov       rcx,rbx
       call      qword ptr [7FFA3EB45200]; Stateless.StateMachine`2[[Benchmark.HsmState, Benchmark],[Benchmark.HsmTrigger, Benchmark]].InternalFireOne(Benchmark.HsmTrigger, System.Object[])
       mov       rdx,[rbx+40]
       mov       rcx,7FFA3EB83D68
       call      qword ptr [7FFA3EB45230]; System.Linq.Enumerable.Any[[System.__Canon, System.Private.CoreLib]](System.Collections.Generic.IEnumerable`1<System.__Canon>)
       test      eax,eax
       jne       short M01_L01
       jmp       short M01_L06
M01_L03:
       xor       eax,eax
       jmp       short M01_L02
M01_L04:
       call      CORINFO_HELP_RNGCHKFAIL
       int       3
M01_L05:
       call      qword ptr [7FFA3EC3D3C8]
       int       3
M01_L06:
       mov       byte ptr [rbx+50],0
       jmp       short M01_L08
M01_L07:
       mov       edx,[r15+18]
       inc       edx
       mov       rcx,r15
       call      qword ptr [7FFA3EB45320]; System.Collections.Generic.Queue`1[[System.__Canon, System.Private.CoreLib]].Grow(Int32)
       jmp       near ptr M01_L00
M01_L08:
       add       rsp,28
       pop       rbx
       pop       rsi
       pop       rdi
       pop       r14
       pop       r15
       pop       rbp
       ret
       push      rbp
       push      r15
       push      r14
       push      rdi
       push      rsi
       push      rbx
       sub       rsp,28
       mov       rbp,[rcx+20]
       mov       [rsp+20],rbp
       lea       rbp,[rbp+50]
       mov       rbx,[rbp+10]
       mov       byte ptr [rbx+50],0
       add       rsp,28
       pop       rbx
       pop       rsi
       pop       rdi
       pop       r14
       pop       r15
       pop       rbp
       ret
; Total bytes of code 403
```
```assembly
; BenchmarkDotNet.Engines.DeadCodeEliminationHelper.KeepAliveWithoutBoxing[[System.__Canon, System.Private.CoreLib]](System.__Canon)
       ret
; Total bytes of code 1
```
```assembly
; Stateless.StateMachine`2[[Benchmark.HsmState, Benchmark],[Benchmark.HsmTrigger, Benchmark]].InternalFireOne(Benchmark.HsmTrigger, System.Object[])
       push      r15
       push      r14
       push      r13
       push      r12
       push      rdi
       push      rsi
       push      rbp
       push      rbx
       sub       rsp,88
       vxorps    xmm4,xmm4,xmm4
       vmovdqu   ymmword ptr [rsp+50],ymm4
       vmovdqa   xmmword ptr [rsp+70],xmm4
       xor       eax,eax
       mov       [rsp+80],rax
       mov       rbx,rcx
       mov       edi,edx
       mov       rsi,r8
       mov       rcx,offset MT_Stateless.StateMachine<Benchmark.HsmState, Benchmark.HsmTrigger>+<>c__DisplayClass74_0
       call      CORINFO_HELP_NEWSFAST
       mov       rbp,rax
       mov       [rbp+24],edi
       lea       rcx,[rbp+8]
       mov       rdx,rsi
       call      CORINFO_HELP_ASSIGN_REF
       lea       rcx,[rbp+10]
       mov       rdx,rbx
       call      CORINFO_HELP_ASSIGN_REF
       mov       rsi,[rbx+10]
       mov       edi,[rbp+24]
       mov       rcx,offset MT_System.Collections.Generic.Dictionary<Benchmark.HsmTrigger, Stateless.StateMachine<Benchmark.HsmState, Benchmark.HsmTrigger>+TriggerWithParameters>
       cmp       [rsi],rcx
       jne       near ptr M03_L48
       cmp       qword ptr [rsi+8],0
       je        near ptr M03_L04
       mov       r14,[rsi+18]
       test      r14,r14
       jne       near ptr M03_L45
       mov       eax,edi
       mov       rcx,[rsi+8]
       mov       edx,edi
       imul      rdx,[rsi+30]
       shr       rdx,20
       inc       rdx
       mov       r8d,[rcx+8]
       imul      rdx,r8
       shr       rdx,20
       cmp       edx,[rcx+8]
       jae       near ptr M03_L93
       mov       edx,edx
       lea       rcx,[rcx+rdx*4+10]
       mov       r14d,[rcx]
       mov       rsi,[rsi+10]
       xor       r15d,r15d
       dec       r14d
       mov       r13d,[rsi+8]
M03_L00:
       cmp       r13d,r14d
       jbe       short M03_L04
       mov       ecx,r14d
       lea       rcx,[rcx+rcx*2]
       lea       r12,[rsi+rcx*8+10]
       cmp       [r12+8],edi
       jne       near ptr M03_L44
       cmp       [r12+10],eax
       jne       near ptr M03_L44
M03_L01:
       jmp       short M03_L05
M03_L02:
       mov       r12d,[rsi+8]
       cmp       r12d,edx
       jbe       short M03_L04
       mov       edx,edx
       lea       rdx,[rdx+rdx*2]
       lea       rdx,[rsi+rdx*8+10]
       mov       rax,rdx
       cmp       [rax+8],r15d
       je        near ptr M03_L46
M03_L03:
       mov       edx,[rax+0C]
       inc       r13d
       cmp       r12d,r13d
       jae       short M03_L02
       jmp       near ptr M03_L81
M03_L04:
       xor       r12d,r12d
M03_L05:
       test      r12,r12
       jne       near ptr M03_L13
       xor       ecx,ecx
       mov       [rsp+80],rcx
M03_L06:
       mov       r8,[rbx+18]
       mov       rcx,offset Stateless.StateMachine`2+<>c__DisplayClass46_0[[Benchmark.HsmState, Benchmark],[Benchmark.HsmTrigger, Benchmark]].<.ctor>b__0()
       cmp       [r8+18],rcx
       jne       near ptr M03_L50
       mov       rcx,[r8+8]
       mov       rcx,[rcx+8]
       mov       r10d,[rcx+8]
M03_L07:
       mov       [rbp+20],r10d
       mov       edi,[rbp+20]
       mov       rsi,[rbx+8]
       mov       rcx,offset MT_System.Collections.Generic.Dictionary<Benchmark.HsmState, Stateless.StateMachine<Benchmark.HsmState, Benchmark.HsmTrigger>+StateRepresentation>
       cmp       [rsi],rcx
       jne       near ptr M03_L56
       mov       edx,edi
       cmp       qword ptr [rsi+8],0
       je        near ptr M03_L16
       mov       r13,[rsi+18]
       test      r13,r13
       jne       near ptr M03_L52
       mov       rcx,[rsi+8]
       mov       edx,edx
       imul      rdx,[rsi+30]
       shr       rdx,20
       inc       rdx
       mov       eax,[rcx+8]
       imul      rdx,rax
       shr       rdx,20
       cmp       edx,[rcx+8]
       jae       near ptr M03_L93
       mov       edx,edx
       lea       rcx,[rcx+rdx*4+10]
       mov       r14d,[rcx]
       mov       r15,[rsi+10]
       xor       r13d,r13d
       dec       r14d
       mov       esi,[r15+8]
M03_L08:
       cmp       esi,r14d
       jbe       near ptr M03_L16
       mov       ecx,r14d
       lea       rcx,[rcx+rcx*2]
       lea       r14,[r15+rcx*8+10]
       cmp       [r14+8],edi
       jne       near ptr M03_L51
       cmp       [r14+10],edi
       jne       near ptr M03_L51
M03_L09:
       test      r14,r14
       je        near ptr M03_L55
       mov       rcx,[r14]
       mov       [rsp+70],rcx
M03_L10:
       mov       rdx,[rsp+70]
       xor       ecx,ecx
       mov       [rsp+70],rcx
       lea       rcx,[rbp+18]
       call      CORINFO_HELP_ASSIGN_REF
       mov       r15,[rbp+18]
       mov       esi,[rbp+24]
       mov       r13,[rbp+8]
       cmp       [r15],r15b
       xor       ecx,ecx
       mov       [rsp+68],rcx
       mov       rcx,offset MT_Stateless.StateMachine<Benchmark.HsmState, Benchmark.HsmTrigger>+StateRepresentation+<>c__DisplayClass41_0
       call      CORINFO_HELP_NEWSFAST
       mov       rdi,rax
       lea       rcx,[rdi+8]
       mov       rdx,r13
       call      CORINFO_HELP_ASSIGN_REF
       mov       r14,[r15+8]
       mov       rcx,offset MT_System.Collections.Generic.Dictionary<Benchmark.HsmTrigger, System.Collections.Generic.ICollection<Stateless.StateMachine<Benchmark.HsmState, Benchmark.HsmTrigger>+TriggerBehaviour>>
       cmp       [r14],rcx
       jne       near ptr M03_L62
       mov       edx,esi
       cmp       qword ptr [r14+8],0
       je        near ptr M03_L19
       mov       r12,[r14+18]
       test      r12,r12
       jne       near ptr M03_L59
       mov       eax,edx
       mov       rcx,[r14+8]
       mov       r8d,edx
       imul      r8,[r14+30]
       shr       r8,20
       inc       r8
       mov       r10d,[rcx+8]
       imul      r8,r10
       shr       r8,20
       cmp       r8d,[rcx+8]
       jae       near ptr M03_L93
       mov       r8d,r8d
       lea       rcx,[rcx+r8*4+10]
       mov       r12d,[rcx]
       mov       r14,[r14+10]
       xor       r8d,r8d
       dec       r12d
       mov       r10d,[r14+8]
M03_L11:
       cmp       r10d,r12d
       jbe       near ptr M03_L19
       mov       ecx,r12d
       lea       rcx,[rcx+rcx*2]
       lea       r12,[r14+rcx*8+10]
       cmp       [r12+8],edx
       jne       near ptr M03_L58
       cmp       [r12+10],eax
       jne       near ptr M03_L58
M03_L12:
       jmp       near ptr M03_L20
M03_L13:
       mov       rcx,[r12]
       mov       [rsp+80],rcx
       jmp       near ptr M03_L49
M03_L14:
       mov       r12d,[rsi+8]
       cmp       r12d,edx
       jbe       short M03_L16
       mov       edx,edx
       lea       rdx,[rdx+rdx*2]
       lea       rdx,[rsi+rdx*8+10]
       mov       rax,rdx
       cmp       [rax+8],r14d
       je        near ptr M03_L53
M03_L15:
       mov       edx,[rax+0C]
       inc       r15d
       cmp       r12d,r15d
       jae       short M03_L14
       jmp       near ptr M03_L81
M03_L16:
       xor       r14d,r14d
       jmp       near ptr M03_L09
M03_L17:
       mov       r10d,[r14+8]
       cmp       r10d,edx
       jbe       short M03_L19
       mov       edx,edx
       lea       rdx,[rdx+rdx*2]
       lea       rdx,[r14+rdx*8+10]
       mov       r9,rdx
       mov       [rsp+5C],eax
       cmp       [r9+8],eax
       je        near ptr M03_L60
M03_L18:
       mov       edx,[r9+0C]
       inc       ecx
       mov       [rsp+58],ecx
       cmp       r10d,ecx
       mov       eax,[rsp+5C]
       mov       ecx,[rsp+58]
       jae       short M03_L17
       jmp       near ptr M03_L81
M03_L19:
       xor       r12d,r12d
M03_L20:
       test      r12,r12
       jne       short M03_L22
       xor       ecx,ecx
       mov       [rsp+60],rcx
M03_L21:
       xor       r14d,r14d
       jmp       near ptr M03_L67
M03_L22:
       mov       rcx,[r12]
       mov       [rsp+60],rcx
M03_L23:
       mov       rcx,offset MT_System.Func<Stateless.StateMachine<Benchmark.HsmState, Benchmark.HsmTrigger>+TriggerBehaviour, Stateless.StateMachine<Benchmark.HsmState, Benchmark.HsmTrigger>+TriggerBehaviourResult>
       call      CORINFO_HELP_NEWSFAST
       mov       r14,rax
       mov       r12,[rsp+60]
       lea       rcx,[r14+8]
       mov       rdx,rdi
       call      CORINFO_HELP_ASSIGN_REF
       mov       r8,offset Stateless.StateMachine`2+StateRepresentation+<>c__DisplayClass41_0[[Benchmark.HsmState, Benchmark],[Benchmark.HsmTrigger, Benchmark]].<TryFindLocalHandler>b__0(TriggerBehaviour<Benchmark.HsmState,Benchmark.HsmTrigger>)
       mov       [r14+18],r8
       mov       r8,r14
       mov       rdx,r12
       mov       rcx,7FFA3EB88DE0
       call      qword ptr [7FFA3EAA7540]; System.Linq.Enumerable.Select[[System.__Canon, System.Private.CoreLib],[System.__Canon, System.Private.CoreLib]](System.Collections.Generic.IEnumerable`1<System.__Canon>, System.Func`2<System.__Canon,System.__Canon>)
       mov       r14,rax
       mov       rdx,r14
       mov       rcx,offset MT_System.Linq.Enumerable+Iterator<Stateless.StateMachine<Benchmark.HsmState, Benchmark.HsmTrigger>+TriggerBehaviourResult>
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       test      rax,rax
       je        near ptr M03_L64
       mov       rcx,offset MT_System.Linq.Enumerable+ListSelectIterator<Stateless.StateMachine<Benchmark.HsmState, Benchmark.HsmTrigger>+TriggerBehaviour, Stateless.StateMachine<Benchmark.HsmState, Benchmark.HsmTrigger>+TriggerBehaviourResult>
       cmp       [rax],rcx
       jne       near ptr M03_L63
       mov       rcx,rax
       call      qword ptr [7FFA3EAF0C60]; System.Linq.Enumerable+ListSelectIterator`2[[System.__Canon, System.Private.CoreLib],[System.__Canon, System.Private.CoreLib]].ToArray()
       mov       r12,rax
M03_L24:
       mov       rcx,r15
       mov       edx,esi
       mov       r8,r12
       call      qword ptr [7FFA3EB45650]; Stateless.StateMachine`2+StateRepresentation[[Benchmark.HsmState, Benchmark],[Benchmark.HsmTrigger, Benchmark]].TryFindLocalHandlerResult(Benchmark.HsmTrigger, System.Collections.Generic.IEnumerable`1<TriggerBehaviourResult<Benchmark.HsmState,Benchmark.HsmTrigger>>)
       mov       r14,rax
       test      r14,r14
       je        near ptr M03_L66
M03_L25:
       test      r14,r14
       je        near ptr M03_L67
       mov       rdx,[r14+10]
       mov       rcx,7FFA3EB89030
       call      qword ptr [7FFA3EB45230]; System.Linq.Enumerable.Any[[System.__Canon, System.Private.CoreLib]](System.Collections.Generic.IEnumerable`1<System.__Canon>)
       test      eax,eax
       sete      al
       movzx     eax,al
M03_L26:
       xor       ecx,ecx
       mov       [rsp+60],rcx
       test      eax,eax
       jne       near ptr M03_L40
       mov       rcx,[r15+30]
       test      rcx,rcx
       je        near ptr M03_L68
       lea       r9,[rsp+68]
       mov       edx,esi
       mov       r8,r13
       cmp       [rcx],ecx
       call      qword ptr [7FFA3EB45470]; Stateless.StateMachine`2+StateRepresentation[[Benchmark.HsmState, Benchmark],[Benchmark.HsmTrigger, Benchmark]].TryFindHandler(Benchmark.HsmTrigger, System.Object[], TriggerBehaviourResult<Benchmark.HsmState,Benchmark.HsmTrigger> ByRef)
M03_L27:
       mov       rcx,[rsp+68]
       cmp       qword ptr [rsp+68],0
       cmove     rcx,r14
       movzx     eax,al
       xor       edx,edx
       mov       [rsp+68],rdx
       test      eax,eax
       je        near ptr M03_L90
       mov       r14,[rcx+8]
       mov       rax,r14
       test      rax,rax
       je        short M03_L28
       mov       rcx,offset MT_Stateless.StateMachine<Benchmark.HsmState, Benchmark.HsmTrigger>+InternalTriggerBehaviour+Sync
       cmp       [rax],rcx
       jne       near ptr M03_L69
       xor       eax,eax
M03_L28:
       test      rax,rax
       jne       near ptr M03_L39
       mov       r15,r14
       test      r15,r15
       je        short M03_L29
       mov       rcx,offset MT_Stateless.StateMachine<Benchmark.HsmState, Benchmark.HsmTrigger>+InternalTriggerBehaviour+Sync
       cmp       [r15],rcx
       jne       near ptr M03_L70
       xor       r15d,r15d
M03_L29:
       test      r15,r15
       jne       near ptr M03_L89
       mov       r15,r14
       test      r15,r15
       je        short M03_L30
       mov       rcx,offset MT_Stateless.StateMachine<Benchmark.HsmState, Benchmark.HsmTrigger>+InternalTriggerBehaviour+Sync
       cmp       [r15],rcx
       jne       near ptr M03_L71
       xor       r15d,r15d
M03_L30:
       test      r15,r15
       jne       near ptr M03_L88
       mov       rcx,r14
       test      rcx,rcx
       je        short M03_L31
       mov       rax,offset MT_Stateless.StateMachine<Benchmark.HsmState, Benchmark.HsmTrigger>+InternalTriggerBehaviour+Sync
       cmp       [rcx],rax
       jne       near ptr M03_L72
       xor       ecx,ecx
M03_L31:
       test      rcx,rcx
       jne       near ptr M03_L87
       mov       r15,r14
       test      r15,r15
       je        short M03_L32
       mov       rcx,offset MT_Stateless.StateMachine<Benchmark.HsmState, Benchmark.HsmTrigger>+InternalTriggerBehaviour+Sync
       cmp       [r15],rcx
       jne       near ptr M03_L73
       xor       r15d,r15d
M03_L32:
       test      r15,r15
       jne       near ptr M03_L86
       mov       rax,r14
       test      rax,rax
       je        short M03_L33
       mov       rcx,offset MT_Stateless.StateMachine<Benchmark.HsmState, Benchmark.HsmTrigger>+InternalTriggerBehaviour+Sync
       cmp       [rax],rcx
       jne       near ptr M03_L74
M03_L33:
       test      rax,rax
       je        near ptr M03_L85
       mov       rcx,offset MT_Stateless.StateMachine<Benchmark.HsmState, Benchmark.HsmTrigger>+Transition
       call      CORINFO_HELP_NEWSFAST
       mov       r15,rax
       mov       edx,[rbp+20]
       mov       ecx,edx
       mov       eax,[rbp+24]
       mov       r8,[rbp+8]
       mov       [r15+10],ecx
       mov       [r15+14],edx
       mov       [r15+18],eax
       test      r8,r8
       je        near ptr M03_L75
M03_L34:
       lea       rcx,[r15+8]
       mov       rdx,r8
       call      CORINFO_HELP_ASSIGN_REF
       mov       rax,[rbx+18]
       mov       r8,offset Stateless.StateMachine`2+<>c__DisplayClass46_0[[Benchmark.HsmState, Benchmark],[Benchmark.HsmTrigger, Benchmark]].<.ctor>b__0()
       cmp       [rax+18],r8
       jne       near ptr M03_L76
       mov       r8,[rax+8]
       mov       r8,[r8+8]
       mov       r14d,[r8+8]
M03_L35:
       mov       r13,[rbx+8]
       mov       r8,offset MT_System.Collections.Generic.Dictionary<Benchmark.HsmState, Stateless.StateMachine<Benchmark.HsmState, Benchmark.HsmTrigger>+StateRepresentation>
       cmp       [r13],r8
       jne       near ptr M03_L83
       mov       edx,r14d
       cmp       qword ptr [r13+8],0
       je        near ptr M03_L43
       mov       r12,[r13+18]
       test      r12,r12
       jne       near ptr M03_L78
       mov       r8,[r13+8]
       mov       ecx,edx
       imul      rcx,[r13+30]
       shr       rcx,20
       inc       rcx
       mov       edx,[r8+8]
       imul      rcx,rdx
       shr       rcx,20
       cmp       ecx,[r8+8]
       jae       near ptr M03_L93
       mov       ecx,ecx
       lea       r8,[r8+rcx*4+10]
       mov       esi,[r8]
       mov       rdi,[r13+10]
       xor       r12d,r12d
       dec       esi
       mov       r13d,[rdi+8]
M03_L36:
       cmp       r13d,esi
       jbe       near ptr M03_L43
       mov       r8d,esi
       lea       r8,[r8+r8*2]
       lea       rsi,[rdi+r8*8+10]
       cmp       [rsi+8],r14d
       jne       near ptr M03_L77
       cmp       [rsi+10],r14d
       jne       near ptr M03_L77
M03_L37:
       test      rsi,rsi
       je        near ptr M03_L82
       mov       r8,[rsi]
       mov       [rsp+50],r8
M03_L38:
       mov       rcx,[rsp+50]
       xor       r8d,r8d
       mov       [rsp+50],r8
       mov       r8,[rbp+8]
       mov       rdx,r15
       cmp       [rcx],ecx
       call      qword ptr [7FFA3EB45578]; Stateless.StateMachine`2+StateRepresentation[[Benchmark.HsmState, Benchmark],[Benchmark.HsmTrigger, Benchmark]].InternalAction(Transition<Benchmark.HsmState,Benchmark.HsmTrigger>, System.Object[])
M03_L39:
       nop
       add       rsp,88
       pop       rbx
       pop       rbp
       pop       rsi
       pop       rdi
       pop       r12
       pop       r13
       pop       r14
       pop       r15
       ret
M03_L40:
       mov       eax,1
       jmp       near ptr M03_L27
M03_L41:
       mov       eax,[r13+8]
       cmp       eax,edx
       jbe       short M03_L43
       mov       edx,edx
       lea       rdx,[rdx+rdx*2]
       lea       rdx,[r13+rdx*8+10]
       mov       r10,rdx
       cmp       [r10+8],esi
       je        near ptr M03_L79
M03_L42:
       mov       edx,[r10+0C]
       inc       edi
       cmp       eax,edi
       jae       short M03_L41
       jmp       near ptr M03_L81
M03_L43:
       xor       esi,esi
       jmp       short M03_L37
M03_L44:
       mov       r14d,[r12+0C]
       inc       r15d
       cmp       r13d,r15d
       jae       near ptr M03_L00
       jmp       near ptr M03_L81
M03_L45:
       mov       rcx,r14
       mov       edx,edi
       mov       r11,7FFA3E6E06C8
       call      qword ptr [r11]
       mov       r15d,eax
       mov       rdx,[rsi+8]
       mov       ecx,r15d
       imul      rcx,[rsi+30]
       shr       rcx,20
       inc       rcx
       mov       r8d,[rdx+8]
       imul      rcx,r8
       shr       rcx,20
       cmp       ecx,[rdx+8]
       jae       near ptr M03_L93
       mov       ecx,ecx
       lea       rdx,[rdx+rcx*4+10]
       mov       edx,[rdx]
       mov       rsi,[rsi+10]
       xor       r13d,r13d
       dec       edx
       jmp       near ptr M03_L02
M03_L46:
       mov       [rsp+40],rax
       mov       edx,[rax+10]
       mov       rcx,r14
       mov       r8d,edi
       mov       r11,7FFA3E6E06D0
       call      qword ptr [r11]
       test      eax,eax
       jne       short M03_L47
       mov       rax,[rsp+40]
       jmp       near ptr M03_L03
M03_L47:
       mov       r12,[rsp+40]
       jmp       near ptr M03_L01
M03_L48:
       lea       r8,[rsp+80]
       mov       rcx,rsi
       mov       edx,edi
       mov       r11,7FFA3E6E06C0
       call      qword ptr [r11]
       test      eax,eax
       je        near ptr M03_L06
M03_L49:
       mov       rdx,[rbp+8]
       mov       rcx,[rsp+80]
       cmp       [rcx],ecx
       call      qword ptr [7FFA3EB45458]
       jmp       near ptr M03_L06
M03_L50:
       mov       rcx,[r8+8]
       call      qword ptr [r8+18]
       mov       r10d,eax
       jmp       near ptr M03_L07
M03_L51:
       mov       r14d,[r14+0C]
       inc       r13d
       cmp       esi,r13d
       jae       near ptr M03_L08
       jmp       near ptr M03_L81
M03_L52:
       mov       rcx,r13
       mov       r11,7FFA3E6E06E8
       call      qword ptr [r11]
       mov       r14d,eax
       mov       rdx,[rsi+8]
       mov       ecx,r14d
       imul      rcx,[rsi+30]
       shr       rcx,20
       inc       rcx
       mov       r8d,[rdx+8]
       imul      rcx,r8
       shr       rcx,20
       cmp       ecx,[rdx+8]
       jae       near ptr M03_L93
       mov       ecx,ecx
       lea       rdx,[rdx+rcx*4+10]
       mov       edx,[rdx]
       mov       rsi,[rsi+10]
       xor       r15d,r15d
       dec       edx
       jmp       near ptr M03_L14
M03_L53:
       mov       [rsp+38],rax
       mov       edx,[rax+10]
       mov       rcx,r13
       mov       r8d,edi
       mov       r11,7FFA3E6E06F0
       call      qword ptr [r11]
       test      eax,eax
       jne       short M03_L54
       mov       rax,[rsp+38]
       jmp       near ptr M03_L15
M03_L54:
       mov       r14,[rsp+38]
       jmp       near ptr M03_L09
M03_L55:
       xor       r8d,r8d
       mov       [rsp+70],r8
       jmp       short M03_L57
M03_L56:
       lea       r8,[rsp+70]
       mov       rcx,rsi
       mov       edx,edi
       mov       r11,7FFA3E6E06D8
       call      qword ptr [r11]
       test      eax,eax
       jne       near ptr M03_L10
M03_L57:
       mov       rcx,offset MT_Stateless.StateMachine<Benchmark.HsmState, Benchmark.HsmTrigger>+StateRepresentation
       call      CORINFO_HELP_NEWSFAST
       mov       rsi,rax
       movzx     r8d,byte ptr [rbx+51]
       mov       rcx,rsi
       mov       edx,edi
       call      qword ptr [7FFA3EB44570]; Stateless.StateMachine`2+StateRepresentation[[Benchmark.HsmState, Benchmark],[Benchmark.HsmTrigger, Benchmark]]..ctor(Benchmark.HsmState, Boolean)
       mov       [rsp+70],rsi
       mov       rcx,[rbx+8]
       mov       r8,[rsp+70]
       mov       edx,edi
       mov       r11,7FFA3E6E06E0
       call      qword ptr [r11]
       jmp       near ptr M03_L10
M03_L58:
       mov       r12d,[r12+0C]
       inc       r8d
       cmp       r10d,r8d
       jae       near ptr M03_L11
       jmp       near ptr M03_L81
M03_L59:
       mov       rcx,r12
       mov       r11,7FFA3E6E0700
       call      qword ptr [r11]
       mov       rdx,[r14+8]
       mov       ecx,eax
       imul      rcx,[r14+30]
       shr       rcx,20
       inc       rcx
       mov       r8d,[rdx+8]
       imul      rcx,r8
       shr       rcx,20
       cmp       ecx,[rdx+8]
       jae       near ptr M03_L93
       mov       ecx,ecx
       lea       rdx,[rdx+rcx*4+10]
       mov       edx,[rdx]
       mov       r14,[r14+10]
       xor       ecx,ecx
       dec       edx
       jmp       near ptr M03_L17
M03_L60:
       mov       [rsp+58],ecx
       mov       [rsp+48],r10d
       mov       [rsp+30],r9
       mov       edx,[r9+10]
       mov       rcx,r12
       mov       r8d,esi
       mov       r11,7FFA3E6E0708
       call      qword ptr [r11]
       test      eax,eax
       mov       ecx,[rsp+58]
       mov       r10d,[rsp+48]
       jne       short M03_L61
       mov       r9,[rsp+30]
       jmp       near ptr M03_L18
M03_L61:
       mov       r12,[rsp+30]
       jmp       near ptr M03_L12
M03_L62:
       lea       r8,[rsp+60]
       mov       rcx,r14
       mov       edx,esi
       mov       r11,7FFA3E6E06F8
       call      qword ptr [r11]
       test      eax,eax
       jne       near ptr M03_L23
       jmp       near ptr M03_L21
M03_L63:
       mov       rcx,rax
       mov       rax,[rax]
       mov       rax,[rax+40]
       call      qword ptr [rax+20]
       mov       r12,rax
       jmp       near ptr M03_L24
M03_L64:
       mov       rdx,r14
       mov       rcx,offset MT_System.Collections.Generic.ICollection<Stateless.StateMachine<Benchmark.HsmState, Benchmark.HsmTrigger>+TriggerBehaviourResult>
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfInterface(Void*, System.Object)
       test      rax,rax
       je        short M03_L65
       mov       rdx,rax
       mov       rcx,7FFA3EC97798
       call      qword ptr [7FFA3EAAD4A0]; System.Linq.Enumerable.ICollectionToArray[[System.__Canon, System.Private.CoreLib]](System.Collections.Generic.ICollection`1<System.__Canon>)
       mov       r12,rax
       jmp       near ptr M03_L24
M03_L65:
       mov       rdx,r14
       mov       rcx,7FFA3EC97820
       call      qword ptr [7FFA3EC3CAC8]
       mov       r12,rax
       jmp       near ptr M03_L24
M03_L66:
       mov       rcx,r12
       call      qword ptr [7FFA3EB45668]
       mov       r14,rax
       jmp       near ptr M03_L25
M03_L67:
       xor       eax,eax
       jmp       near ptr M03_L26
M03_L68:
       xor       eax,eax
       jmp       near ptr M03_L27
M03_L69:
       mov       rdx,r14
       mov       rcx,offset MT_Stateless.StateMachine<Benchmark.HsmState, Benchmark.HsmTrigger>+IgnoredTriggerBehaviour
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       jmp       near ptr M03_L28
M03_L70:
       mov       rdx,r14
       mov       rcx,offset MT_Stateless.StateMachine<Benchmark.HsmState, Benchmark.HsmTrigger>+ReentryTriggerBehaviour
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       r15,rax
       jmp       near ptr M03_L29
M03_L71:
       mov       rdx,r14
       mov       rcx,offset MT_Stateless.StateMachine<Benchmark.HsmState, Benchmark.HsmTrigger>+DynamicTriggerBehaviourAsync
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       r15,rax
       jmp       near ptr M03_L30
M03_L72:
       mov       rdx,r14
       mov       rcx,offset MT_Stateless.StateMachine<Benchmark.HsmState, Benchmark.HsmTrigger>+DynamicTriggerBehaviour
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rcx,rax
       jmp       near ptr M03_L31
M03_L73:
       mov       rdx,r14
       mov       rcx,offset MT_Stateless.StateMachine<Benchmark.HsmState, Benchmark.HsmTrigger>+TransitioningTriggerBehaviour
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       r15,rax
       jmp       near ptr M03_L32
M03_L74:
       mov       rdx,r14
       mov       rcx,offset MT_Stateless.StateMachine<Benchmark.HsmState, Benchmark.HsmTrigger>+InternalTriggerBehaviour
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       jmp       near ptr M03_L33
M03_L75:
       mov       rcx,offset MT_System.Object[]
       xor       edx,edx
       call      CORINFO_HELP_NEWARR_1_OBJ
       mov       r8,rax
       jmp       near ptr M03_L34
M03_L76:
       mov       rcx,[rax+8]
       call      qword ptr [rax+18]
       mov       r14d,eax
       jmp       near ptr M03_L35
M03_L77:
       mov       esi,[rsi+0C]
       inc       r12d
       cmp       r13d,r12d
       jae       near ptr M03_L36
       jmp       near ptr M03_L81
M03_L78:
       mov       rcx,r12
       mov       r11,7FFA3E6E0720
       call      qword ptr [r11]
       mov       esi,eax
       mov       rdx,[r13+8]
       mov       ecx,esi
       imul      rcx,[r13+30]
       shr       rcx,20
       inc       rcx
       mov       r8d,[rdx+8]
       imul      rcx,r8
       shr       rcx,20
       cmp       ecx,[rdx+8]
       jae       near ptr M03_L93
       mov       ecx,ecx
       lea       rdx,[rdx+rcx*4+10]
       mov       edx,[rdx]
       mov       r13,[r13+10]
       xor       edi,edi
       dec       edx
       jmp       near ptr M03_L41
M03_L79:
       mov       [rsp+4C],eax
       mov       [rsp+28],r10
       mov       edx,[r10+10]
       mov       rcx,r12
       mov       r8d,r14d
       mov       r11,7FFA3E6E0728
       call      qword ptr [r11]
       test      eax,eax
       mov       eax,[rsp+4C]
       jne       short M03_L80
       mov       r10,[rsp+28]
       jmp       near ptr M03_L42
M03_L80:
       mov       rsi,[rsp+28]
       jmp       near ptr M03_L37
M03_L81:
       call      qword ptr [7FFA3E78F2A0]
       int       3
M03_L82:
       xor       r8d,r8d
       mov       [rsp+50],r8
       jmp       short M03_L84
M03_L83:
       lea       r8,[rsp+50]
       mov       rcx,r13
       mov       edx,r14d
       mov       r11,7FFA3E6E0710
       call      qword ptr [r11]
       test      eax,eax
       jne       near ptr M03_L38
M03_L84:
       mov       rcx,offset MT_Stateless.StateMachine<Benchmark.HsmState, Benchmark.HsmTrigger>+StateRepresentation
       call      CORINFO_HELP_NEWSFAST
       mov       rsi,rax
       movzx     r8d,byte ptr [rbx+51]
       mov       rcx,rsi
       mov       edx,r14d
       call      qword ptr [7FFA3EB44570]; Stateless.StateMachine`2+StateRepresentation[[Benchmark.HsmState, Benchmark],[Benchmark.HsmTrigger, Benchmark]]..ctor(Benchmark.HsmState, Boolean)
       mov       [rsp+50],rsi
       mov       rcx,[rbx+8]
       mov       r8,[rsp+50]
       mov       edx,r14d
       mov       r11,7FFA3E6E0718
       call      qword ptr [r11]
       jmp       near ptr M03_L38
M03_L85:
       mov       rcx,offset MT_System.InvalidOperationException
       call      CORINFO_HELP_NEWSFAST
       mov       rbp,rax
       mov       ecx,0ED
       mov       rdx,7FFA3EB26780
       call      CORINFO_HELP_STRCNS
       mov       rdx,rax
       mov       rcx,rbp
       call      qword ptr [7FFA3EAA6C70]
       mov       rcx,rbp
       call      CORINFO_HELP_THROW
       int       3
M03_L86:
       lea       rsi,[rbp+20]
       mov       rcx,offset MT_Benchmark.HsmState
       call      CORINFO_HELP_NEWSFAST
       mov       rdi,rax
       mov       ecx,[r15+14]
       mov       [rdi+8],ecx
       mov       rcx,offset MT_Benchmark.HsmState
       call      CORINFO_HELP_NEWSFAST
       mov       ecx,[rsi]
       mov       [rax+8],ecx
       mov       rcx,rax
       mov       rdx,rdi
       call      qword ptr [7FFA3E6D6098]; Precode of System.Enum.Equals(System.Object)
       test      eax,eax
       jne       near ptr M03_L39
       mov       esi,[rbp+20]
       mov       rcx,offset MT_Stateless.StateMachine<Benchmark.HsmState, Benchmark.HsmTrigger>+Transition
       call      CORINFO_HELP_NEWSFAST
       mov       rdi,rax
       mov       r9,[rbp+8]
       mov       [rsp+20],r9
       mov       r9d,[rbp+24]
       mov       r8d,[r15+14]
       mov       rcx,rdi
       mov       edx,esi
       call      qword ptr [7FFA3EB454D0]; Stateless.StateMachine`2+Transition[[Benchmark.HsmState, Benchmark],[Benchmark.HsmTrigger, Benchmark]]..ctor(Benchmark.HsmState, Benchmark.HsmState, Benchmark.HsmTrigger, System.Object[])
       mov       r8,[rbp+18]
       mov       rdx,[rbp+8]
       mov       rcx,rbx
       mov       r9,rdi
       call      qword ptr [7FFA3EB45530]; Stateless.StateMachine`2[[Benchmark.HsmState, Benchmark],[Benchmark.HsmTrigger, Benchmark]].HandleTransitioningTrigger(System.Object[], StateRepresentation<Benchmark.HsmState,Benchmark.HsmTrigger>, Transition<Benchmark.HsmState,Benchmark.HsmTrigger>)
       jmp       near ptr M03_L39
M03_L87:
       mov       edx,[rbp+20]
       mov       r8,[rbp+8]
       lea       r9,[rsp+78]
       call      qword ptr [7FFA3EB45518]
       mov       rcx,offset MT_Stateless.StateMachine<Benchmark.HsmState, Benchmark.HsmTrigger>+Transition
       call      CORINFO_HELP_NEWSFAST
       mov       rsi,rax
       mov       r9,[rbp+8]
       mov       [rsp+20],r9
       mov       r9d,[rbp+24]
       mov       edx,[rbp+20]
       mov       rcx,rsi
       mov       r8d,[rsp+78]
       call      qword ptr [7FFA3EB454D0]; Stateless.StateMachine`2+Transition[[Benchmark.HsmState, Benchmark],[Benchmark.HsmTrigger, Benchmark]]..ctor(Benchmark.HsmState, Benchmark.HsmState, Benchmark.HsmTrigger, System.Object[])
       mov       r8,[rbp+18]
       mov       rdx,[rbp+8]
       mov       rcx,rbx
       mov       r9,rsi
       call      qword ptr [7FFA3EB45530]; Stateless.StateMachine`2[[Benchmark.HsmState, Benchmark],[Benchmark.HsmTrigger, Benchmark]].HandleTransitioningTrigger(System.Object[], StateRepresentation<Benchmark.HsmState,Benchmark.HsmTrigger>, Transition<Benchmark.HsmState,Benchmark.HsmTrigger>)
       jmp       near ptr M03_L39
M03_L88:
       mov       rcx,offset MT_System.Func<System.Threading.Tasks.Task<Benchmark.HsmState>, System.Threading.Tasks.Task>
       call      CORINFO_HELP_NEWSFAST
       mov       rbx,rax
       mov       r8,[rbp+8]
       mov       edx,[rbp+20]
       mov       rcx,r15
       call      qword ptr [7FFA3EB45500]
       mov       rsi,rax
       mov       rcx,rbx
       mov       rdx,rbp
       mov       r8,7FFA3EB41428
       call      qword ptr [7FFA3E7869D0]; System.MulticastDelegate.CtorClosed(System.Object, IntPtr)
       mov       rcx,rsi
       mov       r8,rbx
       mov       rdx,7FFA3EB87550
       cmp       [rcx],ecx
       call      qword ptr [7FFA3EB45398]
       jmp       near ptr M03_L39
M03_L89:
       mov       edx,[rbp+20]
       mov       esi,edx
       mov       rcx,offset MT_Stateless.StateMachine<Benchmark.HsmState, Benchmark.HsmTrigger>+Transition
       call      CORINFO_HELP_NEWSFAST
       mov       rdi,rax
       mov       r9,[rbp+8]
       mov       [rsp+20],r9
       mov       r9d,[rbp+24]
       mov       r8d,[r15+14]
       mov       rcx,rdi
       mov       edx,esi
       call      qword ptr [7FFA3EB454D0]; Stateless.StateMachine`2+Transition[[Benchmark.HsmState, Benchmark],[Benchmark.HsmTrigger, Benchmark]]..ctor(Benchmark.HsmState, Benchmark.HsmState, Benchmark.HsmTrigger, System.Object[])
       mov       r8,[rbp+18]
       mov       rdx,[rbp+8]
       mov       rcx,rbx
       mov       r9,rdi
       call      qword ptr [7FFA3EB454E8]
       jmp       near ptr M03_L39
M03_L90:
       mov       rax,[rbx+28]
       mov       rdx,[rbp+18]
       mov       edx,[rdx+40]
       mov       r8d,[rbp+24]
       test      rcx,rcx
       jne       short M03_L91
       xor       r9d,r9d
       jmp       short M03_L92
M03_L91:
       mov       r9,[rcx+10]
M03_L92:
       mov       rcx,rax
       mov       rax,[rax]
       mov       rax,[rax+40]
       call      qword ptr [rax+20]
       jmp       near ptr M03_L39
M03_L93:
       call      CORINFO_HELP_RNGCHKFAIL
       int       3
; Total bytes of code 3640
```
```assembly
; System.Runtime.CompilerServices.CastHelpers.StelemRef(System.Object[], IntPtr, System.Object)
       sub       rsp,28
       mov       eax,[rcx+8]
       cmp       rax,rdx
       jbe       short M04_L03
       lea       rax,[rcx+rdx*8+10]
       mov       rdx,[rcx]
       mov       rdx,[rdx+30]
       test      r8,r8
       je        short M04_L02
       cmp       rdx,[r8]
       jne       short M04_L01
M04_L00:
       mov       rcx,rax
       mov       rdx,r8
       add       rsp,28
       jmp       near ptr System.Runtime.CompilerServices.CastHelpers.WriteBarrier(System.Object ByRef, System.Object)
M04_L01:
       mov       r10,offset MT_System.Object[]
       cmp       [rcx],r10
       je        short M04_L00
       mov       rcx,rax
       add       rsp,28
       jmp       qword ptr [7FFA3EAA4420]; System.Runtime.CompilerServices.CastHelpers.StelemRef_Helper(System.Object ByRef, Void*, System.Object)
M04_L02:
       xor       ecx,ecx
       mov       [rax],rcx
       add       rsp,28
       ret
M04_L03:
       call      qword ptr [7FFA3EC3CAB0]
       int       3
; Total bytes of code 94
```
```assembly
; System.Linq.Enumerable.Any[[System.__Canon, System.Private.CoreLib]](System.Collections.Generic.IEnumerable`1<System.__Canon>)
       push      rbp
       push      r14
       push      rdi
       push      rsi
       push      rbx
       sub       rsp,40
       lea       rbp,[rsp+60]
       mov       [rbp-40],rsp
       mov       [rbp-28],rcx
       mov       rbx,rcx
       mov       rsi,rdx
       test      rsi,rsi
       je        near ptr M05_L19
       mov       rcx,[rbx+18]
       mov       rcx,[rcx+28]
       test      rcx,rcx
       je        short M05_L03
M05_L00:
       mov       rdx,rsi
       call      qword ptr [7FFA3E78F7F8]; System.Runtime.CompilerServices.CastHelpers.IsInstanceOfInterface(Void*, System.Object)
       mov       rdi,rax
       test      rdi,rdi
       je        short M05_L05
       mov       rcx,[rbx+18]
       cmp       qword ptr [rcx+8],40
       jle       short M05_L04
       mov       r11,[rcx+40]
       test      r11,r11
       je        short M05_L04
M05_L01:
       mov       rcx,rdi
       call      qword ptr [r11]
       test      eax,eax
       setne     dil
       movzx     edi,dil
M05_L02:
       movzx     eax,dil
       add       rsp,40
       pop       rbx
       pop       rsi
       pop       rdi
       pop       r14
       pop       rbp
       ret
M05_L03:
       mov       rcx,rbx
       mov       rdx,7FFA3EC1C6C0
       call      CORINFO_HELP_RUNTIMEHANDLE_METHOD
       mov       rcx,rax
       jmp       short M05_L00
M05_L04:
       mov       rcx,rbx
       mov       rdx,7FFA3EC1C780
       call      CORINFO_HELP_RUNTIMEHANDLE_METHOD
       mov       r11,rax
       jmp       short M05_L01
M05_L05:
       mov       rcx,[rbx+18]
       cmp       qword ptr [rcx+8],30
       jle       short M05_L09
       mov       rcx,[rcx+30]
       test      rcx,rcx
       je        short M05_L09
M05_L06:
       mov       rdx,rsi
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       r14,rax
       test      r14,r14
       jne       near ptr M05_L17
       mov       rcx,rsi
       mov       rdx,offset MT_System.Collections.Generic.Queue<Stateless.StateMachine<Benchmark.HsmState, Benchmark.HsmTrigger>+QueuedTrigger>
       cmp       [rcx],rdx
       jne       short M05_L11
M05_L07:
       test      rcx,rcx
       je        short M05_L13
       mov       rdx,offset MT_System.Collections.Generic.Queue<Stateless.StateMachine<Benchmark.HsmState, Benchmark.HsmTrigger>+QueuedTrigger>
       cmp       [rcx],rdx
       jne       short M05_L12
       mov       r14d,[rcx+18]
M05_L08:
       test      r14d,r14d
       setne     dil
       movzx     edi,dil
       jmp       near ptr M05_L02
M05_L09:
       mov       rcx,rbx
       mov       rdx,7FFA3EC1C758
       call      CORINFO_HELP_RUNTIMEHANDLE_METHOD
       mov       rcx,rax
       jmp       short M05_L06
M05_L10:
       mov       rcx,[rbp-38]
       mov       r11,7FFA3E6E05E8
       call      qword ptr [r11]
       mov       edi,eax
       jmp       short M05_L16
M05_L11:
       mov       rdx,rsi
       mov       rcx,offset MT_System.Collections.ICollection
       call      qword ptr [7FFA3E78F7F8]; System.Runtime.CompilerServices.CastHelpers.IsInstanceOfInterface(Void*, System.Object)
       mov       rcx,rax
       jmp       short M05_L07
M05_L12:
       mov       r11,7FFA3E6E05F8
       call      qword ptr [r11]
       mov       r14d,eax
       jmp       short M05_L08
M05_L13:
       mov       rcx,[rbx+18]
       cmp       qword ptr [rcx+8],38
       jle       short M05_L14
       mov       r11,[rcx+38]
       test      r11,r11
       je        short M05_L14
       jmp       short M05_L15
M05_L14:
       mov       rcx,rbx
       mov       rdx,7FFA3EC1C768
       call      CORINFO_HELP_RUNTIMEHANDLE_METHOD
       mov       r11,rax
M05_L15:
       mov       rcx,rsi
       call      qword ptr [r11]
       mov       [rbp-38],rax
       jmp       short M05_L10
M05_L16:
       mov       rcx,[rbp-38]
       mov       r11,7FFA3E6E05F0
       call      qword ptr [r11]
       jmp       near ptr M05_L02
M05_L17:
       mov       rcx,r14
       mov       edx,1
       mov       rax,[r14]
       mov       rax,[rax+40]
       call      qword ptr [rax+30]
       test      eax,eax
       jl        short M05_L18
       test      eax,eax
       setne     dil
       movzx     edi,dil
       jmp       near ptr M05_L02
M05_L18:
       lea       rdx,[rbp-30]
       mov       rcx,r14
       mov       rax,[r14]
       mov       rax,[rax+48]
       call      qword ptr [rax+10]
       movzx     edi,byte ptr [rbp-30]
       jmp       near ptr M05_L02
M05_L19:
       mov       ecx,11
       call      qword ptr [7FFA3E78F738]
       int       3
       push      rbp
       push      r14
       push      rdi
       push      rsi
       push      rbx
       sub       rsp,30
       mov       rbp,[rcx+20]
       mov       [rsp+20],rbp
       lea       rbp,[rbp+60]
       cmp       qword ptr [rbp-38],0
       je        short M05_L20
       mov       rcx,[rbp-38]
       mov       r11,7FFA3E6E05F0
       call      qword ptr [r11]
M05_L20:
       nop
       add       rsp,30
       pop       rbx
       pop       rsi
       pop       rdi
       pop       r14
       pop       rbp
       ret
; Total bytes of code 561
```
```assembly
; System.Collections.Generic.Queue`1[[System.__Canon, System.Private.CoreLib]].Grow(Int32)
       mov       rax,[rcx+8]
       mov       r8d,[rax+8]
       add       r8d,r8d
       mov       r10d,7FFFFFC7
       cmp       r8d,7FFFFFC7
       cmova     r8d,r10d
       mov       eax,[rax+8]
       add       eax,4
       cmp       r8d,eax
       cmovl     r8d,eax
       cmp       r8d,edx
       cmovl     r8d,edx
       mov       edx,r8d
       lea       rax,[System.Reflection.CustomAttributeExtensions.GetCustomAttribute[[System.__Canon, System.Private.CoreLib]](System.Reflection.Assembly)]
       jmp       qword ptr [rax]
; Total bytes of code 61
```
```assembly
; Stateless.StateMachine`2+<>c__DisplayClass46_0[[Benchmark.HsmState, Benchmark],[Benchmark.HsmTrigger, Benchmark]].<.ctor>b__0()
       mov       rax,[rcx+8]
       mov       eax,[rax+8]
       ret
; Total bytes of code 8
```
```assembly
; Stateless.StateMachine`2+StateRepresentation+<>c__DisplayClass41_0[[Benchmark.HsmState, Benchmark],[Benchmark.HsmTrigger, Benchmark]].<TryFindLocalHandler>b__0(TriggerBehaviour<Benchmark.HsmState,Benchmark.HsmTrigger>)
       push      rdi
       push      rsi
       push      rbx
       sub       rsp,20
       mov       rbx,rdx
       mov       rsi,[rcx+8]
       mov       rcx,offset MT_Stateless.StateMachine<Benchmark.HsmState, Benchmark.HsmTrigger>+TriggerBehaviourResult
       call      CORINFO_HELP_NEWSFAST
       mov       rdi,rax
       mov       rcx,[rbx+8]
       mov       rdx,rsi
       cmp       [rcx],ecx
       call      qword ptr [7FFA3EB45830]; Stateless.StateMachine`2+TransitionGuard[[Benchmark.HsmState, Benchmark],[Benchmark.HsmTrigger, Benchmark]].UnmetGuardConditions(System.Object[])
       mov       rsi,rax
       lea       rcx,[rdi+8]
       mov       rdx,rbx
       call      CORINFO_HELP_ASSIGN_REF
       lea       rcx,[rdi+10]
       mov       rdx,rsi
       call      CORINFO_HELP_ASSIGN_REF
       mov       rax,rdi
       add       rsp,20
       pop       rbx
       pop       rsi
       pop       rdi
       ret
; Total bytes of code 85
```
```assembly
; System.Linq.Enumerable.Select[[System.__Canon, System.Private.CoreLib],[System.__Canon, System.Private.CoreLib]](System.Collections.Generic.IEnumerable`1<System.__Canon>, System.Func`2<System.__Canon,System.__Canon>)
       push      r15
       push      r14
       push      r13
       push      r12
       push      rdi
       push      rsi
       push      rbp
       push      rbx
       sub       rsp,28
       mov       [rsp+20],rcx
       mov       rbx,rcx
       mov       rsi,rdx
       mov       rdi,r8
       test      rsi,rsi
       je        near ptr M09_L27
       test      rdi,rdi
       je        near ptr M09_L26
       mov       rcx,[rbx+18]
       cmp       qword ptr [rcx+10],50
       jle       near ptr M09_L06
       mov       rbp,[rcx+50]
       test      rbp,rbp
       je        near ptr M09_L06
M09_L00:
       mov       rcx,rbp
       mov       rdx,rsi
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       r14,rax
       test      r14,r14
       jne       near ptr M09_L11
       mov       rcx,[rbx+18]
       cmp       qword ptr [rcx+10],58
       jle       near ptr M09_L07
       mov       rcx,[rcx+58]
       test      rcx,rcx
       je        near ptr M09_L07
M09_L01:
       mov       rdx,rsi
       call      qword ptr [7FFA3E78F7F8]; System.Runtime.CompilerServices.CastHelpers.IsInstanceOfInterface(Void*, System.Object)
       mov       r15,rax
       test      r15,r15
       je        near ptr M09_L23
       mov       rcx,[rbx+18]
       cmp       qword ptr [rcx+10],68
       jle       near ptr M09_L08
       mov       rcx,[rcx+68]
       test      rcx,rcx
       je        near ptr M09_L08
M09_L02:
       mov       rdx,rsi
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfAny(Void*, System.Object)
       mov       r13,rax
       test      r13,r13
       jne       near ptr M09_L17
       mov       rcx,[rbx+18]
       cmp       qword ptr [rcx+10],70
       jle       near ptr M09_L09
       mov       rcx,[rcx+70]
       test      rcx,rcx
       je        near ptr M09_L09
M09_L03:
       mov       rdx,rsi
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rbp,rax
       test      rbp,rbp
       je        near ptr M09_L14
       mov       rcx,[rbx+18]
       cmp       qword ptr [rcx+10],80
       jle       near ptr M09_L10
       mov       rcx,[rcx+80]
       test      rcx,rcx
       je        near ptr M09_L10
M09_L04:
       call      CORINFO_HELP_NEWSFAST
       mov       r12,rax
       call      CORINFO_HELP_GETCURRENTMANAGEDTHREADID
       mov       [r12+10],eax
       lea       rcx,[r12+18]
       mov       rdx,rbp
       call      CORINFO_HELP_ASSIGN_REF
       lea       rcx,[r12+20]
       mov       rdx,rdi
       call      CORINFO_HELP_ASSIGN_REF
       mov       rax,r12
M09_L05:
       add       rsp,28
       pop       rbx
       pop       rbp
       pop       rsi
       pop       rdi
       pop       r12
       pop       r13
       pop       r14
       pop       r15
       ret
M09_L06:
       mov       rcx,rbx
       mov       rdx,7FFA3EC1CDD8
       call      CORINFO_HELP_RUNTIMEHANDLE_METHOD
       mov       rbp,rax
       jmp       near ptr M09_L00
M09_L07:
       mov       rcx,rbx
       mov       rdx,7FFA3EC1D0A8
       call      CORINFO_HELP_RUNTIMEHANDLE_METHOD
       mov       rcx,rax
       jmp       near ptr M09_L01
M09_L08:
       mov       rcx,rbx
       mov       rdx,7FFA3EC1D7E8
       call      CORINFO_HELP_RUNTIMEHANDLE_METHOD
       mov       rcx,rax
       jmp       near ptr M09_L02
M09_L09:
       mov       rcx,rbx
       mov       rdx,7FFA3EC1D808
       call      CORINFO_HELP_RUNTIMEHANDLE_METHOD
       mov       rcx,rax
       jmp       near ptr M09_L03
M09_L10:
       mov       rcx,rbx
       mov       rdx,7FFA3EC1DF90
       call      CORINFO_HELP_RUNTIMEHANDLE_METHOD
       mov       rcx,rax
       jmp       near ptr M09_L04
M09_L11:
       mov       rcx,[rbx+18]
       cmp       qword ptr [rcx+10],98
       jle       short M09_L13
       mov       r8,[rcx+98]
       test      r8,r8
       je        short M09_L13
M09_L12:
       mov       rdx,rbp
       mov       rcx,r14
       call      CORINFO_HELP_VIRTUAL_FUNC_PTR
       mov       rcx,r14
       mov       rdx,rdi
       add       rsp,28
       pop       rbx
       pop       rbp
       pop       rsi
       pop       rdi
       pop       r12
       pop       r13
       pop       r14
       pop       r15
       jmp       rax
M09_L13:
       mov       rcx,rbx
       mov       rdx,7FFA3EC1E6F8
       call      CORINFO_HELP_RUNTIMEHANDLE_METHOD
       mov       r8,rax
       jmp       short M09_L12
M09_L14:
       mov       rcx,[rbx+18]
       cmp       qword ptr [rcx+10],78
       jle       short M09_L15
       mov       rcx,[rcx+78]
       test      rcx,rcx
       je        short M09_L15
       jmp       short M09_L16
M09_L15:
       mov       rcx,rbx
       mov       rdx,7FFA3EC1DE90
       call      CORINFO_HELP_RUNTIMEHANDLE_METHOD
       mov       rcx,rax
M09_L16:
       call      CORINFO_HELP_NEWSFAST
       mov       r13,rax
       mov       rcx,r13
       mov       rdx,r15
       mov       r8,rdi
       call      qword ptr [7FFA3EC3C4B0]
       mov       rax,r13
       jmp       near ptr M09_L05
M09_L17:
       cmp       dword ptr [r13+8],0
       jne       short M09_L20
       mov       rcx,[rbx+18]
       cmp       qword ptr [rcx+10],90
       jle       short M09_L18
       mov       rcx,[rcx+90]
       test      rcx,rcx
       je        short M09_L18
       jmp       short M09_L19
M09_L18:
       mov       rcx,rbx
       mov       rdx,7FFA3EC1E4A0
       call      CORINFO_HELP_RUNTIMEHANDLE_METHOD
       mov       rcx,rax
M09_L19:
       add       rsp,28
       pop       rbx
       pop       rbp
       pop       rsi
       pop       rdi
       pop       r12
       pop       r13
       pop       r14
       pop       r15
       jmp       qword ptr [7FFA3EAA72D0]; System.Array.Empty[[System.__Canon, System.Private.CoreLib]]()
M09_L20:
       mov       rcx,[rbx+18]
       cmp       qword ptr [rcx+10],88
       jle       short M09_L21
       mov       rcx,[rcx+88]
       test      rcx,rcx
       je        short M09_L21
       jmp       short M09_L22
M09_L21:
       mov       rcx,rbx
       mov       rdx,7FFA3EC1E458
       call      CORINFO_HELP_RUNTIMEHANDLE_METHOD
       mov       rcx,rax
M09_L22:
       call      CORINFO_HELP_NEWSFAST
       mov       rbx,rax
       mov       rcx,rbx
       mov       rdx,r13
       mov       r8,rdi
       call      qword ptr [7FFA3EC3D4B8]
       mov       rax,rbx
       jmp       near ptr M09_L05
M09_L23:
       mov       rcx,[rbx+18]
       cmp       qword ptr [rcx+10],60
       jle       short M09_L24
       mov       rcx,[rcx+60]
       test      rcx,rcx
       je        short M09_L24
       jmp       short M09_L25
M09_L24:
       mov       rcx,rbx
       mov       rdx,7FFA3EC1D7D0
       call      CORINFO_HELP_RUNTIMEHANDLE_METHOD
       mov       rcx,rax
M09_L25:
       call      CORINFO_HELP_NEWSFAST
       mov       rbx,rax
       mov       rcx,rbx
       mov       rdx,rsi
       mov       r8,rdi
       call      qword ptr [7FFA3EC3D4D0]
       mov       rax,rbx
       jmp       near ptr M09_L05
M09_L26:
       mov       ecx,10
       call      qword ptr [7FFA3E78F738]
       int       3
M09_L27:
       mov       ecx,11
       call      qword ptr [7FFA3E78F738]
       int       3
; Total bytes of code 887
```
```assembly
; System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       test      rdx,rdx
       je        short M10_L02
       cmp       [rdx],rcx
       je        short M10_L02
       mov       rax,[rdx]
       mov       r8,[rax+10]
M10_L00:
       cmp       r8,rcx
       je        short M10_L02
       test      r8,r8
       je        short M10_L01
       mov       r8,[r8+10]
       cmp       r8,rcx
       je        short M10_L02
       test      r8,r8
       je        short M10_L01
       mov       r8,[r8+10]
       cmp       r8,rcx
       je        short M10_L02
       test      r8,r8
       je        short M10_L01
       mov       r8,[r8+10]
       cmp       r8,rcx
       je        short M10_L02
       test      r8,r8
       jne       short M10_L03
M10_L01:
       xor       edx,edx
M10_L02:
       mov       rax,rdx
       ret
M10_L03:
       mov       r8,[r8+10]
       jmp       short M10_L00
; Total bytes of code 81
```
```assembly
; System.Linq.Enumerable+ListSelectIterator`2[[System.__Canon, System.Private.CoreLib],[System.__Canon, System.Private.CoreLib]].ToArray()
       push      r15
       push      r14
       push      r13
       push      r12
       push      rdi
       push      rsi
       push      rbp
       push      rbx
       sub       rsp,38
       xor       eax,eax
       mov       [rsp+28],rax
       mov       [rsp+30],rcx
       mov       rbx,rcx
       mov       rsi,[rbx]
       mov       rcx,[rbx+18]
       xor       edi,edi
       xor       ebp,ebp
       test      rcx,rcx
       je        short M11_L00
       mov       ebp,[rcx+10]
       mov       rdi,[rcx+8]
       cmp       [rdi+8],ebp
       jb        near ptr M11_L09
       add       rdi,10
M11_L00:
       test      ebp,ebp
       je        near ptr M11_L11
       mov       rcx,[rsi+30]
       mov       rcx,[rcx+8]
       mov       rcx,[rcx+50]
       test      rcx,rcx
       je        near ptr M11_L06
M11_L01:
       mov       edx,ebp
       call      CORINFO_HELP_NEWARR_1_OBJ
       mov       r14,rax
       mov       rcx,[rsi+30]
       mov       rcx,[rcx+8]
       mov       rcx,[rcx+58]
       test      rcx,rcx
       je        near ptr M11_L07
M11_L02:
       mov       rdx,[rcx+30]
       mov       rdx,[rdx]
       mov       rax,[rdx+30]
       test      rax,rax
       je        near ptr M11_L08
M11_L03:
       cmp       [r14],rax
       jne       near ptr M11_L10
       lea       rsi,[r14+10]
       mov       r15d,[r14+8]
       mov       r13,[rbx+20]
       xor       r12d,r12d
       test      r15d,r15d
       jle       short M11_L05
M11_L04:
       lea       rcx,[rsi+r12*8]
       mov       [rsp+28],rcx
       cmp       r12d,ebp
       jae       near ptr M11_L14
       mov       rdx,[rdi+r12*8]
       mov       rcx,[r13+8]
       call      qword ptr [r13+18]
       mov       rcx,[rsp+28]
       mov       rdx,rax
       call      CORINFO_HELP_CHECKED_ASSIGN_REF
       inc       r12d
       cmp       r12d,r15d
       jl        short M11_L04
M11_L05:
       mov       rax,r14
       add       rsp,38
       pop       rbx
       pop       rbp
       pop       rsi
       pop       rdi
       pop       r12
       pop       r13
       pop       r14
       pop       r15
       ret
M11_L06:
       mov       rcx,rsi
       mov       rdx,7FFA3EC80D08
       call      CORINFO_HELP_RUNTIMEHANDLE_CLASS
       mov       rcx,rax
       jmp       near ptr M11_L01
M11_L07:
       mov       rcx,rsi
       mov       rdx,7FFA3EC80D80
       call      CORINFO_HELP_RUNTIMEHANDLE_CLASS
       mov       rcx,rax
       jmp       near ptr M11_L02
M11_L08:
       mov       rdx,7FFA3EC80DC8
       call      CORINFO_HELP_RUNTIMEHANDLE_CLASS
       jmp       near ptr M11_L03
M11_L09:
       call      qword ptr [7FFA3E78F2A0]
       int       3
M11_L10:
       call      qword ptr [7FFA3EC3CC60]
       int       3
M11_L11:
       mov       rcx,[rsi+30]
       mov       rcx,[rcx+8]
       mov       rcx,[rcx+60]
       test      rcx,rcx
       je        short M11_L12
       jmp       short M11_L13
M11_L12:
       mov       rcx,rsi
       mov       rdx,7FFA3EC80DA8
       call      CORINFO_HELP_RUNTIMEHANDLE_CLASS
       mov       rcx,rax
M11_L13:
       call      qword ptr [7FFA3EAA72D0]; System.Array.Empty[[System.__Canon, System.Private.CoreLib]]()
       nop
       add       rsp,38
       pop       rbx
       pop       rbp
       pop       rsi
       pop       rdi
       pop       r12
       pop       r13
       pop       r14
       pop       r15
       ret
M11_L14:
       call      CORINFO_HELP_RNGCHKFAIL
       int       3
; Total bytes of code 403
```
```assembly
; Stateless.StateMachine`2+StateRepresentation[[Benchmark.HsmState, Benchmark],[Benchmark.HsmTrigger, Benchmark]].TryFindLocalHandlerResult(Benchmark.HsmTrigger, System.Collections.Generic.IEnumerable`1<TriggerBehaviourResult<Benchmark.HsmState,Benchmark.HsmTrigger>>)
       push      r15
       push      r14
       push      r13
       push      r12
       push      rdi
       push      rsi
       push      rbp
       push      rbx
       sub       rsp,198
       xor       eax,eax
       mov       [rsp+28],rax
       vxorps    xmm4,xmm4,xmm4
       vmovdqu32 [rsp+30],zmm4
       vmovdqu32 [rsp+70],zmm4
       vmovdqu32 [rsp+0B0],zmm4
       vmovdqu32 [rsp+0F0],zmm4
       vmovdqu32 [rsp+130],zmm4
       vmovdqa   xmmword ptr [rsp+170],xmm4
       vmovdqa   xmmword ptr [rsp+180],xmm4
       mov       [rsp+190],rax
       mov       rsi,rcx
       mov       edi,edx
       mov       rbx,r8
       mov       rdx,1F78D800A48
       mov       rbp,[rdx]
       test      rbp,rbp
       je        near ptr M12_L23
M12_L00:
       test      rbx,rbx
       je        near ptr M12_L40
       test      rbp,rbp
       je        near ptr M12_L39
       mov       rdx,rbx
       mov       rcx,offset MT_System.Linq.Enumerable+Iterator<Stateless.StateMachine<Benchmark.HsmState, Benchmark.HsmTrigger>+TriggerBehaviourResult>
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       test      rax,rax
       jne       near ptr M12_L26
       mov       rdx,rbx
       mov       rcx,offset MT_Stateless.StateMachine<Benchmark.HsmState, Benchmark.HsmTrigger>+TriggerBehaviourResult[]
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfAny(Void*, System.Object)
       mov       r14,rax
       test      r14,r14
       jne       near ptr M12_L07
       mov       rdx,rbx
       mov       rcx,offset MT_System.Collections.Generic.List<Stateless.StateMachine<Benchmark.HsmState, Benchmark.HsmTrigger>+TriggerBehaviourResult>
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       r14,rax
       test      r14,r14
       je        near ptr M12_L24
       mov       rcx,offset MT_System.Linq.Enumerable+ListWhereIterator<Stateless.StateMachine<Benchmark.HsmState, Benchmark.HsmTrigger>+TriggerBehaviourResult>
       call      CORINFO_HELP_NEWSFAST
       mov       r15,rax
       call      CORINFO_HELP_GETCURRENTMANAGEDTHREADID
       mov       [r15+10],eax
       lea       rcx,[r15+18]
       mov       rdx,r14
       call      CORINFO_HELP_ASSIGN_REF
       lea       rcx,[r15+20]
       mov       rdx,rbp
       call      CORINFO_HELP_ASSIGN_REF
M12_L01:
       test      r15,r15
       je        near ptr M12_L40
       mov       rdx,r15
       mov       rcx,offset MT_System.Linq.Enumerable+Iterator<Stateless.StateMachine<Benchmark.HsmState, Benchmark.HsmTrigger>+TriggerBehaviourResult>
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       test      rax,rax
       je        near ptr M12_L36
       mov       rcx,offset MT_System.Linq.Enumerable+ListWhereSelectIterator<Stateless.StateMachine<Benchmark.HsmState, Benchmark.HsmTrigger>+GuardCondition, System.String>
       cmp       [rax],rcx
       jne       near ptr M12_L22
       mov       rcx,[rax+18]
       xor       r14d,r14d
       xor       ebx,ebx
       test      rcx,rcx
       je        short M12_L02
       mov       ebx,[rcx+10]
       mov       r14,[rcx+8]
       cmp       [r14+8],ebx
       jb        near ptr M12_L35
       add       r14,10
M12_L02:
       mov       r13,[rax+20]
       mov       r12,[rax+28]
       vxorps    ymm0,ymm0,ymm0
       vmovdqu32 [rsp+158],zmm0
       vxorps    ymm0,ymm0,ymm0
       vmovdqu32 [rsp+60],zmm0
       vmovdqu32 [rsp+0A0],zmm0
       vmovdqu32 [rsp+0E0],zmm0
       vmovdqu   ymmword ptr [rsp+118],ymm0
       xor       ecx,ecx
       mov       [rsp+50],ecx
       mov       [rsp+54],ecx
       mov       [rsp+58],ecx
       lea       rcx,[rsp+158]
       mov       [rsp+138],rcx
       mov       dword ptr [rsp+140],8
       lea       rcx,[rsp+158]
       mov       [rsp+148],rcx
       mov       dword ptr [rsp+150],8
       test      ebx,ebx
       jg        near ptr M12_L08
M12_L03:
       mov       r12d,[rsp+54]
       add       r12d,[rsp+58]
       jo        near ptr M12_L41
       test      r12d,r12d
       jne       near ptr M12_L14
       mov       rcx,offset MT_System.Collections.Generic.List<System.String>
       call      CORINFO_HELP_NEWSFAST
       mov       r12,rax
       mov       rcx,1F78D800498
       mov       rdx,[rcx]
       lea       rcx,[r12+8]
       call      CORINFO_HELP_ASSIGN_REF
M12_L04:
       mov       r8d,[rsp+50]
       test      r8d,r8d
       jne       near ptr M12_L21
M12_L05:
       cmp       dword ptr [r12+10],1
       jg        near ptr M12_L38
       test      r12,r12
       je        near ptr M12_L40
       mov       rdx,r12
       mov       rcx,offset MT_System.Linq.Enumerable+Iterator<Stateless.StateMachine<Benchmark.HsmState, Benchmark.HsmTrigger>+TriggerBehaviourResult>
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       test      rax,rax
       jne       near ptr M12_L37
       lea       r8,[rsp+38]
       mov       rdx,r12
       mov       rcx,7FFA3EB8F718
       call      qword ptr [7FFA3EB45F20]; System.Linq.Enumerable.TryGetFirstNonIterator[[System.__Canon, System.Private.CoreLib]](System.Collections.Generic.IEnumerable`1<System.__Canon>, Boolean ByRef)
M12_L06:
       nop
       add       rsp,198
       pop       rbx
       pop       rbp
       pop       rsi
       pop       rdi
       pop       r12
       pop       r13
       pop       r14
       pop       r15
       ret
M12_L07:
       cmp       dword ptr [r14+8],0
       je        near ptr M12_L25
       mov       rcx,offset MT_System.Linq.Enumerable+ArrayWhereIterator<Stateless.StateMachine<Benchmark.HsmState, Benchmark.HsmTrigger>+TriggerBehaviourResult>
       call      CORINFO_HELP_NEWSFAST
       mov       r15,rax
       call      CORINFO_HELP_GETCURRENTMANAGEDTHREADID
       mov       [r15+10],eax
       lea       rcx,[r15+18]
       mov       rdx,r14
       call      CORINFO_HELP_ASSIGN_REF
       lea       rcx,[r15+20]
       mov       rdx,rbp
       call      CORINFO_HELP_ASSIGN_REF
       jmp       near ptr M12_L01
M12_L08:
       test      r13,r13
       je        short M12_L11
       mov       rcx,offset Stateless.StateMachine`2+TransitionGuard+<>c__DisplayClass17_0[[Benchmark.HsmState, Benchmark],[Benchmark.HsmTrigger, Benchmark]].<UnmetGuardConditions>b__0(GuardCondition<Benchmark.HsmState,Benchmark.HsmTrigger>)
       cmp       [r13+18],rcx
       jne       short M12_L11
       xor       ebp,ebp
M12_L09:
       mov       r15,[r14+rbp]
       mov       rcx,[r13+8]
       mov       rax,[r15+10]
       mov       rdx,[rcx+8]
       mov       rcx,offset Stateless.StateMachine`2+StateConfiguration+<>c[[Benchmark.HsmState, Benchmark],[Benchmark.HsmTrigger, Benchmark]].<InternalTransition>b__58_0(System.Object[])
       cmp       [rax+18],rcx
       jne       near ptr M12_L27
M12_L10:
       add       rbp,8
       dec       ebx
       jne       short M12_L09
       jmp       near ptr M12_L03
M12_L11:
       xor       ebp,ebp
M12_L12:
       mov       r15,[r14+rbp]
       mov       rax,offset Stateless.StateMachine`2+TransitionGuard+<>c__DisplayClass17_0[[Benchmark.HsmState, Benchmark],[Benchmark.HsmTrigger, Benchmark]].<UnmetGuardConditions>b__0(GuardCondition<Benchmark.HsmState,Benchmark.HsmTrigger>)
       cmp       [r13+18],rax
       jne       near ptr M12_L30
       mov       rcx,[r13+8]
       mov       rax,[r15+10]
       mov       rdx,[rcx+8]
       mov       rcx,offset Stateless.StateMachine`2+StateConfiguration+<>c[[Benchmark.HsmState, Benchmark],[Benchmark.HsmTrigger, Benchmark]].<InternalTransition>b__58_0(System.Object[])
       cmp       [rax+18],rcx
       jne       near ptr M12_L29
M12_L13:
       add       rbp,8
       dec       ebx
       jne       short M12_L12
       jmp       near ptr M12_L03
M12_L14:
       mov       rcx,offset MT_System.Collections.Generic.List<System.String>
       call      CORINFO_HELP_NEWSFAST
       mov       r15,rax
       mov       rcx,r15
       mov       edx,r12d
       call      qword ptr [7FFA3EB45E78]; System.Collections.Generic.List`1[[System.__Canon, System.Private.CoreLib]]..ctor(Int32)
       mov       r8d,r12d
       mov       rdx,r15
       mov       rcx,7FFA3EC97FD8
       call      qword ptr [7FFA3EB45E90]; System.Runtime.InteropServices.CollectionsMarshal.SetCount[[System.__Canon, System.Private.CoreLib]](System.Collections.Generic.List`1<System.__Canon>, Int32)
       mov       r13d,[r15+10]
       mov       rbx,[r15+8]
       cmp       [rbx+8],r13d
       jb        near ptr M12_L35
       add       rbx,10
       mov       rbp,rbx
       mov       r14d,r13d
       mov       r12d,[rsp+50]
       test      r12d,r12d
       jne       short M12_L16
M12_L15:
       mov       eax,[rsp+58]
       cmp       eax,[rsp+150]
       jbe       near ptr M12_L20
       jmp       near ptr M12_L33
M12_L16:
       vmovdqu   xmm0,xmmword ptr [rsp+138]
       vmovdqu   xmmword ptr [rsp+28],xmm0
       lea       r8,[rsp+28]
       lea       rcx,[rsp+40]
       mov       rdx,offset MT_System.Span<System.String>
       call      qword ptr [7FFA3EB457D0]; System.Span`1[[System.__Canon, System.Private.CoreLib]].op_Implicit(System.Span`1<System.__Canon>)
       mov       ebp,[rsp+48]
       cmp       ebp,r13d
       ja        near ptr M12_L34
       mov       r14d,ebp
       shl       r14,3
       mov       r8,r14
       mov       rcx,rbx
       mov       rdx,[rsp+40]
       call      qword ptr [7FFA3E785740]
       add       rbx,r14
       sub       r13d,ebp
       mov       rbp,rbx
       mov       r14d,r13d
       dec       r12d
       je        short M12_L15
       cmp       r12d,1B
       ja        near ptr M12_L33
       mov       ebx,r12d
       xor       r13d,r13d
       test      ebx,ebx
       jle       near ptr M12_L15
M12_L17:
       cmp       r13d,ebx
       jae       near ptr M12_L42
       lea       r8,[rsp+60]
       mov       r8,[r8+r13*8]
       test      r8,r8
       je        short M12_L19
       lea       rdx,[r8+10]
       mov       r12d,[r8+8]
M12_L18:
       cmp       r12d,r14d
       ja        near ptr M12_L34
       mov       eax,r12d
       shl       rax,3
       mov       [rsp+20],rax
       mov       r8,rax
       mov       rcx,rbp
       call      qword ptr [7FFA3E785740]
       mov       r8,[rsp+20]
       add       rbp,r8
       sub       r14d,r12d
       inc       r13d
       cmp       r13d,ebx
       jl        short M12_L17
       jmp       near ptr M12_L15
M12_L19:
       xor       edx,edx
       xor       r12d,r12d
       jmp       short M12_L18
M12_L20:
       mov       rdx,[rsp+148]
       cmp       eax,r14d
       ja        near ptr M12_L34
       mov       r8d,eax
       shl       r8,3
       mov       rcx,rbp
       call      qword ptr [7FFA3E785740]
       mov       r12,r15
       jmp       near ptr M12_L04
M12_L21:
       lea       rcx,[rsp+50]
       mov       rdx,offset MT_System.Collections.Generic.SegmentedArrayBuilder<System.String>
       call      qword ptr [7FFA3EC3D080]
       jmp       near ptr M12_L05
M12_L22:
       mov       rcx,rax
       mov       rax,[rax]
       mov       rax,[rax+40]
       call      qword ptr [rax+28]
       mov       r12,rax
       jmp       near ptr M12_L05
M12_L23:
       mov       rcx,offset MT_System.Func<Stateless.StateMachine<Benchmark.HsmState, Benchmark.HsmTrigger>+TriggerBehaviourResult, System.Boolean>
       call      CORINFO_HELP_NEWSFAST
       mov       rbp,rax
       mov       rdx,1F78D800A40
       mov       rdx,[rdx]
       mov       rcx,rbp
       mov       r8,offset Stateless.StateMachine`2+StateRepresentation+<>c[[Benchmark.HsmState, Benchmark],[Benchmark.HsmTrigger, Benchmark]].<TryFindLocalHandlerResult>b__42_0(TriggerBehaviourResult<Benchmark.HsmState,Benchmark.HsmTrigger>)
       call      qword ptr [7FFA3E7869D0]; System.MulticastDelegate.CtorClosed(System.Object, IntPtr)
       mov       rcx,1F78D800A48
       mov       rdx,rbp
       call      CORINFO_HELP_ASSIGN_REF
       jmp       near ptr M12_L00
M12_L24:
       mov       rcx,offset MT_System.Linq.Enumerable+IEnumerableWhereIterator<Stateless.StateMachine<Benchmark.HsmState, Benchmark.HsmTrigger>+TriggerBehaviourResult>
       call      CORINFO_HELP_NEWSFAST
       mov       r15,rax
       mov       rcx,r15
       mov       rdx,rbx
       mov       r8,rbp
       call      qword ptr [7FFA3EC3D4E8]
       jmp       near ptr M12_L01
M12_L25:
       mov       rcx,offset MT_System.Array+EmptyArray<Stateless.StateMachine<Benchmark.HsmState, Benchmark.HsmTrigger>+TriggerBehaviourResult>
       call      CORINFO_HELP_GET_GCSTATIC_BASE
       mov       rcx,1F78D8014A0
       mov       r15,[rcx]
       jmp       near ptr M12_L01
M12_L26:
       mov       rcx,rax
       mov       rdx,rbp
       mov       rax,[rax]
       mov       rax,[rax+50]
       call      qword ptr [rax+8]
       mov       r15,rax
       jmp       near ptr M12_L01
M12_L27:
       mov       rcx,[rax+8]
       call      qword ptr [rax+18]
       test      eax,eax
       jne       near ptr M12_L10
       mov       rdx,r15
       mov       rcx,[r12+8]
       call      qword ptr [r12+18]
       mov       rdx,rax
       mov       rax,[rsp+148]
       mov       r8d,[rsp+150]
       mov       r10d,[rsp+58]
       cmp       r10d,r8d
       jae       short M12_L28
       mov       ecx,r10d
       lea       rcx,[rax+rcx*8]
       call      CORINFO_HELP_CHECKED_ASSIGN_REF
       mov       ecx,[rsp+58]
       inc       ecx
       mov       [rsp+58],ecx
       jmp       near ptr M12_L10
M12_L28:
       lea       rcx,[rsp+50]
       mov       r8,rdx
       mov       rdx,offset MT_System.Collections.Generic.SegmentedArrayBuilder<System.String>
       call      qword ptr [7FFA3EC3D068]
       jmp       near ptr M12_L10
M12_L29:
       mov       rcx,[rax+8]
       call      qword ptr [rax+18]
       test      eax,eax
       jne       near ptr M12_L13
       jmp       short M12_L31
M12_L30:
       mov       rdx,r15
       mov       rcx,[r13+8]
       call      qword ptr [r13+18]
       test      eax,eax
       je        near ptr M12_L13
M12_L31:
       mov       rdx,r15
       mov       rcx,[r12+8]
       call      qword ptr [r12+18]
       mov       rdx,rax
       mov       rax,[rsp+148]
       mov       r8d,[rsp+150]
       mov       r10d,[rsp+58]
       cmp       r10d,r8d
       jae       short M12_L32
       mov       ecx,r10d
       lea       rcx,[rax+rcx*8]
       call      CORINFO_HELP_CHECKED_ASSIGN_REF
       mov       ecx,[rsp+58]
       inc       ecx
       mov       [rsp+58],ecx
       jmp       near ptr M12_L13
M12_L32:
       lea       rcx,[rsp+50]
       mov       r8,rdx
       mov       rdx,offset MT_System.Collections.Generic.SegmentedArrayBuilder<System.String>
       call      qword ptr [7FFA3EC3D068]
       jmp       near ptr M12_L13
M12_L33:
       call      qword ptr [7FFA3E997798]
       int       3
M12_L34:
       call      qword ptr [7FFA3EC3CA68]
       int       3
M12_L35:
       call      qword ptr [7FFA3E78F2A0]
       int       3
M12_L36:
       mov       rcx,offset MT_System.Collections.Generic.List<Stateless.StateMachine<Benchmark.HsmState, Benchmark.HsmTrigger>+TriggerBehaviourResult>
       call      CORINFO_HELP_NEWSFAST
       mov       r12,rax
       mov       rcx,r12
       mov       rdx,r15
       call      qword ptr [7FFA3EB44D20]; System.Collections.Generic.List`1[[System.__Canon, System.Private.CoreLib]]..ctor(System.Collections.Generic.IEnumerable`1<System.__Canon>)
       jmp       near ptr M12_L05
M12_L37:
       lea       rdx,[rsp+38]
       mov       rcx,rax
       mov       rax,[rax]
       mov       rax,[rax+48]
       call      qword ptr [rax+10]
       jmp       near ptr M12_L06
M12_L38:
       mov       rcx,offset MT_Benchmark.HsmTrigger
       call      CORINFO_HELP_NEWSFAST
       mov       rbx,rax
       call      qword ptr [7FFA3EB45DB8]
       mov       rbp,rax
       mov       [rbx+8],edi
       mov       rcx,offset MT_Benchmark.HsmState
       call      CORINFO_HELP_NEWSFAST
       mov       rdi,rax
       mov       ecx,[rsi+40]
       mov       [rdi+8],ecx
       mov       rcx,offset MT_System.InvalidOperationException
       call      CORINFO_HELP_NEWSFAST
       mov       rsi,rax
       mov       r8,rdi
       mov       rdx,rbx
       mov       rcx,rbp
       call      qword ptr [7FFA3EB45DD0]
       mov       rdx,rax
       mov       rcx,rsi
       call      qword ptr [7FFA3EAA6C70]
       mov       rcx,rsi
       call      CORINFO_HELP_THROW
       int       3
M12_L39:
       mov       ecx,0C
       call      qword ptr [7FFA3E78F738]
       int       3
M12_L40:
       mov       ecx,11
       call      qword ptr [7FFA3E78F738]
       int       3
M12_L41:
       call      CORINFO_HELP_OVERFLOW
       int       3
M12_L42:
       call      CORINFO_HELP_RNGCHKFAIL
       int       3
; Total bytes of code 1967
```
```assembly
; Stateless.StateMachine`2+StateRepresentation[[Benchmark.HsmState, Benchmark],[Benchmark.HsmTrigger, Benchmark]].TryFindHandler(Benchmark.HsmTrigger, System.Object[], TriggerBehaviourResult<Benchmark.HsmState,Benchmark.HsmTrigger> ByRef)
M13_L00:
       push      r15
       push      r14
       push      r13
       push      r12
       push      rdi
       push      rsi
       push      rbp
       push      rbx
       sub       rsp,78
       vxorps    xmm4,xmm4,xmm4
       vmovdqu   ymmword ptr [rsp+50],ymm4
       xor       eax,eax
       mov       [rsp+70],rax
       mov       rsi,rcx
       mov       ebx,edx
       mov       rdi,r8
       mov       rbp,r9
       xor       r14d,r14d
       mov       rcx,offset MT_Stateless.StateMachine<Benchmark.HsmState, Benchmark.HsmTrigger>+StateRepresentation+<>c__DisplayClass41_0
       call      CORINFO_HELP_NEWSFAST
       mov       r15,rax
       lea       rcx,[r15+8]
       mov       rdx,rdi
       call      CORINFO_HELP_ASSIGN_REF
       mov       r13,[rsi+8]
       mov       rcx,offset MT_System.Collections.Generic.Dictionary<Benchmark.HsmTrigger, System.Collections.Generic.ICollection<Stateless.StateMachine<Benchmark.HsmState, Benchmark.HsmTrigger>+TriggerBehaviour>>
       cmp       [r13],rcx
       jne       near ptr M13_L29
       mov       edx,ebx
       mov       rcx,[r13+8]
       test      rcx,rcx
       je        near ptr M13_L05
       mov       r12,[r13+18]
       test      r12,r12
       jne       near ptr M13_L26
       mov       eax,edx
       imul      rax,[r13+30]
       shr       rax,20
       inc       rax
       mov       edx,[rcx+8]
       mov       r8d,edx
       imul      rax,r8
       shr       rax,20
       cmp       eax,edx
       jae       near ptr M13_L42
       mov       eax,eax
       lea       rcx,[rcx+rax*4+10]
       mov       r12d,[rcx]
       mov       r13,[r13+10]
       xor       eax,eax
       dec       r12d
       mov       edx,[r13+8]
M13_L01:
       cmp       edx,r12d
       jbe       short M13_L05
       mov       ecx,r12d
       lea       rcx,[rcx+rcx*2]
       lea       r12,[r13+rcx*8+10]
       cmp       [r12+8],ebx
       jne       near ptr M13_L25
       cmp       [r12+10],ebx
       jne       near ptr M13_L25
M13_L02:
       jmp       short M13_L06
M13_L03:
       mov       r10d,[r13+8]
       cmp       r10d,edx
       jbe       short M13_L05
       mov       edx,edx
       lea       rdx,[rdx+rdx*2]
       lea       rdx,[r13+rdx*8+10]
       mov       r9,rdx
       mov       [rsp+64],eax
       cmp       [r9+8],eax
       je        near ptr M13_L27
M13_L04:
       mov       edx,[r9+0C]
       inc       ecx
       mov       [rsp+60],ecx
       cmp       r10d,ecx
       mov       eax,[rsp+64]
       mov       ecx,[rsp+60]
       jae       short M13_L03
       jmp       near ptr M13_L30
M13_L05:
       xor       r12d,r12d
M13_L06:
       test      r12,r12
       jne       short M13_L08
       xor       ecx,ecx
       mov       [rsp+68],rcx
M13_L07:
       xor       r13d,r13d
       jmp       near ptr M13_L36
M13_L08:
       mov       rcx,[r12]
       mov       [rsp+68],rcx
M13_L09:
       mov       rcx,offset MT_System.Func<Stateless.StateMachine<Benchmark.HsmState, Benchmark.HsmTrigger>+TriggerBehaviour, Stateless.StateMachine<Benchmark.HsmState, Benchmark.HsmTrigger>+TriggerBehaviourResult>
       call      CORINFO_HELP_NEWSFAST
       mov       r13,rax
       mov       r12,[rsp+68]
       lea       rcx,[r13+8]
       mov       rdx,r15
       call      CORINFO_HELP_ASSIGN_REF
       mov       r8,offset Stateless.StateMachine`2+StateRepresentation+<>c__DisplayClass41_0[[Benchmark.HsmState, Benchmark],[Benchmark.HsmTrigger, Benchmark]].<TryFindLocalHandler>b__0(TriggerBehaviour<Benchmark.HsmState,Benchmark.HsmTrigger>)
       mov       [r13+18],r8
       mov       r8,r13
       mov       rdx,r12
       mov       rcx,7FFA3EB88DE0
       call      qword ptr [7FFA3EAA7540]; System.Linq.Enumerable.Select[[System.__Canon, System.Private.CoreLib],[System.__Canon, System.Private.CoreLib]](System.Collections.Generic.IEnumerable`1<System.__Canon>, System.Func`2<System.__Canon,System.__Canon>)
       mov       r13,rax
       mov       rdx,r13
       mov       rcx,offset MT_System.Linq.Enumerable+Iterator<Stateless.StateMachine<Benchmark.HsmState, Benchmark.HsmTrigger>+TriggerBehaviourResult>
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       r12,rax
       test      r12,r12
       je        near ptr M13_L33
       mov       rdx,offset MT_System.Linq.Enumerable+ListSelectIterator<Stateless.StateMachine<Benchmark.HsmState, Benchmark.HsmTrigger>+TriggerBehaviour, Stateless.StateMachine<Benchmark.HsmState, Benchmark.HsmTrigger>+TriggerBehaviourResult>
       cmp       [r12],rdx
       jne       near ptr M13_L32
       mov       rdx,[r12+18]
       xor       r15d,r15d
       xor       r13d,r13d
       test      rdx,rdx
       je        short M13_L10
       mov       r13d,[rdx+10]
       mov       r15,[rdx+8]
       cmp       [r15+8],r13d
       jb        near ptr M13_L30
       add       r15,10
M13_L10:
       test      r13d,r13d
       je        near ptr M13_L31
       mov       edx,r13d
       mov       rcx,offset MT_Stateless.StateMachine<Benchmark.HsmState, Benchmark.HsmTrigger>+TriggerBehaviourResult[]
       call      CORINFO_HELP_NEWARR_1_OBJ
       mov       [rsp+30],rax
       lea       r8,[rax+10]
       mov       r10d,[rax+8]
       mov       [rsp+28],r8
       mov       [rsp+4C],r10d
       mov       r12,[r12+20]
       xor       r9d,r9d
       test      r10d,r10d
       jle       short M13_L12
M13_L11:
       lea       rcx,[r8+r9*8]
       mov       [rsp+70],rcx
       cmp       r9d,r13d
       jae       near ptr M13_L42
       mov       [rsp+40],r9
       mov       rdx,[r15+r9*8]
       mov       rcx,[r12+8]
       call      qword ptr [r12+18]
       mov       rcx,[rsp+70]
       mov       rdx,rax
       call      CORINFO_HELP_CHECKED_ASSIGN_REF
       mov       rcx,[rsp+40]
       inc       ecx
       mov       edx,[rsp+4C]
       cmp       ecx,edx
       mov       r9,rcx
       mov       r8,[rsp+28]
       jl        short M13_L11
M13_L12:
       mov       rax,[rsp+30]
       mov       r15,rax
M13_L13:
       mov       rcx,rsi
       mov       edx,ebx
       mov       r8,r15
       call      qword ptr [7FFA3EB45650]; Stateless.StateMachine`2+StateRepresentation[[Benchmark.HsmState, Benchmark],[Benchmark.HsmTrigger, Benchmark]].TryFindLocalHandlerResult(Benchmark.HsmTrigger, System.Collections.Generic.IEnumerable`1<TriggerBehaviourResult<Benchmark.HsmState,Benchmark.HsmTrigger>>)
       mov       r13,rax
       test      r13,r13
       je        near ptr M13_L35
M13_L14:
       test      r13,r13
       je        near ptr M13_L36
       mov       rdx,[r13+10]
       mov       rcx,7FFA3EB89030
       call      qword ptr [7FFA3EB45230]; System.Linq.Enumerable.Any[[System.__Canon, System.Private.CoreLib]](System.Collections.Generic.IEnumerable`1<System.__Canon>)
       test      eax,eax
       sete      r12b
       movzx     r12d,r12b
M13_L15:
       xor       ecx,ecx
       mov       [rsp+68],rcx
       test      r12d,r12d
       jne       near ptr M13_L24
       mov       r12,[rsi+30]
       test      r12,r12
       je        near ptr M13_L41
       cmp       [r12],r12b
       xor       ecx,ecx
       mov       [rsp+58],rcx
       mov       rcx,offset MT_Stateless.StateMachine<Benchmark.HsmState, Benchmark.HsmTrigger>+StateRepresentation+<>c__DisplayClass41_0
       call      CORINFO_HELP_NEWSFAST
       mov       r15,rax
       lea       rcx,[r15+8]
       mov       rdx,rdi
       call      CORINFO_HELP_ASSIGN_REF
       mov       rcx,[r12+8]
       mov       rdx,offset MT_System.Collections.Generic.Dictionary<Benchmark.HsmTrigger, System.Collections.Generic.ICollection<Stateless.StateMachine<Benchmark.HsmState, Benchmark.HsmTrigger>+TriggerBehaviour>>
       cmp       [rcx],rdx
       jne       near ptr M13_L37
       mov       edx,ebx
       call      qword ptr [7FFA3EB44DB0]; System.Collections.Generic.Dictionary`2[[Benchmark.HsmTrigger, Benchmark],[System.__Canon, System.Private.CoreLib]].FindValue(Benchmark.HsmTrigger)
       test      rax,rax
       jne       short M13_L17
       xor       eax,eax
       mov       [rsp+50],rax
M13_L16:
       xor       esi,esi
       jmp       near ptr M13_L39
M13_L17:
       mov       rcx,[rax]
       mov       [rsp+50],rcx
M13_L18:
       mov       rcx,offset MT_System.Func<Stateless.StateMachine<Benchmark.HsmState, Benchmark.HsmTrigger>+TriggerBehaviour, Stateless.StateMachine<Benchmark.HsmState, Benchmark.HsmTrigger>+TriggerBehaviourResult>
       call      CORINFO_HELP_NEWSFAST
       mov       rsi,rax
       mov       r14,[rsp+50]
       lea       rcx,[rsi+8]
       mov       rdx,r15
       call      CORINFO_HELP_ASSIGN_REF
       mov       r8,offset Stateless.StateMachine`2+StateRepresentation+<>c__DisplayClass41_0[[Benchmark.HsmState, Benchmark],[Benchmark.HsmTrigger, Benchmark]].<TryFindLocalHandler>b__0(TriggerBehaviour<Benchmark.HsmState,Benchmark.HsmTrigger>)
       mov       [rsi+18],r8
       mov       r8,rsi
       mov       rdx,r14
       mov       rcx,7FFA3EB88DE0
       call      qword ptr [7FFA3EAA7540]; System.Linq.Enumerable.Select[[System.__Canon, System.Private.CoreLib],[System.__Canon, System.Private.CoreLib]](System.Collections.Generic.IEnumerable`1<System.__Canon>, System.Func`2<System.__Canon,System.__Canon>)
       mov       rdx,rax
       mov       rcx,7FFA3EB88F20
       call      qword ptr [7FFA3EAA7660]; System.Linq.Enumerable.ToArray[[System.__Canon, System.Private.CoreLib]](System.Collections.Generic.IEnumerable`1<System.__Canon>)
       mov       rsi,rax
       mov       rcx,r12
       mov       edx,ebx
       mov       r8,rsi
       call      qword ptr [7FFA3EB45650]; Stateless.StateMachine`2+StateRepresentation[[Benchmark.HsmState, Benchmark],[Benchmark.HsmTrigger, Benchmark]].TryFindLocalHandlerResult(Benchmark.HsmTrigger, System.Collections.Generic.IEnumerable`1<TriggerBehaviourResult<Benchmark.HsmState,Benchmark.HsmTrigger>>)
       test      rax,rax
       je        near ptr M13_L38
M13_L19:
       mov       rsi,rax
       test      rsi,rsi
       je        near ptr M13_L39
       mov       rdx,[rsi+10]
       mov       rcx,7FFA3EB89030
       call      qword ptr [7FFA3EB45230]; System.Linq.Enumerable.Any[[System.__Canon, System.Private.CoreLib]](System.Collections.Generic.IEnumerable`1<System.__Canon>)
       test      eax,eax
       sete      r14b
       movzx     r14d,r14b
M13_L20:
       xor       ecx,ecx
       mov       [rsp+50],rcx
       test      r14d,r14d
       jne       short M13_L23
       mov       rcx,[r12+30]
       test      rcx,rcx
       je        near ptr M13_L40
       lea       r9,[rsp+58]
       mov       edx,ebx
       mov       r8,rdi
       cmp       [rcx],ecx
       call      qword ptr [7FFA3EB45470]
M13_L21:
       mov       rcx,[rsp+58]
       cmp       qword ptr [rsp+58],0
       cmove     rcx,rsi
       mov       r14,rcx
       movzx     esi,al
       xor       ecx,ecx
       mov       [rsp+58],rcx
M13_L22:
       mov       rdx,r14
       test      rdx,rdx
       cmove     rdx,r13
       mov       rcx,rbp
       call      CORINFO_HELP_CHECKED_ASSIGN_REF
       movzx     eax,sil
       add       rsp,78
       pop       rbx
       pop       rbp
       pop       rsi
       pop       rdi
       pop       r12
       pop       r13
       pop       r14
       pop       r15
       ret
M13_L23:
       mov       eax,1
       jmp       short M13_L21
M13_L24:
       mov       esi,1
       jmp       short M13_L22
M13_L25:
       mov       r12d,[r12+0C]
       inc       eax
       cmp       edx,eax
       jae       near ptr M13_L01
       jmp       near ptr M13_L30
M13_L26:
       mov       rcx,r12
       mov       r11,7FFA3E6E0680
       call      qword ptr [r11]
       mov       rdx,[r13+8]
       mov       ecx,eax
       imul      rcx,[r13+30]
       shr       rcx,20
       inc       rcx
       mov       r8d,[rdx+8]
       imul      rcx,r8
       shr       rcx,20
       cmp       ecx,[rdx+8]
       jae       near ptr M13_L42
       mov       ecx,ecx
       lea       rdx,[rdx+rcx*4+10]
       mov       edx,[rdx]
       mov       r13,[r13+10]
       xor       ecx,ecx
       dec       edx
       jmp       near ptr M13_L03
M13_L27:
       mov       [rsp+60],ecx
       mov       [rsp+48],r10d
       mov       [rsp+38],r9
       mov       edx,[r9+10]
       mov       rcx,r12
       mov       r8d,ebx
       mov       r11,7FFA3E6E0688
       call      qword ptr [r11]
       test      eax,eax
       mov       ecx,[rsp+60]
       mov       r10d,[rsp+48]
       jne       short M13_L28
       mov       r9,[rsp+38]
       jmp       near ptr M13_L04
M13_L28:
       mov       r12,[rsp+38]
       jmp       near ptr M13_L02
M13_L29:
       lea       r8,[rsp+68]
       mov       rcx,r13
       mov       edx,ebx
       mov       r11,7FFA3E6E0678
       call      qword ptr [r11]
       test      eax,eax
       jne       near ptr M13_L09
       jmp       near ptr M13_L07
M13_L30:
       call      qword ptr [7FFA3E78F2A0]
       int       3
M13_L31:
       mov       rcx,offset MT_System.Array+EmptyArray<Stateless.StateMachine<Benchmark.HsmState, Benchmark.HsmTrigger>+TriggerBehaviourResult>
       call      CORINFO_HELP_GET_GCSTATIC_BASE
       mov       rcx,1F78D8014A0
       mov       r15,[rcx]
       jmp       near ptr M13_L13
M13_L32:
       mov       rcx,r12
       mov       rax,[r12]
       mov       rax,[rax+40]
       call      qword ptr [rax+20]
       mov       r15,rax
       jmp       near ptr M13_L13
M13_L33:
       mov       rdx,r13
       mov       rcx,offset MT_System.Collections.Generic.ICollection<Stateless.StateMachine<Benchmark.HsmState, Benchmark.HsmTrigger>+TriggerBehaviourResult>
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfInterface(Void*, System.Object)
       test      rax,rax
       je        short M13_L34
       mov       rdx,rax
       mov       rcx,7FFA3EC97798
       call      qword ptr [7FFA3EAAD4A0]; System.Linq.Enumerable.ICollectionToArray[[System.__Canon, System.Private.CoreLib]](System.Collections.Generic.ICollection`1<System.__Canon>)
       mov       r15,rax
       jmp       near ptr M13_L13
M13_L34:
       mov       rdx,r13
       mov       rcx,7FFA3EC97820
       call      qword ptr [7FFA3EC3CAC8]
       mov       r15,rax
       jmp       near ptr M13_L13
M13_L35:
       mov       rcx,r15
       call      qword ptr [7FFA3EB45668]
       mov       r13,rax
       jmp       near ptr M13_L14
M13_L36:
       xor       r12d,r12d
       jmp       near ptr M13_L15
M13_L37:
       lea       r8,[rsp+50]
       mov       edx,ebx
       mov       r11,7FFA3E6E0690
       call      qword ptr [r11]
       test      eax,eax
       jne       near ptr M13_L18
       jmp       near ptr M13_L16
M13_L38:
       mov       rcx,rsi
       call      qword ptr [7FFA3EB45668]
       jmp       near ptr M13_L19
M13_L39:
       xor       r14d,r14d
       jmp       near ptr M13_L20
M13_L40:
       xor       eax,eax
       jmp       near ptr M13_L21
M13_L41:
       xor       esi,esi
       jmp       near ptr M13_L22
M13_L42:
       call      CORINFO_HELP_RNGCHKFAIL
       int       3
; Total bytes of code 1552
```
```assembly
; Stateless.StateMachine`2+StateRepresentation[[Benchmark.HsmState, Benchmark],[Benchmark.HsmTrigger, Benchmark]].InternalAction(Transition<Benchmark.HsmState,Benchmark.HsmTrigger>, System.Object[])
       push      r15
       push      r14
       push      r13
       push      r12
       push      rdi
       push      rsi
       push      rbp
       push      rbx
       sub       rsp,288
       xor       eax,eax
       mov       [rsp+88],rax
       vxorps    xmm4,xmm4,xmm4
       vmovdqa   xmmword ptr [rsp+90],xmm4
       mov       rax,0FFFFFFFFFFFFFE20
M14_L00:
       vmovdqa   xmmword ptr [rsp+rax+280],xmm4
       vmovdqa   xmmword ptr [rsp+rax+290],xmm4
       vmovdqa   xmmword ptr [rsp+rax+2A0],xmm4
       add       rax,30
       jne       short M14_L00
       mov       [rsp+280],rax
       mov       rbx,rdx
       mov       rsi,r8
       xor       edi,edi
       mov       rbp,rcx
       test      rcx,rcx
       je        near ptr M14_L26
M14_L01:
       mov       r14d,[rbx+18]
       xor       ecx,ecx
       mov       [rsp+278],rcx
       mov       rcx,offset MT_Stateless.StateMachine<Benchmark.HsmState, Benchmark.HsmTrigger>+StateRepresentation+<>c__DisplayClass41_0
       call      CORINFO_HELP_NEWSFAST
       mov       r15,rax
       lea       rcx,[r15+8]
       mov       rdx,rsi
       call      CORINFO_HELP_ASSIGN_REF
       mov       r13,[rbp+8]
       mov       rcx,offset MT_System.Collections.Generic.Dictionary<Benchmark.HsmTrigger, System.Collections.Generic.ICollection<Stateless.StateMachine<Benchmark.HsmState, Benchmark.HsmTrigger>+TriggerBehaviour>>
       cmp       [r13],rcx
       jne       near ptr M14_L45
       mov       edx,r14d
       mov       rcx,[r13+8]
       test      rcx,rcx
       je        near ptr M14_L06
       mov       r12,[r13+18]
       test      r12,r12
       jne       near ptr M14_L42
       mov       eax,edx
       imul      rax,[r13+30]
       shr       rax,20
       inc       rax
       mov       edx,[rcx+8]
       mov       r8d,edx
       imul      rax,r8
       shr       rax,20
       cmp       eax,edx
       jae       near ptr M14_L78
       mov       eax,eax
       lea       rcx,[rcx+rax*4+10]
       mov       r12d,[rcx]
       mov       r13,[r13+10]
       xor       eax,eax
       dec       r12d
       mov       edx,[r13+8]
M14_L02:
       cmp       edx,r12d
       jbe       short M14_L06
       mov       ecx,r12d
       lea       rcx,[rcx+rcx*2]
       lea       r12,[r13+rcx*8+10]
       cmp       [r12+8],r14d
       jne       near ptr M14_L41
       cmp       [r12+10],r14d
       jne       near ptr M14_L41
M14_L03:
       jmp       short M14_L07
M14_L04:
       mov       r10d,[r13+8]
       cmp       r10d,edx
       jbe       short M14_L06
       mov       edx,edx
       lea       rdx,[rdx+rdx*2]
       lea       rdx,[r13+rdx*8+10]
       mov       r9,rdx
       mov       [rsp+274],eax
       cmp       [r9+8],eax
       je        near ptr M14_L43
M14_L05:
       mov       edx,[r9+0C]
       inc       ecx
       mov       [rsp+270],ecx
       cmp       r10d,ecx
       mov       eax,[rsp+274]
       mov       ecx,[rsp+270]
       jae       short M14_L04
       jmp       near ptr M14_L64
M14_L06:
       xor       r12d,r12d
M14_L07:
       test      r12,r12
       jne       short M14_L09
       xor       ecx,ecx
       mov       [rsp+278],rcx
M14_L08:
       xor       r13d,r13d
       jmp       near ptr M14_L70
M14_L09:
       mov       rcx,[r12]
       mov       [rsp+278],rcx
M14_L10:
       mov       rcx,offset MT_System.Func<Stateless.StateMachine<Benchmark.HsmState, Benchmark.HsmTrigger>+TriggerBehaviour, Stateless.StateMachine<Benchmark.HsmState, Benchmark.HsmTrigger>+TriggerBehaviourResult>
       call      CORINFO_HELP_NEWSFAST
       mov       r13,rax
       mov       r12,[rsp+278]
       lea       rcx,[r13+8]
       mov       rdx,r15
       call      CORINFO_HELP_ASSIGN_REF
       mov       r8,offset Stateless.StateMachine`2+StateRepresentation+<>c__DisplayClass41_0[[Benchmark.HsmState, Benchmark],[Benchmark.HsmTrigger, Benchmark]].<TryFindLocalHandler>b__0(TriggerBehaviour<Benchmark.HsmState,Benchmark.HsmTrigger>)
       mov       [r13+18],r8
       mov       r8,r13
       mov       rdx,r12
       mov       rcx,7FFA3EB88DE0
       call      qword ptr [7FFA3EAA7540]; System.Linq.Enumerable.Select[[System.__Canon, System.Private.CoreLib],[System.__Canon, System.Private.CoreLib]](System.Collections.Generic.IEnumerable`1<System.__Canon>, System.Func`2<System.__Canon,System.__Canon>)
       mov       r13,rax
       mov       rdx,r13
       mov       rcx,offset MT_System.Linq.Enumerable+Iterator<Stateless.StateMachine<Benchmark.HsmState, Benchmark.HsmTrigger>+TriggerBehaviourResult>
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       r12,rax
       test      r12,r12
       je        near ptr M14_L48
       mov       rdx,offset MT_System.Linq.Enumerable+ListSelectIterator<Stateless.StateMachine<Benchmark.HsmState, Benchmark.HsmTrigger>+TriggerBehaviour, Stateless.StateMachine<Benchmark.HsmState, Benchmark.HsmTrigger>+TriggerBehaviourResult>
       cmp       [r12],rdx
       jne       near ptr M14_L47
       mov       rdx,[r12+18]
       xor       r15d,r15d
       xor       r13d,r13d
       test      rdx,rdx
       je        short M14_L11
       mov       r13d,[rdx+10]
       mov       r15,[rdx+8]
       cmp       [r15+8],r13d
       jb        near ptr M14_L64
       add       r15,10
M14_L11:
       test      r13d,r13d
       je        near ptr M14_L46
       mov       edx,r13d
       mov       rcx,offset MT_Stateless.StateMachine<Benchmark.HsmState, Benchmark.HsmTrigger>+TriggerBehaviourResult[]
       call      CORINFO_HELP_NEWARR_1_OBJ
       mov       [rsp+58],rax
       lea       r8,[rax+10]
       mov       r10d,[rax+8]
       mov       [rsp+28],r8
       mov       [rsp+0A4],r10d
       mov       r12,[r12+20]
       xor       r9d,r9d
       test      r10d,r10d
       jle       short M14_L13
M14_L12:
       lea       rcx,[r8+r9*8]
       mov       [rsp+280],rcx
       cmp       r9d,r13d
       jae       near ptr M14_L78
       mov       [rsp+68],r9
       mov       rdx,[r15+r9*8]
       mov       rcx,[r12+8]
       call      qword ptr [r12+18]
       mov       rcx,[rsp+280]
       mov       rdx,rax
       call      CORINFO_HELP_CHECKED_ASSIGN_REF
       mov       rdx,[rsp+68]
       inc       edx
       mov       ecx,[rsp+0A4]
       cmp       edx,ecx
       mov       r9,rdx
       mov       r8,[rsp+28]
       jl        short M14_L12
M14_L13:
       mov       rax,[rsp+58]
M14_L14:
       mov       r15,rax
M14_L15:
       mov       rdx,1F78D800A48
       mov       r13,[rdx]
       test      r13,r13
       je        near ptr M14_L50
M14_L16:
       test      r15,r15
       je        near ptr M14_L69
       test      r13,r13
       je        near ptr M14_L68
       mov       rdx,r15
       mov       rcx,offset MT_System.Linq.Enumerable+Iterator<Stateless.StateMachine<Benchmark.HsmState, Benchmark.HsmTrigger>+TriggerBehaviourResult>
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       test      rax,rax
       jne       near ptr M14_L53
       mov       r12,r15
       test      r12,r12
       jne       near ptr M14_L28
       mov       rdx,r15
       mov       rcx,offset MT_System.Collections.Generic.List<Stateless.StateMachine<Benchmark.HsmState, Benchmark.HsmTrigger>+TriggerBehaviourResult>
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       r12,rax
       test      r12,r12
       je        near ptr M14_L51
       mov       rcx,offset MT_System.Linq.Enumerable+ListWhereIterator<Stateless.StateMachine<Benchmark.HsmState, Benchmark.HsmTrigger>+TriggerBehaviourResult>
       call      CORINFO_HELP_NEWSFAST
       mov       [rsp+50],rax
       call      CORINFO_HELP_GETCURRENTMANAGEDTHREADID
       mov       r8,[rsp+50]
       mov       [r8+10],eax
       lea       rcx,[r8+18]
       mov       rdx,r12
       call      CORINFO_HELP_ASSIGN_REF
       mov       r12,[rsp+50]
       lea       rcx,[r12+20]
       mov       rdx,r13
       call      CORINFO_HELP_ASSIGN_REF
M14_L17:
       test      r12,r12
       je        near ptr M14_L69
       mov       rdx,r12
       mov       rcx,offset MT_System.Linq.Enumerable+Iterator<Stateless.StateMachine<Benchmark.HsmState, Benchmark.HsmTrigger>+TriggerBehaviourResult>
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       test      rax,rax
       je        near ptr M14_L65
       mov       rcx,offset MT_System.Linq.Enumerable+ListWhereSelectIterator<Stateless.StateMachine<Benchmark.HsmState, Benchmark.HsmTrigger>+GuardCondition, System.String>
       cmp       [rax],rcx
       jne       near ptr M14_L39
       mov       rcx,[rax+18]
       xor       r12d,r12d
       xor       r13d,r13d
       test      rcx,rcx
       je        short M14_L18
       mov       r13d,[rcx+10]
       mov       r12,[rcx+8]
       cmp       [r12+8],r13d
       jb        near ptr M14_L64
       add       r12,10
M14_L18:
       mov       rdx,[rax+20]
       mov       [rsp+38],rdx
       mov       r8,[rax+28]
       mov       [rsp+30],r8
       vxorps    ymm0,ymm0,ymm0
       vmovdqu32 [rsp+230],zmm0
       vxorps    ymm0,ymm0,ymm0
       vmovdqu32 [rsp+138],zmm0
       vmovdqu32 [rsp+178],zmm0
       vmovdqu32 [rsp+1B8],zmm0
       vmovdqu   ymmword ptr [rsp+1F0],ymm0
       xor       ecx,ecx
       mov       [rsp+128],ecx
       mov       [rsp+12C],ecx
       mov       [rsp+130],ecx
       lea       rcx,[rsp+230]
       mov       [rsp+210],rcx
       mov       dword ptr [rsp+218],8
       lea       rcx,[rsp+230]
       mov       [rsp+220],rcx
       mov       dword ptr [rsp+228],8
       xor       eax,eax
       test      r13d,r13d
       jg        near ptr M14_L29
M14_L19:
       mov       r12d,[rsp+12C]
       add       r12d,[rsp+130]
       jo        near ptr M14_L77
       test      r12d,r12d
       jne       near ptr M14_L34
       mov       rcx,offset MT_System.Collections.Generic.List<System.String>
       call      CORINFO_HELP_NEWSFAST
       mov       r12,rax
       mov       rcx,1F78D800498
       mov       rdx,[rcx]
       lea       rcx,[r12+8]
       call      CORINFO_HELP_ASSIGN_REF
M14_L20:
       mov       r8d,[rsp+128]
       test      r8d,r8d
       jne       near ptr M14_L38
M14_L21:
       mov       rdx,r12
M14_L22:
       cmp       dword ptr [rdx+10],1
       jg        near ptr M14_L67
       lea       r8,[rsp+0A8]
       mov       rcx,7FFA3EB8F600
       call      qword ptr [7FFA3E9952F0]; System.Linq.Enumerable.TryGetFirst[[System.__Canon, System.Private.CoreLib]](System.Collections.Generic.IEnumerable`1<System.__Canon>, Boolean ByRef)
       mov       r13,rax
       test      r13,r13
       je        near ptr M14_L66
M14_L23:
       test      r13,r13
       je        near ptr M14_L70
       mov       rdx,[r13+10]
       mov       rcx,7FFA3EB89030
       call      qword ptr [7FFA3EB45230]; System.Linq.Enumerable.Any[[System.__Canon, System.Private.CoreLib]](System.Collections.Generic.IEnumerable`1<System.__Canon>)
       test      eax,eax
       sete      al
       movzx     eax,al
M14_L24:
       xor       ecx,ecx
       mov       [rsp+278],rcx
       test      eax,eax
       je        near ptr M14_L40
       mov       rdx,[r13+8]
       mov       r8,rdx
       test      r8,r8
       je        short M14_L25
       mov       rcx,offset MT_Stateless.StateMachine<Benchmark.HsmState, Benchmark.HsmTrigger>+InternalTriggerBehaviour+Sync
       cmp       [r8],rcx
       jne       near ptr M14_L71
       xor       r8d,r8d
M14_L25:
       test      r8,r8
       jne       near ptr M14_L73
       mov       rdx,[r13+8]
       mov       rdi,rdx
       test      rdi,rdi
       je        short M14_L26
       mov       rcx,offset MT_Stateless.StateMachine<Benchmark.HsmState, Benchmark.HsmTrigger>+InternalTriggerBehaviour+Sync
       cmp       [rdi],rcx
       jne       near ptr M14_L72
M14_L26:
       test      rdi,rdi
       je        near ptr M14_L76
       mov       r10,[rdi+18]
       mov       rcx,offset Stateless.StateMachine`2+StateConfiguration+<>c__DisplayClass59_0[[Benchmark.HsmState, Benchmark],[Benchmark.HsmTrigger, Benchmark]].<InternalTransitionIf>b__0(Transition<Benchmark.HsmState,Benchmark.HsmTrigger>, System.Object[])
       cmp       [r10+18],rcx
       jne       near ptr M14_L75
       mov       rcx,[r10+8]
       mov       r10,[rcx+8]
       mov       rcx,offset Benchmark.StatelessHsm+<>c.<.ctor>b__1_0(Transition<Benchmark.HsmState,Benchmark.HsmTrigger>)
       cmp       [r10+18],rcx
       jne       near ptr M14_L74
M14_L27:
       add       rsp,288
       pop       rbx
       pop       rbp
       pop       rsi
       pop       rdi
       pop       r12
       pop       r13
       pop       r14
       pop       r15
       ret
M14_L28:
       cmp       dword ptr [r12+8],0
       je        near ptr M14_L52
       mov       rcx,offset MT_System.Linq.Enumerable+ArrayWhereIterator<Stateless.StateMachine<Benchmark.HsmState, Benchmark.HsmTrigger>+TriggerBehaviourResult>
       call      CORINFO_HELP_NEWSFAST
       mov       [rsp+48],rax
       call      CORINFO_HELP_GETCURRENTMANAGEDTHREADID
       mov       r8,[rsp+48]
       mov       [r8+10],eax
       lea       rcx,[r8+18]
       mov       rdx,r12
       call      CORINFO_HELP_ASSIGN_REF
       mov       r12,[rsp+48]
       lea       rcx,[r12+20]
       mov       rdx,r13
       call      CORINFO_HELP_ASSIGN_REF
       jmp       near ptr M14_L17
M14_L29:
       test      rdx,rdx
       je        short M14_L32
       mov       rcx,offset Stateless.StateMachine`2+TransitionGuard+<>c__DisplayClass17_0[[Benchmark.HsmState, Benchmark],[Benchmark.HsmTrigger, Benchmark]].<UnmetGuardConditions>b__0(GuardCondition<Benchmark.HsmState,Benchmark.HsmTrigger>)
       cmp       [rdx+18],rcx
       jne       short M14_L32
M14_L30:
       cmp       eax,r13d
       jae       near ptr M14_L78
       mov       ecx,eax
       mov       r10,[r12+rcx*8]
       mov       rcx,[rdx+8]
       mov       r9,[r10+10]
       mov       r11,[rcx+8]
       mov       rcx,offset Stateless.StateMachine`2+StateConfiguration+<>c[[Benchmark.HsmState, Benchmark],[Benchmark.HsmTrigger, Benchmark]].<InternalTransition>b__58_0(System.Object[])
       cmp       [r9+18],rcx
       mov       [rsp+40],r10
       jne       near ptr M14_L54
M14_L31:
       inc       eax
       cmp       eax,r13d
       jl        short M14_L30
       jmp       near ptr M14_L19
M14_L32:
       cmp       eax,r13d
       jae       near ptr M14_L78
       mov       r10d,eax
       mov       r10,[r12+r10*8]
       mov       r9,offset Stateless.StateMachine`2+TransitionGuard+<>c__DisplayClass17_0[[Benchmark.HsmState, Benchmark],[Benchmark.HsmTrigger, Benchmark]].<UnmetGuardConditions>b__0(GuardCondition<Benchmark.HsmState,Benchmark.HsmTrigger>)
       cmp       [rdx+18],r9
       jne       near ptr M14_L58
       mov       rcx,[rdx+8]
       mov       [rsp+40],r10
       mov       r9,[r10+10]
       mov       r11,[rcx+8]
       mov       rcx,offset Stateless.StateMachine`2+StateConfiguration+<>c[[Benchmark.HsmState, Benchmark],[Benchmark.HsmTrigger, Benchmark]].<InternalTransition>b__58_0(System.Object[])
       cmp       [r9+18],rcx
       jne       near ptr M14_L57
M14_L33:
       inc       eax
       cmp       eax,r13d
       jl        short M14_L32
       jmp       near ptr M14_L19
M14_L34:
       mov       rcx,offset MT_System.Collections.Generic.List<System.String>
       call      CORINFO_HELP_NEWSFAST
       mov       r13,rax
       mov       rcx,r13
       mov       edx,r12d
       call      qword ptr [7FFA3EB45E78]; System.Collections.Generic.List`1[[System.__Canon, System.Private.CoreLib]]..ctor(Int32)
       mov       r8d,r12d
       mov       rdx,r13
       mov       rcx,7FFA3EC97FD8
       call      qword ptr [7FFA3EB45E90]; System.Runtime.InteropServices.CollectionsMarshal.SetCount[[System.__Canon, System.Private.CoreLib]](System.Collections.Generic.List`1<System.__Canon>, Int32)
       mov       ecx,[r13+10]
       mov       rdx,[r13+8]
       cmp       [rdx+8],ecx
       jb        near ptr M14_L64
       add       rdx,10
       mov       [rsp+100],rdx
       mov       [rsp+108],ecx
       mov       r12d,[rsp+128]
       test      r12d,r12d
       jne       short M14_L36
M14_L35:
       mov       ecx,[rsp+130]
       mov       [rsp+20],ecx
       lea       rcx,[rsp+220]
       lea       rdx,[rsp+110]
       mov       r8,offset MT_System.Span<System.String>
       xor       r9d,r9d
       call      qword ptr [7FFA3EB45ED8]; System.Span`1[[System.__Canon, System.Private.CoreLib]].Slice(Int32, Int32)
       vmovdqu   xmm0,xmmword ptr [rsp+100]
       vmovdqu   xmmword ptr [rsp+88],xmm0
       lea       r8,[rsp+88]
       lea       rcx,[rsp+110]
       mov       rdx,offset MT_System.Span<System.String>
       call      qword ptr [7FFA3EB45EF0]; System.Span`1[[System.__Canon, System.Private.CoreLib]].CopyTo(System.Span`1<System.__Canon>)
       mov       r12,r13
       jmp       near ptr M14_L20
M14_L36:
       vmovdqu   xmm0,xmmword ptr [rsp+210]
       vmovdqu   xmmword ptr [rsp+88],xmm0
       lea       r8,[rsp+88]
       lea       rcx,[rsp+0F0]
       mov       rdx,offset MT_System.Span<System.String>
       call      qword ptr [7FFA3EB457D0]; System.Span`1[[System.__Canon, System.Private.CoreLib]].op_Implicit(System.Span`1<System.__Canon>)
       mov       rcx,[rsp+100]
       mov       r8d,[rsp+108]
       mov       eax,[rsp+0F8]
       mov       [rsp+0A0],eax
       cmp       eax,r8d
       ja        near ptr M14_L63
       mov       r10d,eax
       shl       r10,3
       mov       [rsp+80],r10
       mov       r8,r10
       mov       rdx,[rsp+0F0]
       call      qword ptr [7FFA3E785740]
       mov       ecx,[rsp+108]
       mov       edx,[rsp+0A0]
       cmp       edx,ecx
       ja        near ptr M14_L62
       mov       r8,[rsp+80]
       add       r8,[rsp+100]
       sub       ecx,edx
       mov       [rsp+100],r8
       mov       [rsp+108],ecx
       dec       r12d
       je        near ptr M14_L35
       lea       rcx,[rsp+138]
       mov       [rsp+0E0],rcx
       mov       dword ptr [rsp+0E8],1B
       mov       [rsp+20],r12d
       lea       rcx,[rsp+0E0]
       lea       rdx,[rsp+0D0]
       mov       r8,offset MT_System.ReadOnlySpan<System.String[]>
       xor       r9d,r9d
       call      qword ptr [7FFA3EC34C00]
       xor       r12d,r12d
       mov       eax,[rsp+0D8]
       mov       [rsp+9C],eax
       test      eax,eax
       jle       near ptr M14_L35
M14_L37:
       cmp       r12d,eax
       jae       near ptr M14_L78
       mov       rcx,[rsp+0D0]
       mov       r8,[rcx+r12*8]
       vxorps    xmm0,xmm0,xmm0
       vmovdqu   xmmword ptr [rsp+0B0],xmm0
       lea       rcx,[rsp+0B0]
       mov       r10,offset MT_System.ReadOnlySpan<System.String>
       mov       [rsp+70],r10
       mov       rdx,r10
       call      qword ptr [7FFA3EC3D728]
       vmovdqu   xmm0,xmmword ptr [rsp+0B0]
       vmovdqu   xmmword ptr [rsp+0C0],xmm0
       vmovdqu   xmm0,xmmword ptr [rsp+100]
       vmovdqu   xmmword ptr [rsp+88],xmm0
       lea       r8,[rsp+88]
       lea       rcx,[rsp+0C0]
       mov       rdx,[rsp+70]
       call      qword ptr [7FFA3EC3D740]
       lea       rcx,[rsp+100]
       lea       rdx,[rsp+100]
       mov       r9d,[rsp+0C8]
       mov       r8,offset MT_System.Span<System.String>
       call      qword ptr [7FFA3EC3D758]
       inc       r12d
       mov       ecx,[rsp+9C]
       cmp       r12d,ecx
       mov       eax,ecx
       jl        near ptr M14_L37
       jmp       near ptr M14_L35
M14_L38:
       lea       rcx,[rsp+128]
       mov       rdx,offset MT_System.Collections.Generic.SegmentedArrayBuilder<System.String>
       call      qword ptr [7FFA3EC3D080]
       jmp       near ptr M14_L21
M14_L39:
       mov       rcx,rax
       mov       rax,[rax]
       mov       rax,[rax+40]
       call      qword ptr [rax+28]
       mov       rdx,rax
       jmp       near ptr M14_L22
M14_L40:
       mov       rbp,[rbp+30]
       test      rbp,rbp
       jne       near ptr M14_L01
       jmp       near ptr M14_L26
M14_L41:
       mov       r12d,[r12+0C]
       inc       eax
       cmp       edx,eax
       jae       near ptr M14_L02
       jmp       near ptr M14_L64
M14_L42:
       mov       rcx,r12
       mov       r11,7FFA3E6E06B0
       call      qword ptr [r11]
       mov       rdx,[r13+8]
       mov       ecx,eax
       imul      rcx,[r13+30]
       shr       rcx,20
       inc       rcx
       mov       r8d,[rdx+8]
       imul      rcx,r8
       shr       rcx,20
       cmp       ecx,[rdx+8]
       jae       near ptr M14_L78
       mov       ecx,ecx
       lea       rdx,[rdx+rcx*4+10]
       mov       edx,[rdx]
       mov       r13,[r13+10]
       xor       ecx,ecx
       dec       edx
       jmp       near ptr M14_L04
M14_L43:
       mov       [rsp+270],ecx
       mov       [rsp+7C],r10d
       mov       [rsp+60],r9
       mov       edx,[r9+10]
       mov       rcx,r12
       mov       r8d,r14d
       mov       r11,7FFA3E6E06B8
       call      qword ptr [r11]
       test      eax,eax
       mov       ecx,[rsp+270]
       mov       r10d,[rsp+7C]
       jne       short M14_L44
       mov       r9,[rsp+60]
       jmp       near ptr M14_L05
M14_L44:
       mov       r12,[rsp+60]
       jmp       near ptr M14_L03
M14_L45:
       lea       r8,[rsp+278]
       mov       rcx,r13
       mov       edx,r14d
       mov       r11,7FFA3E6E06A8
       call      qword ptr [r11]
       test      eax,eax
       jne       near ptr M14_L10
       jmp       near ptr M14_L08
M14_L46:
       mov       rcx,offset MT_System.Array+EmptyArray<Stateless.StateMachine<Benchmark.HsmState, Benchmark.HsmTrigger>+TriggerBehaviourResult>
       call      CORINFO_HELP_GET_GCSTATIC_BASE
       mov       rcx,1F78D8014A0
       mov       rax,[rcx]
       jmp       near ptr M14_L14
M14_L47:
       mov       rcx,r12
       mov       rax,[r12]
       mov       rax,[rax+40]
       call      qword ptr [rax+20]
       jmp       near ptr M14_L14
M14_L48:
       mov       rdx,r13
       mov       rcx,offset MT_System.Collections.Generic.ICollection<Stateless.StateMachine<Benchmark.HsmState, Benchmark.HsmTrigger>+TriggerBehaviourResult>
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfInterface(Void*, System.Object)
       test      rax,rax
       je        short M14_L49
       mov       rdx,rax
       mov       rcx,7FFA3EC97798
       call      qword ptr [7FFA3EAAD4A0]; System.Linq.Enumerable.ICollectionToArray[[System.__Canon, System.Private.CoreLib]](System.Collections.Generic.ICollection`1<System.__Canon>)
       mov       r15,rax
       jmp       near ptr M14_L15
M14_L49:
       mov       rdx,r13
       mov       rcx,7FFA3EC97820
       call      qword ptr [7FFA3EC3CAC8]
       mov       r15,rax
       jmp       near ptr M14_L15
M14_L50:
       mov       rcx,offset MT_System.Func<Stateless.StateMachine<Benchmark.HsmState, Benchmark.HsmTrigger>+TriggerBehaviourResult, System.Boolean>
       call      CORINFO_HELP_NEWSFAST
       mov       r13,rax
       mov       rcx,1F78D800A40
       mov       rdx,[rcx]
       lea       rcx,[r13+8]
       call      CORINFO_HELP_ASSIGN_REF
       mov       rcx,offset Stateless.StateMachine`2+StateRepresentation+<>c[[Benchmark.HsmState, Benchmark],[Benchmark.HsmTrigger, Benchmark]].<TryFindLocalHandlerResult>b__42_0(TriggerBehaviourResult<Benchmark.HsmState,Benchmark.HsmTrigger>)
       mov       [r13+18],rcx
       mov       rcx,1F78D800A48
       mov       rdx,r13
       call      CORINFO_HELP_ASSIGN_REF
       jmp       near ptr M14_L16
M14_L51:
       mov       rcx,offset MT_System.Linq.Enumerable+IEnumerableWhereIterator<Stateless.StateMachine<Benchmark.HsmState, Benchmark.HsmTrigger>+TriggerBehaviourResult>
       call      CORINFO_HELP_NEWSFAST
       mov       r12,rax
       mov       rcx,r12
       call      qword ptr [7FFA3EC3D7E8]
       lea       rcx,[r12+18]
       mov       rdx,r15
       call      CORINFO_HELP_ASSIGN_REF
       lea       rcx,[r12+20]
       mov       rdx,r13
       call      CORINFO_HELP_ASSIGN_REF
       jmp       near ptr M14_L17
M14_L52:
       mov       rcx,offset MT_System.Array+EmptyArray<Stateless.StateMachine<Benchmark.HsmState, Benchmark.HsmTrigger>+TriggerBehaviourResult>
       call      CORINFO_HELP_GET_GCSTATIC_BASE
       mov       rcx,1F78D8014A0
       mov       r12,[rcx]
       jmp       near ptr M14_L17
M14_L53:
       mov       rcx,rax
       mov       rdx,r13
       mov       rax,[rax]
       mov       rax,[rax+50]
       call      qword ptr [rax+8]
       mov       r12,rax
       jmp       near ptr M14_L17
M14_L54:
       mov       [rsp+124],eax
       mov       rdx,r11
       mov       rcx,[r9+8]
       call      qword ptr [r9+18]
       test      eax,eax
       mov       r8,[rsp+30]
       jne       short M14_L55
       mov       r10,[rsp+40]
       mov       rdx,r10
       mov       rcx,[r8+8]
       call      qword ptr [r8+18]
       mov       rdx,rax
       mov       rax,[rsp+220]
       mov       r8d,[rsp+228]
       mov       r10d,[rsp+130]
       cmp       r10d,r8d
       jae       short M14_L56
       cmp       r10d,r8d
       jae       near ptr M14_L78
       mov       ecx,r10d
       lea       rcx,[rax+rcx*8]
       call      CORINFO_HELP_CHECKED_ASSIGN_REF
       mov       ecx,[rsp+130]
       inc       ecx
       mov       [rsp+130],ecx
       mov       eax,[rsp+124]
       mov       rdx,[rsp+38]
       mov       r8,[rsp+30]
       jmp       near ptr M14_L31
M14_L55:
       mov       eax,[rsp+124]
       mov       rdx,[rsp+38]
       jmp       near ptr M14_L31
M14_L56:
       lea       rcx,[rsp+128]
       mov       r8,rdx
       mov       rdx,offset MT_System.Collections.Generic.SegmentedArrayBuilder<System.String>
       call      qword ptr [7FFA3EC3D068]
       mov       eax,[rsp+124]
       mov       rdx,[rsp+38]
       mov       r8,[rsp+30]
       jmp       near ptr M14_L31
M14_L57:
       mov       [rsp+124],eax
       mov       rdx,r11
       mov       rcx,[r9+8]
       call      qword ptr [r9+18]
       test      eax,eax
       je        short M14_L59
       mov       eax,[rsp+124]
       mov       rdx,[rsp+38]
       mov       r8,[rsp+30]
       jmp       near ptr M14_L33
M14_L58:
       mov       [rsp+124],eax
       mov       [rsp+40],r10
       mov       rdx,r10
       mov       r9,[rsp+38]
       mov       rcx,[r9+8]
       call      qword ptr [r9+18]
       test      eax,eax
       je        short M14_L60
M14_L59:
       mov       rdx,[rsp+40]
       mov       rax,[rsp+30]
       mov       rcx,[rax+8]
       call      qword ptr [rax+18]
       mov       rdx,rax
       mov       rax,[rsp+220]
       mov       r8d,[rsp+228]
       mov       r10d,[rsp+130]
       cmp       r10d,r8d
       jae       short M14_L61
       cmp       r10d,r8d
       jae       near ptr M14_L78
       mov       ecx,r10d
       lea       rcx,[rax+rcx*8]
       call      CORINFO_HELP_CHECKED_ASSIGN_REF
       mov       ecx,[rsp+130]
       inc       ecx
       mov       [rsp+130],ecx
       mov       eax,[rsp+124]
       mov       rdx,[rsp+38]
       mov       r8,[rsp+30]
       jmp       near ptr M14_L33
M14_L60:
       mov       eax,[rsp+124]
       mov       rdx,[rsp+38]
       mov       r8,[rsp+30]
       jmp       near ptr M14_L33
M14_L61:
       lea       rcx,[rsp+128]
       mov       r8,rdx
       mov       rdx,offset MT_System.Collections.Generic.SegmentedArrayBuilder<System.String>
       call      qword ptr [7FFA3EC3D068]
       mov       eax,[rsp+124]
       mov       rdx,[rsp+38]
       mov       r8,[rsp+30]
       jmp       near ptr M14_L33
M14_L62:
       call      qword ptr [7FFA3E997798]
       int       3
M14_L63:
       call      qword ptr [7FFA3EC3CA68]
       int       3
M14_L64:
       call      qword ptr [7FFA3E78F2A0]
       int       3
M14_L65:
       mov       rcx,offset MT_System.Collections.Generic.List<Stateless.StateMachine<Benchmark.HsmState, Benchmark.HsmTrigger>+TriggerBehaviourResult>
       call      CORINFO_HELP_NEWSFAST
       mov       r13,rax
       mov       rcx,r13
       mov       rdx,r12
       call      qword ptr [7FFA3EB44D20]; System.Collections.Generic.List`1[[System.__Canon, System.Private.CoreLib]]..ctor(System.Collections.Generic.IEnumerable`1<System.__Canon>)
       mov       rdx,r13
       jmp       near ptr M14_L22
M14_L66:
       mov       rcx,r15
       call      qword ptr [7FFA3EB45668]
       mov       r13,rax
       jmp       near ptr M14_L23
M14_L67:
       mov       rcx,offset MT_Benchmark.HsmTrigger
       call      CORINFO_HELP_NEWSFAST
       mov       rdi,rax
       call      qword ptr [7FFA3EB45DB8]
       mov       rbx,rax
       mov       [rdi+8],r14d
       mov       rcx,offset MT_Benchmark.HsmState
       call      CORINFO_HELP_NEWSFAST
       mov       rsi,rax
       mov       ecx,[rbp+40]
       mov       [rsi+8],ecx
       mov       rcx,offset MT_System.InvalidOperationException
       call      CORINFO_HELP_NEWSFAST
       mov       rbp,rax
       mov       r8,rsi
       mov       rdx,rdi
       mov       rcx,rbx
       call      qword ptr [7FFA3EB45DD0]
       mov       rdx,rax
       mov       rcx,rbp
       call      qword ptr [7FFA3EAA6C70]
       mov       rcx,rbp
       call      CORINFO_HELP_THROW
       int       3
M14_L68:
       mov       ecx,0C
       call      qword ptr [7FFA3E78F738]
       int       3
M14_L69:
       mov       ecx,11
       call      qword ptr [7FFA3E78F738]
       int       3
M14_L70:
       xor       eax,eax
       jmp       near ptr M14_L24
M14_L71:
       mov       rcx,offset MT_Stateless.StateMachine<Benchmark.HsmState, Benchmark.HsmTrigger>+InternalTriggerBehaviour+Async
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       r8,rax
       jmp       near ptr M14_L25
M14_L72:
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rdi,rax
       jmp       near ptr M14_L26
M14_L73:
       mov       rcx,offset MT_System.InvalidOperationException
       call      CORINFO_HELP_NEWSFAST
       mov       rbx,rax
       mov       ecx,0EA9
       mov       rdx,7FFA3EB26780
       call      CORINFO_HELP_STRCNS
       mov       rdx,rax
       mov       rcx,rbx
       call      qword ptr [7FFA3EAA6C70]
       mov       rcx,rbx
       call      CORINFO_HELP_THROW
       int       3
M14_L74:
       mov       rdx,rbx
       mov       rcx,[r10+8]
       call      qword ptr [r10+18]
       jmp       near ptr M14_L27
M14_L75:
       mov       rdx,rbx
       mov       r8,rsi
       mov       rcx,[r10+8]
       call      qword ptr [r10+18]
       jmp       near ptr M14_L27
M14_L76:
       mov       rcx,offset MT_System.ArgumentNullException
       call      CORINFO_HELP_NEWSFAST
       mov       rbx,rax
       mov       ecx,0F2E
       mov       rdx,7FFA3EB26780
       call      CORINFO_HELP_STRCNS
       mov       rdx,rax
       mov       rcx,rbx
       call      qword ptr [7FFA3EAA64D8]
       mov       rcx,rbx
       call      CORINFO_HELP_THROW
       int       3
M14_L77:
       call      CORINFO_HELP_OVERFLOW
       int       3
M14_L78:
       call      CORINFO_HELP_RNGCHKFAIL
       int       3
; Total bytes of code 3929
```
```assembly
; Stateless.StateMachine`2+StateRepresentation[[Benchmark.HsmState, Benchmark],[Benchmark.HsmTrigger, Benchmark]]..ctor(Benchmark.HsmState, Boolean)
       push      rbp
       sub       rsp,50
       lea       rbp,[rsp+50]
       vxorps    xmm4,xmm4,xmm4
       vmovdqu   ymmword ptr [rbp-30],ymm4
       vmovdqa   xmmword ptr [rbp-10],xmm4
       mov       [rbp+10],rcx
       mov       [rbp+18],edx
       mov       [rbp+20],r8d
       mov       rcx,offset MT_System.Collections.Generic.Dictionary<Benchmark.HsmTrigger, System.Collections.Generic.ICollection<Stateless.StateMachine<Benchmark.HsmState, Benchmark.HsmTrigger>+TriggerBehaviour>>
       call      CORINFO_HELP_NEWSFAST
       mov       [rbp-8],rax
       mov       rcx,[rbp-8]
       call      qword ptr [7FFA3EB443D8]; System.Collections.Generic.Dictionary`2[[Benchmark.HsmTrigger, Benchmark],[System.__Canon, System.Private.CoreLib]]..ctor()
       mov       rax,[rbp+10]
       lea       rcx,[rax+8]
       mov       rdx,[rbp-8]
       call      CORINFO_HELP_ASSIGN_REF
       mov       rcx,offset MT_System.Collections.Generic.List<Stateless.StateMachine<Benchmark.HsmState, Benchmark.HsmTrigger>+EntryActionBehavior>
       call      CORINFO_HELP_NEWSFAST
       mov       [rbp-10],rax
       mov       rcx,[rbp-10]
       call      qword ptr [7FFA3EAA73F0]; System.Collections.Generic.List`1[[System.__Canon, System.Private.CoreLib]]..ctor()
       mov       rax,[rbp+10]
       lea       rcx,[rax+10]
       mov       rdx,[rbp-10]
       call      CORINFO_HELP_ASSIGN_REF
       mov       rcx,offset MT_System.Collections.Generic.List<Stateless.StateMachine<Benchmark.HsmState, Benchmark.HsmTrigger>+ExitActionBehavior>
       call      CORINFO_HELP_NEWSFAST
       mov       [rbp-18],rax
       mov       rcx,[rbp-18]
       call      qword ptr [7FFA3EAA73F0]; System.Collections.Generic.List`1[[System.__Canon, System.Private.CoreLib]]..ctor()
       mov       rax,[rbp+10]
       lea       rcx,[rax+18]
       mov       rdx,[rbp-18]
       call      CORINFO_HELP_ASSIGN_REF
       mov       rcx,offset MT_System.Collections.Generic.List<Stateless.StateMachine<Benchmark.HsmState, Benchmark.HsmTrigger>+ActivateActionBehaviour>
       call      CORINFO_HELP_NEWSFAST
       mov       [rbp-20],rax
       mov       rcx,[rbp-20]
       call      qword ptr [7FFA3EAA73F0]; System.Collections.Generic.List`1[[System.__Canon, System.Private.CoreLib]]..ctor()
       mov       rax,[rbp+10]
       lea       rcx,[rax+20]
       mov       rdx,[rbp-20]
       call      CORINFO_HELP_ASSIGN_REF
       mov       rcx,offset MT_System.Collections.Generic.List<Stateless.StateMachine<Benchmark.HsmState, Benchmark.HsmTrigger>+DeactivateActionBehaviour>
       call      CORINFO_HELP_NEWSFAST
       mov       [rbp-28],rax
       mov       rcx,[rbp-28]
       call      qword ptr [7FFA3EAA73F0]; System.Collections.Generic.List`1[[System.__Canon, System.Private.CoreLib]]..ctor()
       mov       rax,[rbp+10]
       lea       rcx,[rax+28]
       mov       rdx,[rbp-28]
       call      CORINFO_HELP_ASSIGN_REF
       mov       rcx,offset MT_System.Collections.Generic.List<Stateless.StateMachine<Benchmark.HsmState, Benchmark.HsmTrigger>+StateRepresentation>
       call      CORINFO_HELP_NEWSFAST
       mov       [rbp-30],rax
       mov       rcx,[rbp-30]
       call      qword ptr [7FFA3EAA73F0]; System.Collections.Generic.List`1[[System.__Canon, System.Private.CoreLib]]..ctor()
       mov       rax,[rbp+10]
       lea       rcx,[rax+38]
       mov       rdx,[rbp-30]
       call      CORINFO_HELP_ASSIGN_REF
       mov       rcx,[rbp+10]
       call      qword ptr [7FFA3E78C8A0]; System.Object..ctor()
       mov       rax,[rbp+10]
       mov       ecx,[rbp+18]
       mov       [rax+40],ecx
       mov       rax,[rbp+10]
       mov       ecx,[rbp+20]
       mov       [rax+48],cl
       add       rsp,50
       pop       rbp
       ret
; Total bytes of code 347
```
```assembly
; System.Runtime.CompilerServices.CastHelpers.IsInstanceOfInterface(Void*, System.Object)
       test      rdx,rdx
       je        short M16_L01
       mov       r8,[rdx]
       movzx     r10d,word ptr [r8+0E]
       test      r10,r10
       je        short M16_L03
       mov       r9,[r8+38]
       cmp       r10,4
       jl        short M16_L04
M16_L00:
       cmp       [r9],rcx
       je        short M16_L01
       cmp       [r9+8],rcx
       jne       short M16_L02
M16_L01:
       mov       rax,rdx
       ret
M16_L02:
       cmp       [r9+10],rcx
       je        short M16_L01
       cmp       [r9+18],rcx
       je        short M16_L01
       add       r9,20
       add       r10,0FFFFFFFFFFFFFFFC
       cmp       r10,4
       jge       short M16_L00
       test      r10,r10
       jne       short M16_L04
M16_L03:
       test      dword ptr [r8],504C0000
       jne       short M16_L05
       xor       edx,edx
       jmp       short M16_L01
M16_L04:
       cmp       [r9],rcx
       je        short M16_L01
       add       r9,8
       dec       r10
       test      r10,r10
       jg        short M16_L04
       jmp       short M16_L03
M16_L05:
       jmp       qword ptr [7FFA3EAAEE68]; System.Runtime.CompilerServices.CastHelpers.IsInstance_Helper(Void*, System.Object)
; Total bytes of code 112
```
```assembly
; System.Linq.Enumerable.ICollectionToArray[[System.__Canon, System.Private.CoreLib]](System.Collections.Generic.ICollection`1<System.__Canon>)
       push      rdi
       push      rsi
       push      rbx
       sub       rsp,30
       mov       [rsp+28],rcx
       mov       rbx,rcx
       mov       rsi,rdx
       mov       rcx,rbx
       call      qword ptr [7FFAE4A9F030]
       mov       rcx,rsi
       mov       r11,rax
       call      qword ptr [rax]
       mov       edi,eax
       test      edi,edi
       je        short M17_L00
       mov       rcx,rbx
       call      qword ptr [7FFAE4A9E510]
       mov       rcx,rax
       movsxd    rdx,edi
       call      qword ptr [7FFAE4A9C678]; CORINFO_HELP_NEWARR_1_DIRECT
       mov       rdi,rax
       mov       rcx,rbx
       call      qword ptr [7FFAE4A9F038]
       mov       rcx,rsi
       mov       r11,rax
       mov       rdx,rdi
       xor       r8d,r8d
       call      qword ptr [rax]
       mov       rax,rdi
       add       rsp,30
       pop       rbx
       pop       rsi
       pop       rdi
       ret
M17_L00:
       mov       rcx,rbx
       call      qword ptr [7FFAE4A9EC78]
       mov       rcx,rax
       lea       rax,[System.Linq.Enumerable.Select[[System.__Canon, System.Private.CoreLib],[System.__Canon, System.Private.CoreLib]](System.Collections.Generic.IEnumerable`1<System.__Canon>, System.Func`2<System.__Canon,System.__Canon>)]
       add       rsp,30
       pop       rbx
       pop       rsi
       pop       rdi
       jmp       qword ptr [rax]
; Total bytes of code 128
```
```assembly
; Stateless.StateMachine`2+Transition[[Benchmark.HsmState, Benchmark],[Benchmark.HsmTrigger, Benchmark]]..ctor(Benchmark.HsmState, Benchmark.HsmState, Benchmark.HsmTrigger, System.Object[])
       push      rbx
       sub       rsp,20
       mov       rbx,rcx
       mov       [rbx+10],edx
       mov       [rbx+14],r8d
       mov       [rbx+18],r9d
       mov       rdx,[rsp+50]
       test      rdx,rdx
       je        short M18_L01
M18_L00:
       lea       rcx,[rbx+8]
       call      CORINFO_HELP_ASSIGN_REF
       nop
       add       rsp,20
       pop       rbx
       ret
M18_L01:
       mov       rcx,offset MT_System.Object[]
       xor       edx,edx
       call      CORINFO_HELP_NEWARR_1_OBJ
       mov       rdx,rax
       jmp       short M18_L00
; Total bytes of code 67
```
```assembly
; Stateless.StateMachine`2[[Benchmark.HsmState, Benchmark],[Benchmark.HsmTrigger, Benchmark]].HandleTransitioningTrigger(System.Object[], StateRepresentation<Benchmark.HsmState,Benchmark.HsmTrigger>, Transition<Benchmark.HsmState,Benchmark.HsmTrigger>)
       push      rbp
       sub       rsp,90
       lea       rbp,[rsp+90]
       xor       eax,eax
       mov       [rbp-68],rax
       vxorps    xmm4,xmm4,xmm4
       vmovdqu32 [rbp-60],zmm4
       vmovdqa   xmmword ptr [rbp-20],xmm4
       vmovdqa   xmmword ptr [rbp-10],xmm4
       mov       [rbp+10],rcx
       mov       [rbp+18],rdx
       mov       [rbp+20],r8
       mov       [rbp+28],r9
       mov       rcx,[rbp+20]
       mov       rdx,[rbp+28]
       cmp       [rcx],ecx
       call      qword ptr [7FFA3EB45F80]; Stateless.StateMachine`2+StateRepresentation[[Benchmark.HsmState, Benchmark],[Benchmark.HsmTrigger, Benchmark]].Exit(Transition<Benchmark.HsmState,Benchmark.HsmTrigger>)
       mov       [rbp+28],rax
       mov       rcx,[rbp+28]
       cmp       [rcx],ecx
       call      qword ptr [7FFA3EB45F98]; Stateless.StateMachine`2+Transition[[Benchmark.HsmState, Benchmark],[Benchmark.HsmTrigger, Benchmark]].get_Destination()
       mov       [rbp-4C],eax
       mov       edx,[rbp-4C]
       mov       rcx,[rbp+10]
       call      qword ptr [7FFA3EB45FB0]; Stateless.StateMachine`2[[Benchmark.HsmState, Benchmark],[Benchmark.HsmTrigger, Benchmark]].set_State(Benchmark.HsmState)
       mov       rcx,[rbp+28]
       cmp       [rcx],ecx
       call      qword ptr [7FFA3EB45F98]; Stateless.StateMachine`2+Transition[[Benchmark.HsmState, Benchmark],[Benchmark.HsmTrigger, Benchmark]].get_Destination()
       mov       [rbp-50],eax
       mov       edx,[rbp-50]
       mov       rcx,[rbp+10]
       call      qword ptr [7FFA3EB444F8]; Stateless.StateMachine`2[[Benchmark.HsmState, Benchmark],[Benchmark.HsmTrigger, Benchmark]].GetRepresentation(Benchmark.HsmState)
       mov       [rbp-8],rax
       mov       rax,[rbp+10]
       mov       rcx,[rax+30]
       mov       rdx,[rbp+28]
       cmp       [rcx],ecx
       call      qword ptr [7FFA3EB45FC8]; Stateless.StateMachine`2+OnTransitionedEvent[[Benchmark.HsmState, Benchmark],[Benchmark.HsmTrigger, Benchmark]].Invoke(Transition<Benchmark.HsmState,Benchmark.HsmTrigger>)
       mov       rcx,[rbp+10]
       mov       rdx,[rbp-8]
       mov       r8,[rbp+28]
       mov       r9,[rbp+18]
       call      qword ptr [7FFA3EB45FE0]; Stateless.StateMachine`2[[Benchmark.HsmState, Benchmark],[Benchmark.HsmTrigger, Benchmark]].EnterState(StateRepresentation<Benchmark.HsmState,Benchmark.HsmTrigger>, Transition<Benchmark.HsmState,Benchmark.HsmTrigger>, System.Object[])
       mov       [rbp-10],rax
       mov       rcx,[rbp-10]
       cmp       [rcx],ecx
       call      qword ptr [7FFA3EB447E0]; Stateless.StateMachine`2+StateRepresentation[[Benchmark.HsmState, Benchmark],[Benchmark.HsmTrigger, Benchmark]].get_UnderlyingState()
       mov       [rbp-14],eax
       mov       rcx,offset MT_Benchmark.HsmState
       call      CORINFO_HELP_NEWSFAST
       mov       [rbp-20],rax
       mov       rcx,[rbp+10]
       call      qword ptr [7FFA3EAAFC30]; Stateless.StateMachine`2[[Benchmark.HsmState, Benchmark],[Benchmark.HsmTrigger, Benchmark]].get_State()
       mov       rcx,[rbp-20]
       mov       [rcx+8],eax
       mov       rcx,offset MT_Benchmark.HsmState
       call      CORINFO_HELP_NEWSFAST
       mov       [rbp-28],rax
       mov       rax,[rbp-28]
       mov       ecx,[rbp-14]
       mov       [rax+8],ecx
       mov       rax,[rbp-28]
       mov       [rbp-58],rax
       mov       rcx,[rbp-58]
       mov       rdx,[rbp-20]
       mov       rax,[rbp-58]
       mov       rax,[rax]
       mov       rax,[rax+40]
       call      qword ptr [rax+10]
       test      eax,eax
       jne       short M19_L00
       mov       rcx,[rbp-10]
       cmp       [rcx],ecx
       call      qword ptr [7FFA3EB447E0]; Stateless.StateMachine`2+StateRepresentation[[Benchmark.HsmState, Benchmark],[Benchmark.HsmTrigger, Benchmark]].get_UnderlyingState()
       mov       [rbp-5C],eax
       mov       edx,[rbp-5C]
       mov       rcx,[rbp+10]
       call      qword ptr [7FFA3EB45FB0]; Stateless.StateMachine`2[[Benchmark.HsmState, Benchmark],[Benchmark.HsmTrigger, Benchmark]].set_State(Benchmark.HsmState)
M19_L00:
       mov       rax,[rbp+10]
       mov       rax,[rax+38]
       mov       [rbp-30],rax
       mov       rcx,[rbp+28]
       cmp       [rcx],ecx
       call      qword ptr [7FFA3EB45FF8]; Stateless.StateMachine`2+Transition[[Benchmark.HsmState, Benchmark],[Benchmark.HsmTrigger, Benchmark]].get_Source()
       mov       [rbp-34],eax
       mov       rcx,[rbp+10]
       call      qword ptr [7FFA3EAAFC30]; Stateless.StateMachine`2[[Benchmark.HsmState, Benchmark],[Benchmark.HsmTrigger, Benchmark]].get_State()
       mov       [rbp-38],eax
       mov       rcx,[rbp+28]
       cmp       [rcx],ecx
       call      qword ptr [7FFA3EB46010]; Stateless.StateMachine`2+Transition[[Benchmark.HsmState, Benchmark],[Benchmark.HsmTrigger, Benchmark]].get_Trigger()
       mov       [rbp-3C],eax
       mov       rcx,offset MT_Stateless.StateMachine<Benchmark.HsmState, Benchmark.HsmTrigger>+Transition
       call      CORINFO_HELP_NEWSFAST
       mov       [rbp-48],rax
       mov       rcx,[rbp+28]
       cmp       [rcx],ecx
       call      qword ptr [7FFA3EB46028]; Stateless.StateMachine`2+Transition[[Benchmark.HsmState, Benchmark],[Benchmark.HsmTrigger, Benchmark]].get_Parameters()
       mov       [rbp-68],rax
       mov       rax,[rbp-68]
       mov       [rsp+20],rax
       mov       edx,[rbp-34]
       mov       r8d,[rbp-38]
       mov       r9d,[rbp-3C]
       mov       rcx,[rbp-48]
       call      qword ptr [7FFA3EB454D0]; Stateless.StateMachine`2+Transition[[Benchmark.HsmState, Benchmark],[Benchmark.HsmTrigger, Benchmark]]..ctor(Benchmark.HsmState, Benchmark.HsmState, Benchmark.HsmTrigger, System.Object[])
       mov       rcx,[rbp-30]
       mov       rdx,[rbp-48]
       cmp       [rcx],ecx
       call      qword ptr [7FFA3EB45FC8]; Stateless.StateMachine`2+OnTransitionedEvent[[Benchmark.HsmState, Benchmark],[Benchmark.HsmTrigger, Benchmark]].Invoke(Transition<Benchmark.HsmState,Benchmark.HsmTrigger>)
       nop
       add       rsp,90
       pop       rbp
       ret
; Total bytes of code 476
```
```assembly
; System.MulticastDelegate.CtorClosed(System.Object, IntPtr)
       push      rsi
       push      rbx
       sub       rsp,28
       mov       rbx,rcx
       mov       rsi,r8
       test      rdx,rdx
       je        short M20_L00
       lea       rcx,[rbx+8]
       call      CORINFO_HELP_ASSIGN_REF
       mov       [rbx+18],rsi
       add       rsp,28
       pop       rbx
       pop       rsi
       ret
M20_L00:
       call      qword ptr [7FFA3EC376A8]
       int       3
; Total bytes of code 44
```

