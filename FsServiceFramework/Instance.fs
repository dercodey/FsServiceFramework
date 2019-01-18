namespace FsServiceFramework

open System
open System.Runtime.Serialization
open System.ServiceModel

open Unity

[<DataContract>]
[<CLIMutable>]
type DurableInstanceContex = {
    [<DataMember>] DurableInstanceId : Guid
    [<DataMember>] InstanceCreation : DateTime }

module Instance =

    // instance context extension type that creates a new child UnityContainer 
    //      on demand when a new instance context is created
    type UnityInstanceContextExtension(container:IUnityContainer) =
        let childContainer = lazy ( container.CreateChildContainer() )
        member this.ChildContainer = childContainer.Value
        interface IExtension<InstanceContext> with
            member this.Attach (owner:InstanceContext) = ()
            member this.Detach (owner:InstanceContext) = ()

    (* TODO: why is this here? *)
    type StorageProviderInstanceContextExtension(container:IUnityContainer) =
        interface IExtension<InstanceContext> with
            member this.Attach (owner:InstanceContext) = ()
            member this.Detach (owner:InstanceContext) = ()
            