namespace Infrastructure

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

    let entityMap = Map.empty<'key, 'entity>

    interface IRepository<'key, 'entity> with
        member this.Get (key:'key) = 
            entityMap.Item key
        member this.Create (entity:'entity) = 
            let key = getKeyFunc entity
            entityMap.Add(key, entity) |> ignore
            entity
        member this.Delete (entity:'entity) = 
            let key = getKeyFunc entity
            entityMap.Remove(key) |> ignore
            true
        member this.Update (entity:'entity) =
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

module Utility = 

    open System
    open System.Reflection
    open System.Collections.Generic
    open Microsoft.FSharp.Reflection

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