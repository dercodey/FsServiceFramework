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
    open System.ServiceModel.Dispatcher
    open System.ServiceModel.Description

    //{ new IInstanceContextInitializer with 
    //    member this.Initialize (ic, _) = 
    //        ic.Extensions.Add(UnityInstanceContextExtension(container))
    //        ic.Extensions.Add(Instance.StorageProviderInstanceContextExtension(container)) }
    //        |> dr.InstanceContextInitializers.Add }

    (* TODO: why is this here? *)
    type StorageProviderInstanceContextExtension(container:IUnityContainer) =
        interface IExtension<InstanceContext> with
            member this.Attach (owner:InstanceContext) = ()
            member this.Detach (owner:InstanceContext) = ()

    let configureContainer (container:IUnityContainer) = 
        (typedefof<IInstanceProvider>,
            { new IInstanceProvider with // add instance provider to resolve from unity container
                member this.GetInstance (ic) = this.GetInstance(ic, null)
                member this.GetInstance (ic, msg) = 
                    container.Resolve<ServiceEndpoint>()
                    |> function endpoint -> endpoint.Contract.ContractType
                    |> container.Resolve
                member this.ReleaseInstance (_, _) = () })
        |> container.RegisterInstance 
            