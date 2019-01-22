namespace Import.Services

open Import.Contracts

module Impl = 
    
    let echoImage (engine:IImportEngine) (inImage:ImportImage) = inImage

    let instance = 
        { new IImportManager with
            member this.EchoImage inImage = inImage
            member this.ImportPortalImage inImage = -1
            member this.ImportSpatialRegistration sro = -1 }
