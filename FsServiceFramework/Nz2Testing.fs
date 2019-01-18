namespace FsServiceFramework 

open System
open System.Runtime.Serialization
open System.Data.Entity

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

    let registerMessageInspectors (container:IUnityContainer) = 
        let createClientMessageInspector (childContainer:IUnityContainer) =
            { new IClientMessageInspector with
                member this.BeforeSendRequest (request, channel) = 
                    let testingContext = container.Resolve<TestingContext>()
                    Utility.updateHeaderWithContext request.Headers testingContext |> ignore
                    null
                member this.AfterReceiveReply (request, correlation) = () } :> obj
        let createDispatchMessageInspector (childContainer:IUnityContainer) =
            { new IDispatchMessageInspector with
                member this.AfterReceiveRequest (request, channel, context) = 
                    let testingContext = Utility.getContextFromHeader<TestingContext> request.Headers
                    container.RegisterInstance<TestingContext>(testingContext) |> ignore
                    null
                member this.BeforeSendReply (reply, correlation) = () } :> obj
        container
            .RegisterType<IClientMessageInspector>("nz2testing", 
                InjectionFactory(createClientMessageInspector))
            .RegisterType<IDispatchMessageInspector>("nz2testing", 
                InjectionFactory(createDispatchMessageInspector))