namespace FsServiceFramework 

open System
open System.Runtime.Serialization
open System.Data.Entity

(* TODO: this can be moved to separate module with Unity config file to configure it *)
[<KnownType("GetKnownTypes")>]
type TestingContextId =
    | Production
    | VolatileTest of Guid
    | DurableTest of Guid
    static member GetKnownTypes() = 
        Utility.GetKnownTypes<TestingContextId>()

[<DataContract>]
type TestingContext(testingContextId:TestingContextId) =
    inherit CallContextBase()
    [<DataMember>] member val TestContextId = testingContextId with get, set

module Nz2Testing =

    open Unity
    open Unity.Injection
    open System.ServiceModel.Dispatcher

    let createRepositoryForTestContext<'key, 'entity
            when 'key : comparison
            and 'entity : not struct> (container:IUnityContainer) =
        match container.Resolve<TestingContext>().TestContextId with
        | Production -> 
            printfn "Creating concrete repository for %s" typedefof<'entity>.Name
            // TODO: how do we get an actual DbSet?
            let dbSet : DbSet<'entity> = null
            DbSetRepository<'key, 'entity>(dbSet) :> obj

        | VolatileTest guid -> 
            printfn "Creating test repository for %s guid = %s" typedefof<'entity>.Name (guid.ToString())
            let getKeyFunc (forEntity:'entity) = Unchecked.defaultof<'key>
            // TODO: populate the repository with test data?  or restore?
            VolatileRepository<'key, 'entity>(getKeyFunc) :> obj

        | DurableTest guid ->  
            printfn "Creating test repository for %s guid = %s" typedefof<'entity>.Name (guid.ToString())
            let getKeyFunc (forEntity:'entity) = Unchecked.defaultof<'key>
            VolatileRepository<'key, 'entity>(getKeyFunc) :> obj
