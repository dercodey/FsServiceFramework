namespace FsServiceFramework

open System
open System.Reflection
open System.ServiceModel
open System.ServiceModel.Description

// these are attributes to be applied to interfaces

[<AttributeUsage(AttributeTargets.Interface)>]
type PolicyAttribute(binding:Channels.Binding) =
    inherit Attribute()    
    member this.Binding = binding
    member this.EndpointAddress (contractType:Type) = 
        let builder = UriBuilder(this.Binding.Scheme, "localhost", -1, contractType.Name)
        builder.Uri

[<AttributeUsage(AttributeTargets.Interface)>]
type ComponentPolicyAttribute() =
    inherit PolicyAttribute(NetNamedPipeBinding())

[<AttributeUsage(AttributeTargets.Interface)>]
type IntranetPolicyAttribute() =
    inherit PolicyAttribute(NetTcpBinding())


module Policy = ()
