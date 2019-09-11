namespace FsServiceFramework.FsUnitTesst.Tests

open Microsoft.VisualStudio.TestTools.UnitTesting
open System.ServiceModel.Channels

open Unity

open FsServiceFramework
open FsServiceFramework.LambdaServiceRegistration
open Trending.Contracts
open Trending.Services

[<TestClass>] 
type ``test trending manager as example service`` () =
    let container = Hosting.createHostContainer()    
    
    [<TestInitialize>] 
    member ___.``set up hosting services.`` () = 
        container
        |> LambdaServiceRegistration.registerService<LambdaInvoker>
        |> Hosting.startServices 

    [<TestCleanup>] 
    member ___.``stop hosting services`` () =
        container
        |> Hosting.stopServices 
        container.Dispose()

    [<TestMethod>] 
    member ___.``invoke trending manager using lambda`` () = 
        let proxyManager = container.Resolve<IProxyManager>()
        let proxy = proxyManager.GetProxy<LambdaInvoker>()
        proxy.InvokeOperation(
            Message.CreateMessage(MessageVersion.Default, 
                "http://tempuri.org/LambdaInvoker/InvokeOperation")) |> ignore
        ()

    [<TestMethod>] 
    member ___.``when series is updated without change it should be the same is the original.`` () =
        let proxyManager = container.Resolve<IProxyManager>()
        let proxy = proxyManager.GetProxy<LambdaInvoker>()
        ()
