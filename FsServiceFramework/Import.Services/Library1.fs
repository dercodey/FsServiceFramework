namespace Import.Services

open FsServiceFramework

open Import.Contracts

module Impl = 
    open Unity
    
    let echoImage (engine:IImportEngine) (inImage:ImportImage) = inImage

    [<System.ServiceModel.ServiceBehavior(IncludeExceptionDetailInFaults=true)>]
    type ImportManager() =
        interface IImportManager with
            member this.EchoImage inImage = inImage
            member this.ImportPortalImage inImage = -1
            member this.ImportSpatialRegistration sro = -1

    let public registerService container =        
        ComponentRegistration.registerService_ typedefof<IImportManager> typedefof<ImportManager> container
