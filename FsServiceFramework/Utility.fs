namespace FsServiceFramework

open System
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

type VolatileRepository<'key, 'entity 
        when 'key : comparison 
        and 'entity: not struct>
            (getKeyFunc:'entity->'key) =

    let mutable entityMap = Map.empty<'key, 'entity>

    interface IRepository<'key, 'entity> with
        member this.Get (key:'key) = 
            entityMap.Item key
        member this.Create (entity:'entity) = 
            let key = getKeyFunc entity
            entityMap <- entityMap.Add(key, entity)
            entity
        member this.Delete (entity:'entity) = 
            let key = getKeyFunc entity
            entityMap <- entityMap.Remove(key)
            true
        member this.Update (entity:'entity) =
            let key = getKeyFunc entity
            entityMap <- entityMap.Remove(key)
            entityMap <- entityMap.Add(key, entity)
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

    let unityInterceptionBehavior invoke =
        { new IInterceptionBehavior with
            member this.Invoke(input:IMethodInvocation, getNext) = invoke input getNext
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

    let updateHeaderWithContext<'t> (headers:System.ServiceModel.Channels.MessageHeaders) (current:'t) =
        let headerName = typedefof<'t>.Name
        let headerNamespace = typedefof<'t>.Namespace
        let tcHeader = System.ServiceModel.MessageHeader<'t>(current).GetUntypedHeader(headerName, headerNamespace)
        match headers.FindHeader(headerName, headerNamespace) with 
        | -1 -> ()    // no header currently
        | _ -> headers.RemoveAll(headerName, headerNamespace)
        headers.Add(tcHeader)
        headers

    let getContextFromHeader<'t> (headers:System.ServiceModel.Channels.MessageHeaders) : 't =
        let headerName = typedefof<'t>.Name
        let headerNamespace = typedefof<'t>.Namespace
        match headers.FindHeader(headerName, headerNamespace) with
        | -1 ->  invalidArg "getContextFromHeader" "no message header for type" 
        | index -> headers.GetHeader<'t>(index)

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

