namespace Infrastructure

open System
open System.Runtime.Serialization
open System.ServiceModel

open Unity

[<DataContract>]
[<CLIMutable>]
type DurableInstanceContex = {
    [<DataMember>] DurableInstanceId : Guid
    [<DataMember>] InstanceCreation : DateTime }

// instance context extension type that creates a new child UnityContainer 
//      on demand when a new instance context is created
type UnityInstanceContextExtension(container:IUnityContainer) =
    let childContainer = lazy ( container.CreateChildContainer() )
    member this.ChildContainer = childContainer.Value
    interface IExtension<InstanceContext> with
        member this.Attach (owner:InstanceContext) = ()
        member this.Detach (owner:InstanceContext) = ()

type StorageProviderInstanceContextExtension(container:IUnityContainer) =
    interface IExtension<InstanceContext> with
        member this.Attach (owner:InstanceContext) = ()
        member this.Detach (owner:InstanceContext) = ()

module Instance =
    open System.ServiceModel.Dispatcher
    open System.ServiceModel.Description
    open System.ServiceModel.Channels

    // creates an instancecontext initializer that will register the UnityInstanceContextExtension
    //      for a new instancecontext
    let createInstanceContextInitializer (container:IUnityContainer) : IInstanceContextInitializer = 
        { new IInstanceContextInitializer with 
            member this.Initialize (ic:InstanceContext, message:Message) = 
                ic.Extensions.Add(UnityInstanceContextExtension(container))
                ic.Extensions.Add(StorageProviderInstanceContextExtension(container)) }

    // creates an instance provider that will create a service for the given contract type
    //      by resolving from the child unity container maintained by the UnityInstanceContextExtension
    let createInstanceProvider (container:IUnityContainer) (contractType:Type) : IInstanceProvider =
        { new IInstanceProvider with
            member this.GetInstance (ic:InstanceContext) =
                this.GetInstance(ic, null)
            member this.GetInstance (ic:InstanceContext, message:Message) = 
                let extension = ic.Extensions.Find<UnityInstanceContextExtension>()
                extension.ChildContainer.Resolve(contractType)
            member this.ReleaseInstance (ic:InstanceContext, instance:obj) = () }

    // behavior that provides service instances via a UnityContainer
    let createInstanceContractBehavior (container:IUnityContainer) (contractType:Type) : IContractBehavior =
        { new IContractBehavior with 
            member this.AddBindingParameters (cd:ContractDescription, ep:ServiceEndpoint, bpc:BindingParameterCollection) = ()
            member this.ApplyClientBehavior (cd:ContractDescription, ep:ServiceEndpoint, cr:ClientRuntime) = ()
            member this.ApplyDispatchBehavior (cd:ContractDescription, ep:ServiceEndpoint, dr:DispatchRuntime) =
                dr.InstanceProvider <- createInstanceProvider container contractType
                dr.InstanceContextInitializers.Add(createInstanceContextInitializer container) 
                ()
            member this.Validate (cd:ContractDescription, ep:ServiceEndpoint) = () }