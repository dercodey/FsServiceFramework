namespace FsServiceFramework

open System
open System.Data.Entity
open System.Net.Http
open System.Data.Entity

type IKeyService =
    abstract GetKey<'key, 'entity when 'key : comparison> : 'entity -> 'key

type IRepository<'key, 'entity 
        when 'key : comparison 
        and 'entity: not struct> =
    abstract Get : 'key -> 'entity
    abstract Create : 'entity -> 'entity
    abstract Delete : 'entity -> bool
    abstract Update : 'entity -> 'entity

type DbSetRepository<'key, 'entity 
        when 'key : comparison 
        and 'entity: not struct>
            (dbSet:DbSet<'entity>) =

    interface IRepository<'key, 'entity> with
        member this.Get (key:'key) = 
            dbSet.Find(key)
        member this.Create (entity:'entity) = 
            dbSet.Add(entity)
        member this.Delete (entity:'entity) = 
            dbSet.Remove(entity) |> ignore
            true
        member this.Update (entity:'entity) =
            entity

type DbCmd<'key, 'entity> =
| Get of 'key * AsyncReplyChannel<'entity>
| Create of 'entity * AsyncReplyChannel<bool>
| Delete of 'key * AsyncReplyChannel<bool>
| Update of 'entity * AsyncReplyChannel<bool>

type VolatileRepository<'key, 'entity 
        when 'key : comparison 
        and 'entity: not struct>
            (getKeyFunc:'entity->'key) =

    let mutable entityMap = Map.empty<'key, 'entity>


    let mailboxFunc (init:'state) = 
        new MailboxProcessor<('state->('reply*'state))*AsyncReplyChannel<'reply>> (fun mailbox -> 
            let rec loop (state:'state)= async { 
                let! (func, replyChannel) = mailbox.Receive()
                let (reply, next) = state |> func
                replyChannel.Reply(reply)
                return! next |> loop }
            loop init)
    do 
        let mb = mailboxFunc 0
        mb.Start()
        let createReply (replyChannel:AsyncReplyChannel<int>) =
            ((fun x -> (x + 2, x + 2)), replyChannel)
        mb.PostAndReply<int> createReply |> ignore


    let mailbox = 
        new MailboxProcessor<DbCmd<'key,'entity>> (fun mailbox -> 
            let rec loop (map:Map<'key,'entity>)= async { 
                let! cmd = mailbox.Receive()
                let updatedMap =
                    match cmd with
                    | Get (key,reply) -> 
                        reply.Reply(map.Item key)
                        map
                    | Create (entity,reply) -> 
                        let key = getKeyFunc entity in 
                            try map |> Map.add key entity
                            finally reply.Reply(true)               
                    | Delete (key,reply) ->                                             
                        try map |> Map.remove key
                        finally reply.Reply(true)
                    | Update (entity,reply) -> 
                        let key = getKeyFunc entity in 
                            try map
                                |> Map.remove key 
                                |> Map.add key entity
                            finally reply.Reply(true)
                return! loop map }
            loop Map.empty<'key,'entity>)
    do mailbox.Start()

    interface IRepository<'key, 'entity> with
        member this.Get (key:'key) = 
            (fun rc -> Get (key,rc)) |> mailbox.PostAndReply
        member this.Create (entity:'entity) = 
            (fun rc -> Create (entity,rc)) |> mailbox.PostAndReply
            entity
        member this.Delete (entity:'entity) =
            let key = getKeyFunc entity
            (fun rc -> Delete (key,rc)) |> mailbox.PostAndReply
        member this.Update (entity:'entity) =
            (fun rc -> Update (entity,rc)) |> mailbox.PostAndReply
            entity

type IUnitOfWork =
    abstract GetRepository<'key, 'entity 
        when 'key : comparison 
        and 'entity: not struct> : unit -> IRepository<'key, 'entity>
    abstract Save : unit -> int
    abstract Rollback : unit -> int

type VolatileUnitOfWork(keyService:IKeyService) =

    let repositoryMap = Map.empty<_, obj>

    interface IUnitOfWork with
        member this.GetRepository<'key, 'entity 
                when 'key : comparison 
                and 'entity: not struct> () =
            let keyForEntity (e:'entity) = keyService.GetKey(e)
            VolatileRepository<'key, 'entity>(keyForEntity) :> IRepository<'key, 'entity>
        member this.Save () = 0
        member this.Rollback () = 0

type OperationObject<'req, 'res> =
    abstract member Call : 'req->'res

module Utility = 

    open System
    open System.Reflection
    open System.Collections.Generic

    open Microsoft.FSharp.Reflection

    open Unity
    open Unity.Interception.PolicyInjection.Pipeline
    open Unity.Interception.InterceptionBehaviors

    let unityInterceptionBehavior 
            (wrapper:IMethodInvocation->(IMethodInvocation->IMethodReturn)->IMethodReturn) =
        { new IInterceptionBehavior with
            member this.Invoke(input:IMethodInvocation, getNext) = 
                let innerInvoke innerInput = 
                    getNext.Invoke().Invoke(innerInput, getNext)
                wrapper input innerInvoke
            member this.GetRequiredInterfaces() = Type.EmptyTypes |> Array.toSeq
            member this.WillExecute = true }

    // retrieves a custom attribute by template
    let getCustomAttribute<'attributeType when 'attributeType :> Attribute> (fromType:Type) =
        fromType.GetCustomAttributes() 
            |> Seq.filter (fun x -> x :? 'attributeType)  
            |> Seq.cast<'attributeType>
            |> Seq.head

    let cacheCreateOrGet<'keyType,'entryType> 
                (cache:Dictionary<Type,obj>) 
                (createFunc : unit -> 'entryType) =
        let keyType = typedefof<'keyType>
        match cache.TryGetValue(keyType) with
        | (true, entry) -> entry :?> 'entryType
        | _ -> let newEntry = createFunc()  
               cache.Add(keyType, newEntry)
               newEntry

    let GetKnownTypes<'t>() =
        let forBindings= BindingFlags.Public ||| BindingFlags.NonPublic
        typedefof<'t>.GetNestedTypes(forBindings)
        |> Array.filter FSharpType.IsUnion

    let resolve<'op,'req,'res when 'op :> OperationObject<'req,'res> > 
            (cont:IUnityContainer) =
        cont.Resolve<'op>() :> OperationObject<'req,'res>

    let call<'op,'req,'res when 'op :> OperationObject<'req,'res> > 
            (cont:IUnityContainer) (req:'req) =
        cont
        |> resolve<'op,'req,'res>
        |> function op -> op.Call(req)

    let mutable container : IUnityContainer = null

    let bindAndCall<'svc1,'rest> (f:'svc1->'rest) = 
        container.Resolve<'svc1>() |> f        

