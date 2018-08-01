namespace FsServiceFramework

open System
open Unity.Interception.InterceptionBehaviors
open Unity.Interception.PolicyInjection.Pipeline

type PerformanceMonitorInterceptionBehavior() = 
    interface IInterceptionBehavior with
        member this.Invoke (input:IMethodInvocation, getNext:GetNextInterceptionBehaviorDelegate) =
            let writeLog (message:string) =
                printfn "%s: %s" (DateTime.Now.ToLongTimeString()) message
            let enterTimestamp = DateTime.Now
            let result = getNext.Invoke().Invoke(input, getNext)
            let exitTimestamp = DateTime.Now
            match result.Exception with
            | null -> 
                writeLog (sprintf "Method %s returned (%s -> %s)" input.MethodBase.Name 
                    (enterTimestamp.ToLongTimeString()) (exitTimestamp.ToLongTimeString()))
            | ex -> 
                writeLog (sprintf "Method %s threw exception %s (%s -> %s)" input.MethodBase.Name result.Exception.Message
                    (enterTimestamp.ToLongTimeString()) (exitTimestamp.ToLongTimeString()))
            result
        member this.GetRequiredInterfaces() = 
            Type.EmptyTypes |> Array.toSeq
        member this.WillExecute = true

module Performance =
    ()



