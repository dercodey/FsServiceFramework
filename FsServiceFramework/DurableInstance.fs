namespace FsServiceFramework

open System
open System.Runtime.Serialization
open System.ServiceModel
open System.ServiceModel.Dispatcher
open System.ServiceModel.Description

open Unity

[<DataContract>]
[<CLIMutable>]
type DurableInstanceContex = {
    [<DataMember>] DurableInstanceId : Guid
    [<DataMember>] InstanceCreation : DateTime }

module DurableInstance =
    let configureContainer (container:IUnityContainer) = 
        System.Diagnostics.Trace.Assert(container.Resolve<ServiceEndpoint>() <> null)
        (typedefof<IInstanceProvider>,
            { new IInstanceProvider with // add instance provider to resolve from unity container
                member this.GetInstance (ic) = this.GetInstance(ic, null)
                member this.GetInstance (ic, msg) = 
                    container.Resolve<ServiceEndpoint>()
                    |> function endpoint -> endpoint.Contract.ContractType
                    |> container.Resolve
                member this.ReleaseInstance (_, _) = () })
        |> container.RegisterInstance 
            