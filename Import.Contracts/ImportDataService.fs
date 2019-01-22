namespace Import.Contracts

open System
open System.ServiceModel
open System.Runtime.Serialization

open FsServiceFramework

[<DataContract>]
type ImportImage() =

    [<DataMember(EmitDefaultValue=false, IsRequired=true)>] 
    member val Label = String.Empty with get, set

    [<DataMember(EmitDefaultValue=false, IsRequired=true)>] 
    member val Width = 1 with get, set

    [<DataMember(EmitDefaultValue=false, IsRequired=true)>] 
    member val Height = 1 with get, set

    [<DataMember(EmitDefaultValue=false, IsRequired=true)>] 
    member val Pixels = [| 0uy |] with get, set

[<ServiceContract>]
[<DicomPolicy>]
type IImportManager =
    [<OperationContract>] abstract EchoImage: inImage:ImportImage -> ImportImage
    [<OperationContract>] abstract ImportPortalImage: inImage:ImportImage -> int
    [<OperationContract>] abstract ImportSpatialRegistration: sro:float[] -> int

[<ServiceContract>]
[<ComponentPolicy>]
type IImportEngine = 
     [<OperationContract>] abstract ImportPortalImage: inImage:ImportImage -> int

[<ServiceContract>]
[<ComponentPolicy>]
type IImageDataAccess = 
    [<OperationContract>] abstract StoreImage: inImage:ImportImage -> int
