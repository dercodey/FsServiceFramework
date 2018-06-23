namespace Infrastructure 

open System
open System.Reflection
open System.Runtime.Serialization
open System.ServiceModel
open System.ServiceModel.Dispatcher

open Microsoft.FSharp.Reflection
open Microsoft.Practices.Unity

[<KnownType("GetKnownTypes")>]
type TestingContextId =
    | Production
    | VolatileTest of Guid
    | DurableTest of Guid
    static member GetKnownTypes() = 
        Utility.GetKnownTypes<TestingContextId>()

[<DataContract>]
[<CLIMutable>]
type TestingContext = {
    [<DataMember>] TestContextId : TestingContextId
    [<DataMember>] StartTimeStamp : DateTime }

module Nz2Testing =

    let createTestingContext() =
        { TestContextId = Production;
            StartTimeStamp = DateTime.Now }

    let createRepositoryForTestContext<'key, 'entity
            when 'key : comparison
            and 'entity : not struct> (container:IUnityContainer) =
        match container.Resolve<TestingContext>().TestContextId with
        | Production -> 
            printfn "Creating concrete repository for %s" typedefof<'entity>.Name
            DbSetRepository<'key, 'entity>() :> obj
        | VolatileTest guid -> 
            printfn "Creating test repository for %s guid = %s" typedefof<'entity>.Name (guid.ToString())
            // TODO: populate the repository with test data?  or restore?
            VolatileRepository<'key, 'entity>() :> obj
        | DurableTest guid ->  
            printfn "Creating test repository for %s guid = %s" typedefof<'entity>.Name (guid.ToString())
            VolatileRepository<'key, 'entity>() :> obj

    let registerMessageInspectors (container:IUnityContainer) = 
        let createClientMessageInspector (childContainer:IUnityContainer) =
            { new IClientMessageInspector with
                member this.BeforeSendRequest (request, channel) = 
                    let testingContext = container.Resolve<TestingContext>()
                    MessageHeaders.updateHeaderWithContext request.Headers testingContext |> ignore
                    null
                member this.AfterReceiveReply (request, correlation) = () } :> obj
        let createDispatchMessageInspector (childContainer:IUnityContainer) =
            { new IDispatchMessageInspector with
                member this.AfterReceiveRequest (request, channel, context) = 
                    let testingContext = MessageHeaders.getContextFromHeader<TestingContext> request.Headers
                    container.RegisterInstance<TestingContext>(testingContext) |> ignore
                    null
                member this.BeforeSendReply (reply, correlation) = () } :> obj
        container
            .RegisterType<IClientMessageInspector>("nz2testing", 
                InjectionFactory(createClientMessageInspector))
            .RegisterType<IDispatchMessageInspector>("nz2testing", 
                InjectionFactory(createDispatchMessageInspector))